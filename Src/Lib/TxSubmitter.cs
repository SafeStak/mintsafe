using SimpleExec;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxSubmitter : ITxSubmitter
    {
        private readonly BlockfrostClient _blockFrostClient;

        public TxSubmitter(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            var txHash = await _blockFrostClient.SubmitTransactionAsync(txSignedBinary, ct);

            return txHash;
        }
    }

    public class CardanoCliTxSubmitter : ITxSubmitter
    {
        private readonly NiftyLaunchpadSettings _settings;

        public CardanoCliTxSubmitter(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();

            // Build the JSON containing cborHex that the CLI expects
            var txSubmissionId = Guid.NewGuid();
            var txSignedJsonPath = Path.Combine(_settings.BasePath, $"{txSubmissionId}.txsigned");
            var txSignedJson = $"{{ \"type\": \"Tx MaryEra\", \"description\": \"\", \"cborHex\": \"{Convert.ToHexString(txSignedBinary)}\"}}";
            File.WriteAllText(txSignedJsonPath, txSignedJson);
            
            var rawUtxoTable = await Command.ReadAsync(
                "cardano-cli", string.Join(" ",
                    "transaction", "submit",
                    GetNetworkParameter(),
                    "--tx-file", txSignedJsonPath
                ), noEcho: true, cancellationToken: ct);
            Console.WriteLine($"UTxOs retrieved after {stopwatch.ElapsedMilliseconds}ms:{Environment.NewLine}{rawUtxoTable}");

            // Derive the txhash
            var txHash = await Command.ReadAsync(
                "cardano-cli", string.Join(" ",
                    "transaction", "txid",
                    "--tx-file", txSignedJsonPath
                ), noEcho: true, cancellationToken: ct);

            return txHash;
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }

    public class FakeTxSubmitter : ITxSubmitter
    {
        private readonly BlockfrostClient _blockFrostClient;

        public FakeTxSubmitter(BlockfrostClient blockFrostClient)
        {
            _blockFrostClient = blockFrostClient;
        }

        public async Task<string> SubmitTxAsync(byte[] txSignedBinary, CancellationToken ct = default)
        {
            await Task.Delay(100);
            return "51e9b6577ad260c273aee5a3786d6b39cce44fc3c49bf44f395499d34b3814f5";
        }
    }
}
