namespace Mintsafe.Abstractions;

public class YoloPayment
{
    public string? SourceAddress { get; init; }
    public string? DestinationAddress { get; init; }
    public Value[]? Values { get; init; }
    public string[]? Message { get; init; }
    public string? SigningKeyCborHex { get; init; }
}
