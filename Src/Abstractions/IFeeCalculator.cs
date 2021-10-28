using System.Threading;
using System.Threading.Tasks;

public interface IFeeCalculator
{
    public Task CalculateFeeAsync(
        TxCalculateFeeCommand calculateFeeCommand,
        CancellationToken ct = default);
}
