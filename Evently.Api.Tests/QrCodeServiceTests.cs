using Evently.Api.Services;

namespace Evently.Api.Tests;

public class QrCodeServiceTests
{
    private readonly QrCodeService _service = new(TestSecurity.Settings);

    [Fact]
    public void CreateTicketCode_CanBeVerified()
    {
        var ticketId = Guid.NewGuid();

        var code = _service.CreateTicketCode(ticketId);

        Assert.True(_service.TryVerifyTicketCode(code, out var verified));
        Assert.Equal(ticketId, verified);
        Assert.True(code.Length < 50);
    }

    [Fact]
    public void TryVerifyTicketCode_RejectsTamperedCode()
    {
        var code = _service.CreateTicketCode(Guid.NewGuid());
        var tampered = code[..^1] + (code[^1] == 'a' ? 'b' : 'a');

        Assert.False(_service.TryVerifyTicketCode(tampered, out _));
    }
}
