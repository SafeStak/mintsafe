using CardanoSharp.Wallet.Extensions.Models.Transactions;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;

namespace Mintsafe.Lib;

public class TransactionSigner
{
    private readonly ILogger<TransactionSigner> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;

    public TransactionSigner(
        ILogger<TransactionSigner> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
    }

    public byte[] AppendWitnesses(byte[] transactionBytes)
    {
        var tx = transactionBytes.DeserializeTransaction();

        //tx.TransactionWitnessSet.VKeyWitnesses.Add(
        //    new VKeyWitness
        //    {
                
        //    });

        return transactionBytes;
    }

}
