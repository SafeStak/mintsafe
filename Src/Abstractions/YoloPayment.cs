namespace Mintsafe.Abstractions;

public class YoloPayment
{
    public string? SourceAddress { get; set; }
    public string? DestinationAddress { get; set; }
    public Value[]? Values { get; set; }
    public string[]? Message { get; set; }
    public string? SigningKeyCborHex { get; set; }
}
