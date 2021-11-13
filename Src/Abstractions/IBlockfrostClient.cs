using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions
{
    public interface IBlockfrostClient
    {
        Task<BlockFrostTransactionUtxoResponse> GetTransactionAsync(string txHash, CancellationToken ct = default);
        Task<Utxo[]> GetUtxosAtAddressAsync(string address, CancellationToken ct = default);
        Task<string> SubmitTransactionAsync(byte[] txSignedBinary, CancellationToken ct = default);
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

    public class BlockfrostResponseException : ApplicationException
    {
        public int StatusCode { get; }

        public BlockfrostResponseException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        public BlockfrostResponseException(string message, int statusCode, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}