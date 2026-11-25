using System.Security.Claims;
using System.Text;
using Evently.Api.Data;
using Evently.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PaymentConfirmationService _paymentConfirmation;
        private readonly PaymentWebhookService _webhookService;

        public PaymentsController(
            AppDbContext context,
            PaymentConfirmationService paymentConfirmation,
            PaymentWebhookService webhookService)
        {
            _context = context;
            _paymentConfirmation = paymentConfirmation;
            _webhookService = webhookService;
        }

        [Authorize]
        [HttpPost("mock")]
        public async Task<IActionResult> MockPayment([FromBody] MockPaymentDto dto)
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

            if (order == null)
                return NotFound(new { message = "Pedido não encontrado." });

            if (order.UserId != userId)
                return Forbid();

            var result = await _paymentConfirmation.ConfirmAsync(
                order.Id, "Mock", Guid.NewGuid().ToString(), null, null);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            if (result.AlreadyProcessed)
                return BadRequest(new { message = "Este pedido já foi pago." });

            return Ok(new
            {
                message = "Pagamento aprovado (mock)",
                orderId = order.Id,
                tickets = result.TicketCount
            });
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);

            if (!_webhookService.TryVerifySignature(rawBody, Request.Headers["X-Evently-Signature"].ToString()))
                return Unauthorized(new { message = "Assinatura do webhook inválida." });

            var payload = _webhookService.Parse(rawBody);

            if (payload == null || payload.OrderId == Guid.Empty || string.IsNullOrWhiteSpace(payload.TransactionId))
                return BadRequest(new { message = "Payload do webhook inválido." });

            if (!string.Equals(payload.Status, "Paid", StringComparison.OrdinalIgnoreCase))
                return Ok(new { message = "Evento de pagamento ignorado.", status = payload.Status });

            var provider = string.IsNullOrWhiteSpace(payload.Provider) ? "Webhook" : payload.Provider;
            provider = provider[..Math.Min(provider.Length, 50)];

            var transactionId = payload.TransactionId[..Math.Min(payload.TransactionId.Length, 200)];
            var amount = payload.Amount > 0 ? payload.Amount : (decimal?)null;

            var result = await _paymentConfirmation.ConfirmAsync(
                payload.OrderId, provider, transactionId, amount, rawBody);

            if (!result.Success)
                return BadRequest(new { message = result.Error });

            if (result.AlreadyProcessed)
                return Ok(new { message = "Pagamento já processado.", orderId = payload.OrderId, tickets = 0 });

            return Ok(new
            {
                message = "Pagamento confirmado.",
                orderId = payload.OrderId,
                tickets = result.TicketCount
            });
        }

        public class MockPaymentDto
        {
            public Guid OrderId { get; set; }
        }
    }
}
