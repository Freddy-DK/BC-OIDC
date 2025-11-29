using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace OidcProvider;

public sealed record CommandResult
{
    private CommandResult(HttpStatusCode statusCode, string contentType, string? content, IReadOnlyDictionary<string, string> headers)
    {
        StatusCode = statusCode;
        ContentType = contentType;
        Content = content;
        Headers = headers;
    }

    public HttpStatusCode StatusCode { get; }

    public string ContentType { get; }

    public string? Content { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public static CommandResult FromText(HttpStatusCode statusCode, string content, string contentType = "text/plain")
    {
        return new CommandResult(statusCode, contentType, content, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public static CommandResult Empty(HttpStatusCode statusCode)
    {
        return new CommandResult(statusCode, "text/plain", null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    public CommandResult WithHeader(string name, string value)
    {
        var headers = Headers.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : Headers.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        headers[name] = value;

        return new CommandResult(StatusCode, ContentType, Content, headers);
    }
}
