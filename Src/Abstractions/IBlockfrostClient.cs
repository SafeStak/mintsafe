using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IBlockfrostClient
{
    Task<BlockFrostTransactionUtxoResponse> GetTransactionAsync(string txHash, CancellationToken ct = default);
    Task<BlockFrostAddressUtxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default);
    Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default);
}

public class BlockfrostResponseException : ApplicationException
{
    public int StatusCode { get; }
    public string? ResponseContent { get; }

    public BlockfrostResponseException(
        string message,
        int statusCode,
        string? responseContent = null,
        Exception? innerException = null) : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
    }
}

public class BlockFrostValue
{
    public string? Unit { get; init; }
    public string? Quantity { get; init; }
}

public class BlockFrostTransactionIo
{
    public string? Address { get; init; }
    public int Output_Index { get; init; }
    public BlockFrostValue[]? Amount { get; init; }
}

public class BlockFrostTransactionUtxoResponse
{
    public string? Hash { get; init; }
    public BlockFrostTransactionIo[]? Inputs { get; init; }
    public BlockFrostTransactionIo[]? Outputs { get; init; }
}

public class BlockFrostAddressUtxo
{
    public string? Tx_Hash { get; init; }
    public int Output_Index { get; init; }
    public BlockFrostValue[]? Amount { get; init; }
}
