using System.Security.Claims;
using Evently.Api.Data;
using Evently.Api.Data.Mongo;
using Evently.Api.DTOs;
using Evently.Api.Models;
using Evently.Api.Models.Mongo;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EventlyMongoContext _mongoContext;
        private readonly RedisService _redisService;
        private readonly CouponService _couponService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            AppDbContext context,
            EventlyMongoContext mongoContext,
            RedisService redisService,
            CouponService couponService,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _mongoContext = mongoContext;
            _redisService = redisService;
            _couponService = couponService;
            _logger = logger;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var ev = await _context.Events.FindAsync(dto.EventId);
            if (ev == null)
                return NotFound(new { message = "Evento não encontrado." });

            if (ev.Status != "Published")
                return BadRequest(new { message = "Este evento não está disponível para venda." });

            var requested = dto.Items
                .GroupBy(i => i.TicketTypeId)
                .Select(g => new { BatchId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            var batchIds = requested.Select(r => r.BatchId).ToList();
            var batches = await _context.TicketBatches
                .Where(b => batchIds.Contains(b.Id))
                .ToListAsync();

            if (batches.Count != batchIds.Count)
                return BadRequest(new { message = "Um ou mais lotes de ingressos não existem." });

            if (batches.Any(b => b.EventId != dto.EventId))
                return BadRequest(new { message = "Lote de ingressos não pertence a este evento." });

            // Validação do formulário dinâmico (MongoDB)
            var form = await _mongoContext.EventForms
                .Find(f => f.EventId == dto.EventId)
                .FirstOrDefaultAsync();

            if (form != null && form.Fields.Count > 0)
            {
                var errors = ValidateForm(form, dto.FormAnswers);
                if (errors.Count > 0)
                    return BadRequest(new { message = "Formulário de inscrição inválido.", errors });
            }

            var acquiredLocks = new List<(string Key, string Token)>();

            try
            {
                foreach (var r in requested.OrderBy(r => r.BatchId))
                {
                    var key = $"lock:batch:{r.BatchId}";
                    string? token;

                    try
                    {
                        token = await _redisService.AcquireLockAsync(key, TimeSpan.FromSeconds(30));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Falha ao adquirir o lock Redis para {Key}.", key);
                        return StatusCode(503, new { message = "O serviço de pedidos está temporariamente indisponível. Tente novamente." });
                    }

                    if (token == null)
                        return StatusCode(503, new { message = "Há outros pedidos em andamento para este evento. Tente novamente." });

                    acquiredLocks.Add((key, token));
                }

                await using var tx = await _context.Database.BeginTransactionAsync();
                var now = DateTime.UtcNow;
                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var r in requested)
                {
                    var batch = batches.First(b => b.Id == r.BatchId);

                    if (batch.SaleStart.HasValue && now < batch.SaleStart.Value)
                        return BadRequest(new { message = $"O lote '{batch.Name}' ainda não está à venda." });
                    if (batch.SaleEnd.HasValue && now > batch.SaleEnd.Value)
                        return BadRequest(new { message = $"As vendas do lote '{batch.Name}' já encerraram." });

                    var available = batch.Quantity - batch.Sold;
                    if (available < r.Quantity)
                        return Conflict(new { message = $"Estoque insuficiente para o lote '{batch.Name}'. Disponível: {available}." });

                    batch.Sold += r.Quantity;
                    subtotal += batch.Price * r.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        TicketTypeId = batch.Id,
                        TicketTypeName = batch.Name,
                        Quantity = r.Quantity,
                        UnitPrice = batch.Price,
                        TotalPrice = batch.Price * r.Quantity
                    });
                }

                // Cupom de desconto
                var (coupon, discount, couponError) = await _couponService.EvaluateAsync(dto.EventId, dto.CouponCode, subtotal);
                if (couponError != null)
                    return BadRequest(new { message = couponError });

                if (coupon != null)
                    coupon.UsedCount += 1;

                var total = subtotal - discount;

                var order = new Order
                {
                    UserId = userId,
                    EventId = dto.EventId,
                    TotalAmount = total,
                    Status = "Pending",
                    CouponCode = coupon?.Code,
                    DiscountAmount = discount,
                    Items = orderItems
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                var answers = dto.FormAnswers?
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                if (answers is { Count: > 0 })
                {
                    await _mongoContext.OrderFormResponses.InsertOneAsync(new OrderFormResponse
                    {
                        OrderId = order.Id,
                        EventId = dto.EventId,
                        UserId = userId,
                        Answers = answers
                    });
                }

                await tx.CommitAsync();

                return Created("", new { orderId = order.Id, totalAmount = order.TotalAmount, discount, subtotal });
            }
            finally
            {
                foreach (var (key, token) in acquiredLocks)
                {
                    try { await _redisService.ReleaseLockAsync(key, token); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Falha ao liberar o lock Redis {Key}.", key); }
                }
            }
        }

        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Event)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (order.UserId != userId && role != "Admin" && role != "Organizer" && role != "Staff")
                return Forbid();

            var tickets = await _context.Tickets
                .Where(t => t.OrderId == order.Id)
                .Select(t => new { t.Id, t.TicketType, t.Status, t.QrCode, t.UsedAt })
                .ToListAsync();

            return Ok(new
            {
                order.Id,
                order.Status,
                order.TotalAmount,
                order.DiscountAmount,
                order.CouponCode,
                order.CreatedAt,
                order.PaymentProvider,
                Event = order.Event == null ? null : new
                {
                    order.Event.Id,
                    order.Event.Title,
                    order.Event.Date,
                    order.Event.Location,
                    order.Event.Category,
                    order.Event.ImageUrl
                },
                Buyer = order.User == null ? null : new { order.User.Name, order.User.Email },
                Items = order.Items.Select(i => new { i.TicketTypeName, i.Quantity, i.UnitPrice, i.TotalPrice }),
                Tickets = tickets
            });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Items)
                .Include(o => o.Event)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.Id,
                o.Status,
                o.TotalAmount,
                o.DiscountAmount,
                o.CouponCode,
                o.CreatedAt,
                TicketCount = o.Items.Sum(i => i.Quantity),
                Event = o.Event == null ? null : new
                {
                    o.Event.Id,
                    o.Event.Title,
                    o.Event.Date,
                    o.Event.Location,
                    o.Event.ImageUrl,
                    o.Event.Category
                }
            });

            return Ok(result);
        }

        [Authorize]
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound(new { message = "Pedido não encontrado." });

            if (order.UserId != userId && role != "Admin")
                return Forbid();

            if (order.Status is "Cancelled" or "Refunded")
                return BadRequest(new { message = "Este pedido já foi cancelado." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            // Repor estoque
            var batchIds = order.Items.Select(i => i.TicketTypeId).ToList();
            var batches = await _context.TicketBatches
                .Where(b => batchIds.Contains(b.Id))
                .ToListAsync();

            foreach (var item in order.Items)
            {
                var batch = batches.FirstOrDefault(b => b.Id == item.TicketTypeId);
                if (batch != null)
                    batch.Sold = Math.Max(0, batch.Sold - item.Quantity);
            }

            // Cancelar ingressos não utilizados
            var tickets = await _context.Tickets
                .Where(t => t.OrderId == order.Id && t.Status == "Valid")
                .ToListAsync();

            foreach (var ticket in tickets)
                ticket.Status = "Cancelled";

            // Repor cupom
            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.EventId == order.EventId && c.Code == order.CouponCode);
                if (coupon != null && coupon.UsedCount > 0)
                    coupon.UsedCount -= 1;
            }

            order.Status = order.Status == "Paid" ? "Refunded" : "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "Pedido cancelado com sucesso.", status = order.Status });
        }

        private static List<string> ValidateForm(EventForm form, Dictionary<string, string>? answers)
        {
            var errors = new List<string>();
            answers ??= new Dictionary<string, string>();

            foreach (var field in form.Fields)
            {
                answers.TryGetValue(field.Name, out var value);

                if (field.Required && string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"O campo '{field.Label}' é obrigatório.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(value)
                    && string.Equals(field.Type, "select", StringComparison.OrdinalIgnoreCase)
                    && field.Options is { Count: > 0 }
                    && !field.Options.Contains(value))
                {
                    errors.Add($"Valor inválido para o campo '{field.Label}'.");
                }
            }

            return errors;
        }
    }
}
