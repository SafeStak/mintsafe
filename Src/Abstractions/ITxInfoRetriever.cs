﻿using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Abstractions;

public interface ITxInfoRetriever
{
    Task<TxInfo> GetTxInfoAsync(string txHash, CancellationToken ct = default);
}
