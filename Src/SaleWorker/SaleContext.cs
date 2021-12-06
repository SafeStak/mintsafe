using Mintsafe.Abstractions;
using System;
using System.Collections.Generic;

namespace Mintsafe.SaleWorker;
public record SaleContext
(
    Guid SaleWorkerId,
    Sale Sale,
    NiftyCollection Collection,
    List<Nifty> MintableTokens,
    List<Nifty> AllocatedTokens,
    HashSet<Utxo> LockedUtxos,
    HashSet<Utxo> SuccessfulUtxos,
    HashSet<Utxo> RefundedUtxos
);
