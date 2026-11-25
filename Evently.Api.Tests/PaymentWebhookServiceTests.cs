using System.Security.Cryptography;
using System.Text;
using Evently.Api.Services;

namespace Evently.Api.Tests;

public class PaymentWebhookServiceTests
{
    private const string Body = "{\"orderId\":\"6d1f1c2a-4c9e-4f0b-9b15-8d2f5c3a1e77\",\"transactionId\":\"trx_1\",\"amount\":100,\"status\":\"Paid\"}";

    private readonly PaymentWebhookService _service = new(TestSecurity.Settings);

    [Fact]
    public void TryVerifySignature_AcceptsValidSignature()
    {
        var signature = Sign(Body);
        Assert.True(_service.TryVerifySignature(Body, signature));
    }

    [Fact]
    public void TryVerifySignature_RejectsTamperedBody()
    {
        var signature = Sign(Body);
        Assert.False(_service.TryVerifySignature(Body.Replace("100", "1"), signature));
    }

    [Fact]
    public void TryVerifySignature_RejectsMissingOrInvalidSignature()
    {
        Assert.False(_service.TryVerifySignature(Body, null));
        Assert.False(_service.TryVerifySignature(Body, "sha256=xyz"));
    }

    [Fact]
    public void Parse_ReadsPayloadCaseInsensitively()
    {
        var payload = _service.Parse(Body);

        Assert.NotNull(payload);
        Assert.Equal(Guid.Parse("6d1f1c2a-4c9e-4f0b-9b15-8d2f5c3a1e77"), payload.OrderId);
        Assert.Equal("trx_1", payload.TransactionId);
        Assert.Equal(100m, payload.Amount);
        Assert.Equal("Paid", payload.Status);
    }

    [Fact]
    public void Parse_ReturnsNullForInvalidJson()
    {
        Assert.Null(_service.Parse("{"));
    }

    private static string Sign(string body)
    {
        var hash = HMACSHA256.HashData(TestSecurity.Settings.WebhookSecretBytes, Encoding.UTF8.GetBytes(body));
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
