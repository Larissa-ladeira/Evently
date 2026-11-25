using System.Text;
using Evently.Api.Data;
using Evently.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Evently.Api.Services
{
    public sealed record PaymentConfirmationResult(bool Success, string? Error, bool AlreadyProcessed, int TicketCount);

    public sealed class PaymentConfirmationService
    {
        private readonly AppDbContext _context;
        private readonly QrCodeService _qrCodeService;
        private readonly EmailService _emailService;
        private readonly ILogger<PaymentConfirmationService> _logger;

        public PaymentConfirmationService(
            AppDbContext context,
            QrCodeService qrCodeService,
            EmailService emailService,
            ILogger<PaymentConfirmationService> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<PaymentConfirmationResult> ConfirmAsync(
            Guid orderId,
            string provider,
            string providerTransactionId,
            decimal? amount,
            string? rawPayload)
        {
            var alreadyRecorded = await _context.Transactions
                .AnyAsync(t => t.Provider == provider && t.ProviderTransactionId == providerTransactionId);

            if (alreadyRecorded)
                return new PaymentConfirmationResult(true, null, true, 0);

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return new PaymentConfirmationResult(false, "Pedido não encontrado.", false, 0);

            if (order.Status == "Paid")
                return new PaymentConfirmationResult(true, null, true, 0);

            if (order.Status is "Cancelled" or "Refunded")
                return new PaymentConfirmationResult(false, "Este pedido não pode mais ser pago.", false, 0);

            if (amount.HasValue && amount.Value != order.TotalAmount)
                return new PaymentConfirmationResult(false, "O valor do pagamento não corresponde ao pedido.", false, 0);

            await using IDbContextTransaction? tx = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync()
                : null;

            order.Status = "Paid";
            order.PaymentProvider = provider;
            order.PaymentId = providerTransactionId;
            order.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Add(new Transaction
            {
                OrderId = order.Id,
                Provider = provider,
                ProviderTransactionId = providerTransactionId,
                Amount = order.TotalAmount,
                Status = "Paid",
                RawPayload = rawPayload
            });

            var tickets = new List<Ticket>();

            foreach (var item in order.Items)
            {
                for (var i = 0; i < item.Quantity; i++)
                {
                    var ticket = new Ticket
                    {
                        OrderId = order.Id,
                        EventId = order.EventId,
                        UserId = order.UserId,
                        TicketType = item.TicketTypeName,
                        Status = "Valid"
                    };
                    ticket.QrCode = _qrCodeService.CreateTicketCode(ticket.Id);
                    _context.Tickets.Add(ticket);
                    tickets.Add(ticket);
                }
            }

            await _context.SaveChangesAsync();

            if (tx != null)
                await tx.CommitAsync();

            await TrySendTicketsEmailAsync(order, tickets);

            return new PaymentConfirmationResult(true, null, false, tickets.Count);
        }

        private async Task TrySendTicketsEmailAsync(Order order, List<Ticket> tickets)
        {
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    return;

                var ev = await _context.Events.FindAsync(order.EventId);

                var body = new StringBuilder();
                body.Append("<h2>Os seus ingressos chegaram!</h2>");
                body.Append($"<p>Evento: <strong>{ev?.Title}</strong></p>");
                body.Append($"<p>Data: {ev?.Date:dd/MM/yyyy HH:mm}</p>");
                body.Append($"<p>Local: {ev?.Location}</p>");
                body.Append("<hr />");

                foreach (var ticket in tickets)
                {
                    var qrBase64 = _qrCodeService.GenerateQrCodeBase64(ticket.QrCode);
                    body.Append("<div style=\"margin-bottom:16px;\">");
                    body.Append($"<p><strong>{ticket.TicketType}</strong> — Código: {ticket.QrCode}</p>");
                    body.Append($"<img src=\"data:image/png;base64,{qrBase64}\" alt=\"QR Code\" width=\"160\" />");
                    body.Append("</div>");
                }

                await _emailService.SendEmailAsync(user.Email, $"Ingressos - {ev?.Title}", body.ToString(), isHtml: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao enviar o e-mail de ingressos do pedido {OrderId}.", order.Id);
            }
        }
    }
}
