using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IBlockfrostClient
{
    Task<BlockfrostLatestBlock> GetLatestBlockAsync(CancellationToken ct = default);
    Task<BlockfrostProtocolParameters> GetLatestProtocolParameters(CancellationToken ct = default);
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

public class BlockfrostLatestBlock
{
    public uint? Epoch { get; init; }
    public uint? Slot { get; init; }
    public uint? Height { get; init; }
    public string? Hash { get; init; }
}

public class BlockfrostProtocolParameters
{
    public uint? Protocol_major_ver { get; init; }
    public uint? Protocol_minor_ver { get; init; }
    public uint? Min_fee_a { get; init; }
    public uint? Min_fee_b { get; init; }
    public string? Coins_per_utxo_word { get; init; }
}

public class BlockFrostValue
{
    public string? Unit { get; init; }
    public string? Quantity { get; init; }
}

public class BlockFrostTransactionIo
{
    public string? Address { get; init; }
    public uint Output_Index { get; init; }
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
    public string? Tx_hash { get; init; }
    public uint Output_index { get; init; }
    public BlockFrostValue[]? Amount { get; init; }
}
