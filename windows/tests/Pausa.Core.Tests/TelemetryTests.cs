using System.Net;
using System.Text.Json;

namespace Pausa.Core.Tests;

public class TelemetryTests
{
    [Fact]
    public void Launch_envelope_matches_the_seriamo_contract()
    {
        var id = Guid.Parse("6f1c2c5e-7d4b-4f0e-9b1a-2a3b4c5d6e7f");
        var json = Telemetry.BuildLaunchEnvelope("1.0", "A1B2C3D4-0000-4000-8000-000000000001",
            new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.FromHours(-7)), id);

        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("schema_version").GetInt32());
        var e = Assert.Single(doc.RootElement.GetProperty("events").EnumerateArray());
        var fields = e.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString());
        Assert.Equal(new Dictionary<string, string?>
        {
            ["event_id"] = id.ToString(),
            ["product"] = "pausa",
            ["event"] = "app_launch",
            ["app_version"] = "1.0",
            ["platform"] = "windows",
            ["occurred_at"] = "2026-11-01T15:30:00Z",
            ["install_id"] = "A1B2C3D4-0000-4000-8000-000000000001",
        }, fields);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.Accepted, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public void Retries_only_transient_failures(HttpStatusCode? status, bool retry) =>
        Assert.Equal(retry, Telemetry.IsRetryable(status));

    [Fact]
    public void Endpoints_point_at_seriamo_hosts()
    {
        Assert.Equal("https://telemetry.seriamo.com/v1/events", Endpoints.Telemetry.ToString());
        Assert.Equal("https://updates.seriamo.com/v1/products/pausa/releases/latest?current=1.0&platform=windows",
            Endpoints.LatestRelease("1.0").ToString());
        Assert.Equal("https://feedback.seriamo.com/?product=pausa&version=1.0&platform=windows",
            Endpoints.FeedbackForm("1.0").ToString());
    }
}
