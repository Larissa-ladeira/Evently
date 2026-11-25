using Evently.Api.Services;

namespace Evently.Api.Tests;

internal static class TestSecurity
{
    public static SecuritySettings Settings { get; } = new(
        "test-jwt-key-with-at-least-32-characters",
        "EventlyApi",
        "EventlyUser",
        "test-webhook-secret-with-32-chars");
}
