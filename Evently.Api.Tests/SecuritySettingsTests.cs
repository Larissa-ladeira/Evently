using Evently.Api.Services;
using Microsoft.Extensions.Configuration;

namespace Evently.Api.Tests;

public class SecuritySettingsTests
{
    [Fact]
    public void FromConfiguration_ThrowsWhenJwtKeyIsMissing()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "EventlyApi",
            ["Jwt:Audience"] = "EventlyUser",
            ["Payment:WebhookSecret"] = "test-webhook-secret-with-32-chars"
        });

        Assert.Throws<InvalidOperationException>(() => SecuritySettings.FromConfiguration(configuration));
    }

    [Fact]
    public void FromConfiguration_ThrowsForPlaceholderKey()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "CHAVE-AUSENTE-configure-este-segredo-agora",
            ["Jwt:Issuer"] = "EventlyApi",
            ["Jwt:Audience"] = "EventlyUser",
            ["Payment:WebhookSecret"] = "test-webhook-secret-with-32-chars"
        });

        Assert.Throws<InvalidOperationException>(() => SecuritySettings.FromConfiguration(configuration));
    }

    [Fact]
    public void FromConfiguration_ThrowsForShortSecret()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-jwt-key-with-at-least-32-characters",
            ["Jwt:Issuer"] = "EventlyApi",
            ["Jwt:Audience"] = "EventlyUser",
            ["Payment:WebhookSecret"] = "short"
        });

        Assert.Throws<InvalidOperationException>(() => SecuritySettings.FromConfiguration(configuration));
    }

    [Fact]
    public void FromConfiguration_AcceptsValidSettings()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "test-jwt-key-with-at-least-32-characters",
            ["Jwt:Issuer"] = "EventlyApi",
            ["Jwt:Audience"] = "EventlyUser",
            ["Payment:WebhookSecret"] = "test-webhook-secret-with-32-chars"
        });

        var settings = SecuritySettings.FromConfiguration(configuration);

        Assert.Equal("EventlyApi", settings.JwtIssuer);
        Assert.True(settings.WebhookSecretBytes.Length >= 32);
    }

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
