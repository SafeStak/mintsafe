using Mintsafe.Abstractions;
using System.Collections.Generic;

namespace Mintsafe.SaleWorker;
public record SaleContext
(
    List<Nifty> MintableTokens,
    List<Nifty> AllocatedTokens,
    HashSet<Utxo> LockedUtxos,
    HashSet<Utxo> SuccessfulUtxos,
    HashSet<Utxo> RefundedUtxos
);
