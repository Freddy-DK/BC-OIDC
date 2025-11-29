using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OidcProvider.Functions;

public sealed class CommandsFunction
{
    private static readonly string[] SupportedMethods =
    {
        "delete", "get", "head", "options", "patch", "post", "put"
    };

    private readonly CommandProcessor _processor;
    private readonly ILogger<CommandsFunction> _logger;

    public CommandsFunction(CommandProcessor processor, ILogger<CommandsFunction> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [Function("Commands")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "put", "delete", "patch", "options", "head", Route = "{*command}")] HttpRequestData req,
        string? command)
    {
        if (!SupportedMethods.Contains(req.Method, StringComparer.OrdinalIgnoreCase))
        {
            var methodNotAllowed = req.CreateResponse(HttpStatusCode.MethodNotAllowed);
            await methodNotAllowed.WriteStringAsync($"HTTP method '{req.Method}' is not supported.", req.FunctionContext.CancellationToken);
            return methodNotAllowed;
        }

        var body = await ReadBodyAsync(req.Body);
        command ??= req.Query.GetValues("command")?.FirstOrDefault();

        var headerSnapshot = SnapshotHeaders(req.Headers);
        var commandRequest = new CommandRequest(command, body, req.Method, headerSnapshot);
        var result = await _processor.HandleAsync(commandRequest, req.FunctionContext.CancellationToken);

        var response = req.CreateResponse(result.StatusCode);
        if (!string.IsNullOrEmpty(result.Content))
        {
            response.Headers.Add("Content-Type", result.ContentType);
            await response.WriteStringAsync(result.Content, req.FunctionContext.CancellationToken);
        }

        foreach (var header in result.Headers)
        {
            response.Headers.Add(header.Key, header.Value);
        }

        _logger.LogInformation("Handled command '{Command}' with status code {StatusCode}", command ?? string.Empty, (int)result.StatusCode);
        return response;
    }

    private static async Task<string> ReadBodyAsync(Stream body)
    {
        if (body == Stream.Null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(body);
        return await reader.ReadToEndAsync();
    }

    private static IReadOnlyDictionary<string, string> SnapshotHeaders(HttpHeadersCollection headers)
    {
        var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            snapshot[header.Key] = string.Join(",", header.Value);
        }

        return snapshot;
    }
}
