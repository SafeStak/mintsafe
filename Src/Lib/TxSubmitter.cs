﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxSubmitter
    {
        private readonly BlockfrostClient _blockFrostClient;

        public TxSubmitter(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            var submission = await _blockFrostClient.SubmitTransactionAsync(txSignedBinary, ct);

            return submission;
        }
    }
}