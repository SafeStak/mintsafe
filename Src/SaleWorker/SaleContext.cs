using Mintsafe.Abstractions;
using System.Collections.Generic;

namespace Mintsafe.SaleWorker
{
    public record SaleContext
    (
        List<Nifty> MintableTokens,
        List<Nifty> AllocatedTokens,
        HashSet<string> LockedUtxos,
        HashSet<string> SuccessfulUtxos,
        HashSet<string> RefundedUtxos
    );
}
