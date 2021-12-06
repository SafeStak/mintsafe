using System;
using System.Collections.Generic;

namespace Mintsafe.Abstractions;

public interface IInstrumentor
{
    void TrackElapsed(
        int eventId,
        long elapsedMilliseconds,
        string name = "",
        IDictionary<string, object>? customProperties = null);

    void TrackRequest(
        int eventId,
        long elapsedMilliseconds,
        DateTimeOffset timestamp,
        string name,
        string? source = null,
        Uri? uri = null,
        bool isSuccessful = true,
        IDictionary<string, object>? customProperties = null);

    void TrackDependency(
        int eventId,
        long elapsedMilliseconds,
        DateTimeOffset timestamp,
        string type,
        string target,
        string name,
        string? data = null,
        bool isSuccessful = true,
        IDictionary<string, object>? customProperties = null);
}