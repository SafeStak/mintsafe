using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface IMintTransactionBuilder
{
    BuiltTransaction BuildTx(
        BuildTransactionCommand buildCommand,
        NetworkContext networkContext);

    BuiltTransaction BuildTx(
        BuildTxCommand buildCommand,
        NetworkContext networkContext);
}
