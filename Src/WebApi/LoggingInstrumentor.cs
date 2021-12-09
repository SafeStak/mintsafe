using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Mintsafe.WebApi;

public class LoggingInstrumentor : IInstrumentor
{
    private readonly ILogger<LoggingInstrumentor> _logger;

    public LoggingInstrumentor(ILogger<LoggingInstrumentor> logger)
    {
        _logger = logger;
    }

    public void TrackElapsed(
        int eventId,
        long elapsedMilliseconds,
        string name = "",
        IDictionary<string, object>? customProperties = null)
    {
        _logger.LogInformation(eventId, $"Operation {name}({eventId}) took {elapsedMilliseconds}ms");
    }

    public void TrackRequest(
        int eventId,
        long elapsedMilliseconds,
        DateTimeOffset timestamp,
        string name,
        string? source = null,
        Uri? uri = null,
        bool isSuccessful = true,
        IDictionary<string, object>? customProperties = null)
    {
        var customPropertyContent = customProperties != null
            ? JsonSerializer.Serialize(customProperties)
            : string.Empty;
        _logger.LogInformation(eventId, $"Request {name}({eventId}) took {elapsedMilliseconds}ms at {uri} isSuccess:{isSuccessful} customProperties: {customPropertyContent}");
    }

    public void TrackDependency(
        int eventId,
        long elapsedMilliseconds,
        DateTimeOffset timestamp,
        string type,
        string target,
        string name,
        string? data = null,
        bool isSuccessful = true,
        IDictionary<string, object>? customProperties = null)
    {
        var customPropertyContent = customProperties != null 
            ? JsonSerializer.Serialize(customProperties)
            : string.Empty;
        _logger.LogInformation(eventId, $"Dependency {name}({eventId}) took {elapsedMilliseconds}ms at {target}({type}) isSuccess:{isSuccessful}, customProperties: {customPropertyContent}");
    }
}

