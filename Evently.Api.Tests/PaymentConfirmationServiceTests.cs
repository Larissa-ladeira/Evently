using Evently.Api.Data;
using Evently.Api.Models;
using Evently.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evently.Api.Tests;

public class PaymentConfirmationServiceTests
{
    [Fact]
    public async Task ConfirmAsync_MarksOrderPaidAndCreatesTickets()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var order = AddPendingOrder(context);

        var result = await service.ConfirmAsync(order.Id, "Webhook", "trx_123", 100m, "{}");

        Assert.True(result.Success);
        Assert.False(result.AlreadyProcessed);
        Assert.Equal(2, result.TicketCount);
        Assert.Equal("Paid", order.Status);
        Assert.Equal("Webhook", order.PaymentProvider);
        Assert.Equal("trx_123", order.PaymentId);
        Assert.Equal(2, await context.Tickets.CountAsync());
        Assert.Single(context.Transactions);
        Assert.All(context.Tickets, t => Assert.True(new QrCodeService(TestSecurity.Settings).TryVerifyTicketCode(t.QrCode, out _)));
    }

    [Fact]
    public async Task ConfirmAsync_IsIdempotentForSameTransaction()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var order = AddPendingOrder(context);

        var first = await service.ConfirmAsync(order.Id, "Webhook", "trx_123", 100m, "{}");
        var second = await service.ConfirmAsync(order.Id, "Webhook", "trx_123", 100m, "{}");

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.True(second.AlreadyProcessed);
        Assert.Equal(2, await context.Tickets.CountAsync());
        Assert.Single(context.Transactions);
    }

    [Fact]
    public async Task ConfirmAsync_RejectsAmountMismatch()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var order = AddPendingOrder(context);

        var result = await service.ConfirmAsync(order.Id, "Webhook", "trx_123", 99m, "{}");

        Assert.False(result.Success);
        Assert.Equal("Pending", order.Status);
        Assert.Empty(context.Transactions);
    }

    [Fact]
    public async Task ConfirmAsync_ReturnsErrorForUnknownOrder()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.ConfirmAsync(Guid.NewGuid(), "Webhook", "trx_123", null, "{}");

        Assert.False(result.Success);
        Assert.Equal("Pedido não encontrado.", result.Error);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static PaymentConfirmationService CreateService(AppDbContext context)
    {
        var email = new EmailService(new ConfigurationBuilder().Build());
        return new PaymentConfirmationService(
            context,
            new QrCodeService(TestSecurity.Settings),
            email,
            NullLogger<PaymentConfirmationService>.Instance);
    }

    private static Order AddPendingOrder(AppDbContext context, decimal total = 100m)
    {
        var order = new Order
        {
            UserId = Guid.NewGuid(),
            EventId = Guid.NewGuid(),
            TotalAmount = total,
            Status = "Pending",
            Items = new List<OrderItem>
            {
                new()
                {
                    TicketTypeId = Guid.NewGuid(),
                    TicketTypeName = "Pista",
                    Quantity = 2,
                    UnitPrice = total / 2,
                    TotalPrice = total
                }
            }
        };

        context.Orders.Add(order);
        context.SaveChanges();
        return order;
    }
}
