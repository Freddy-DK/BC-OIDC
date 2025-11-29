using System;
using System.Collections.Generic;

namespace OidcProvider;

/// <summary>
/// Represents the normalized inputs for a command invocation.
/// </summary>
public sealed class CommandRequest
{
    public CommandRequest(string? command, string? body, string method, IReadOnlyDictionary<string, string> headers)
    {
        Command = command;
        Body = body ?? string.Empty;
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public string? Command { get; }

    public string Body { get; }

    public string Method { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public bool TryGetHeader(string name, out string value)
    {
        if (Headers.TryGetValue(name, out var headerValue))
        {
            value = headerValue;
            return true;
        }

        value = string.Empty;
        return false;
    }
}
