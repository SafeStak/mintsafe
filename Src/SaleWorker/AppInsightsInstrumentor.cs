using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;

namespace Mintsafe.SaleWorker;

public class AppInsightsInstrumentor : IInstrumentor
{
    private readonly TelemetryClient _telemetryClient;

    public AppInsightsInstrumentor(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void TrackElapsed(
        int eventId,
        long elapsedMilliseconds,
        string name = "",
        IDictionary<string, object>? customProperties = null)
    {
        var customMetricTelemetry = new MetricTelemetry(name, elapsedMilliseconds);
        EnrichTelemetryProperties(eventId, customProperties, customMetricTelemetry);
        _telemetryClient.TrackMetric(customMetricTelemetry);
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
        var requestTelemetry = new RequestTelemetry(
            name,
            timestamp,
            TimeSpan.FromMilliseconds(elapsedMilliseconds),
            DeriveResponseCode(isSuccessful),
            isSuccessful)
        {
            Source = source,
            Url = uri
        };
        EnrichTelemetryProperties(eventId, customProperties, requestTelemetry);
        _telemetryClient.TrackRequest(requestTelemetry);
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
        var dependencyTelemetry = new DependencyTelemetry(
            type,
            target,
            name,
            data,
            timestamp,
            TimeSpan.FromMilliseconds(elapsedMilliseconds),
            DeriveResponseCode(isSuccessful),
            isSuccessful);
        EnrichTelemetryProperties(eventId, customProperties, dependencyTelemetry);
        _telemetryClient.TrackDependency(dependencyTelemetry);
    }

    public static void EnrichTelemetryProperties(
        int eventId,
        IDictionary<string, object>? customProperties,
        ISupportProperties telemetry)
    {
        if (telemetry?.Properties == null) return;

        telemetry.Properties.Add("EventId", eventId.ToString());

        if (customProperties == null) return;
        foreach (var prop in customProperties)
        {
            telemetry.Properties.Add(prop.Key, prop.Value?.ToString());
        }
    }

    private static string DeriveResponseCode(bool isSuccessful)
    {
        return isSuccessful ? "200" : "500";
    }
}

