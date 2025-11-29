using System;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace OidcProvider;

/// <summary>
/// Centralizes command handling logic so new commands can be added in one place.
/// </summary>
public sealed class CommandProcessor
{
    private readonly ILogger<CommandProcessor> _logger;
    private const string LocalHostUrl = "http://localhost:7071";

    public CommandProcessor(ILogger<CommandProcessor> logger)
    {
        _logger = logger;
    }

    public Task<CommandResult> HandleAsync(CommandRequest commandRequest, CancellationToken cancellationToken)
    {
        _ = cancellationToken; // Propagate cancellation when command implementations opt in.

        var normalized = Normalize(commandRequest.Command);
        var baseUrl = ResolveHostUrl(commandRequest);

        // Add new command cases here to extend the HTTP endpoint.
        switch (normalized)
        {
            case "ping":
                return Task.FromResult(CommandResult.FromText(HttpStatusCode.OK, "pong"));

            case "url":
                return Task.FromResult(CommandResult.FromText(HttpStatusCode.OK, baseUrl));

            case ".well-known/openid-configuration":
                return Task.FromResult(CommandResult.FromText(HttpStatusCode.OK, SerializeOpenIdConfiguration(baseUrl), "application/json"));

            case "":
                return Task.FromResult(CommandResult.FromText(HttpStatusCode.BadRequest, "Missing command. Provide it in the route or ?command=query."));

            default:
                _logger.LogWarning("Unknown command received: {Command}", normalized);
                return Task.FromResult(CommandResult.FromText(HttpStatusCode.NotFound, $"Unknown command '{normalized}'."));
        }
    }

    private static string SerializeOpenIdConfiguration(string baseUrl)
    {
        var payload = new
        {
            issuer = baseUrl,
            jwks_uri = $"{baseUrl}/.well-known/jwks.json",
            token_endpoint = $"{baseUrl}/oidc/token",
            id_token_signing_alg_values_supported = new[] { "RS256" },
            response_types_supported = Array.Empty<string>(),
            subject_types_supported = new[] { "public" }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string ResolveHostUrl(CommandRequest request)
    {
        if (TryGetForwardedUrl(request, out var forwarded))
        {
            return forwarded;
        }

        if (request.TryGetHeader("Host", out var host) && !string.IsNullOrWhiteSpace(host))
        {
            var scheme = request.TryGetHeader("X-Forwarded-Proto", out var proto) && !string.IsNullOrWhiteSpace(proto)
                ? proto
                : (host.StartsWith("localhost", StringComparison.OrdinalIgnoreCase) ? "http" : "https");

            return BuildUrl(scheme, host);
        }

        var websiteHost = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME");
        if (!string.IsNullOrWhiteSpace(websiteHost))
        {
            return BuildUrl("https", websiteHost);
        }

        return LocalHostUrl;
    }

    private static bool TryGetForwardedUrl(CommandRequest request, out string url)
    {
        if (request.TryGetHeader("X-Forwarded-Host", out var forwardedHost) && !string.IsNullOrWhiteSpace(forwardedHost))
        {
            var scheme = request.TryGetHeader("X-Forwarded-Proto", out var forwardedProto) && !string.IsNullOrWhiteSpace(forwardedProto)
                ? forwardedProto
                : "https";

            url = BuildUrl(scheme, forwardedHost);
            return true;
        }

        if (request.TryGetHeader("Forwarded", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            var parsed = ParseForwardedHeader(forwarded);
            if (!string.IsNullOrEmpty(parsed))
            {
                url = parsed!;
                return true;
            }
        }

        url = string.Empty;
        return false;
    }

    private static string? ParseForwardedHeader(string forwarded)
    {
        var segments = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            string? proto = null;
            string? host = null;

            var pairs = segment.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var pair in pairs)
            {
                var kvp = pair.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kvp.Length != 2)
                {
                    continue;
                }

                var key = kvp[0].ToLowerInvariant();
                var value = kvp[1].Trim('"');

                if (key == "proto")
                {
                    proto = value;
                }
                else if (key == "host")
                {
                    host = value;
                }
            }

            if (!string.IsNullOrWhiteSpace(host))
            {
                return BuildUrl(proto ?? "https", host);
            }
        }

        return null;
    }

    private static string BuildUrl(string? scheme, string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return LocalHostUrl;
        }

        var sanitizedScheme = (scheme ?? string.Empty).Trim().TrimEnd(':');
        if (string.IsNullOrWhiteSpace(sanitizedScheme))
        {
            sanitizedScheme = "https";
        }

        var sanitizedHost = host.Trim().TrimEnd('/').Trim('"');
        if (sanitizedHost.Contains("://", StringComparison.Ordinal))
        {
            return sanitizedHost.TrimEnd('/');
        }

        return $"{sanitizedScheme.ToLowerInvariant()}://{sanitizedHost}";
    }

    private static string Normalize(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return string.Empty;
        }

        return command.Trim().ToLowerInvariant();
    }
}
