using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OidcProvider;
using Xunit;

namespace OidcProvider.Tests;

public class CommandProcessorTests
{
    private readonly CommandProcessor _processor = new(new TestLogger<CommandProcessor>());

    [Fact]
    public async Task PingCommandReturnsPong()
    {
        var request = new CommandRequest("ping", string.Empty, "get", new Dictionary<string, string>());

        var result = await _processor.HandleAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("pong", result.Content);
    }

    [Fact]
    public async Task MissingCommandReturnsBadRequest()
    {
        var request = new CommandRequest(null, string.Empty, "get", new Dictionary<string, string>());

        var result = await _processor.HandleAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task UnknownCommandReturnsNotFound()
    {
        var request = new CommandRequest("unknown", string.Empty, "get", new Dictionary<string, string>());

        var result = await _processor.HandleAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Contains("unknown", result.Content, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UrlCommandReturnsEndpoint()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host"] = "localhost:7071"
        };

        var request = new CommandRequest("url", string.Empty, "get", headers);

        var result = await _processor.HandleAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("http://localhost:7071", result.Content);
    }

    [Fact]
    public async Task WellKnownOpenIdConfigurationReturnsExpectedDocument()
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["X-Forwarded-Host"] = "prod.example.com",
            ["X-Forwarded-Proto"] = "https"
        };

        var request = new CommandRequest(".well-known/openid-configuration", string.Empty, "get", headers);

        var result = await _processor.HandleAsync(request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("application/json", result.ContentType);
        Assert.NotNull(result.Content);

        using var document = JsonDocument.Parse(result.Content);
        var root = document.RootElement;

        Assert.Equal("https://prod.example.com", root.GetProperty("issuer").GetString());
        Assert.Equal("https://prod.example.com/.well-known/jwks.json", root.GetProperty("jwks_uri").GetString());
        Assert.Equal("https://prod.example.com/oidc/token", root.GetProperty("token_endpoint").GetString());

        var algs = root.GetProperty("id_token_signing_alg_values_supported").EnumerateArray().ToArray();
        Assert.Single(algs);
        Assert.Equal("RS256", algs[0].GetString());

        Assert.Empty(root.GetProperty("response_types_supported").EnumerateArray());

        var subjects = root.GetProperty("subject_types_supported").EnumerateArray().ToArray();
        Assert.Single(subjects);
        Assert.Equal("public", subjects[0].GetString());
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
