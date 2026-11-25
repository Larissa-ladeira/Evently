namespace Evently.Api.DTOs
{
    public class PaymentWebhookDto
    {
        public string? Provider { get; set; }
        public string? Type { get; set; }
        public Guid OrderId { get; set; }
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
    }
}
