using System.Threading;
using System.Threading.Tasks;

public interface ITxBuilder
{
    public Task<byte[]> BuildTxAsync(
        TxBuildCommand buildCommand, 
        string policyId,
        string saleId,
        CancellationToken ct = default);
}
