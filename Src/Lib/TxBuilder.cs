using SimpleExec;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NiftyLaunchpad.Lib
{
    public class TxBuilder : ITxBuilder
    {
        private readonly NiftyLaunchpadSettings _settings;

        public TxBuilder(NiftyLaunchpadSettings settings)
        {
            _settings = settings;
        }

        public async Task<byte[]> BuildTxAsync(
            TxBuildCommand buildCommand, 
            string policyId, 
            string saleId, 
            CancellationToken ct = default)
        {
            try
            {
                var id = Guid.NewGuid();
                var networkMagic = GetNetworkParameter();

                var protocolParamsPath = Path.Combine(_settings.BasePath, "protocol.json");
                await Command.ReadAsync(
                    "cardano-cli", $"query protocol-parameters {networkMagic} --out-file {protocolParamsPath}",
                    noEcho: true);
                //Console.WriteLine($"query protocol-parameters {networkMagic} --out-file {protocolParamsPath}");

                var feeCalculationTxBodyPath = Path.Combine(_settings.BasePath, $"fee-{id}.txraw");
                var feeTxBuildArgs = string.Join(" ",
                    "transaction", "build-raw",
                    GetTxInArgs(buildCommand),
                    GetTxOutArgs(buildCommand),
                    "--metadata-json-file", buildCommand.MetadataJsonPath,
                    GetMintArgs(buildCommand),
                    "--minting-script-file", buildCommand.MintingScriptPath,
                    "--invalid-hereafter", buildCommand.TtlSlot,
                    "--fee", "0",
                    "--out-file", feeCalculationTxBodyPath
                );
                var feeTxBuildOutput = await Command.ReadAsync("cardano-cli", feeTxBuildArgs, noEcho: true);
                //var feeTxBuildOutput = "";
                Console.WriteLine($"Fee Tx built {feeTxBuildArgs}{Environment.NewLine}{feeTxBuildOutput}");

                var feeCalculationArgs = string.Join(" ",
                    "transaction", "calculate-min-fee",
                    "--tx-body-file", feeCalculationTxBodyPath,
                    "--tx-in-count", buildCommand.Inputs.Length,
                    "--tx-out-count", buildCommand.Outputs.Length,
                    "--witness-count", 2,
                    networkMagic,
                    "--protocol-params-file", protocolParamsPath
                );
                var feeCalculationOutput = await Command.ReadAsync("cardano-cli", feeCalculationArgs, noEcho: true);
                //var feeCalculationOutput = "12798";
                Console.WriteLine($"Fee Calculated {feeCalculationArgs}{Environment.NewLine}{feeCalculationOutput}");

                var mintTxBodyPath = Path.Combine(_settings.BasePath, $"mint-{id}.txraw");
                var txBuildArgs = string.Join(" ",
                    "transaction", "build-raw",
                    GetTxInArgs(buildCommand),
                    GetTxOutArgs(buildCommand, long.Parse(feeCalculationOutput)),
                    "--metadata-json-file", buildCommand.MetadataJsonPath,
                    GetMintArgs(buildCommand),
                    "--minting-script-file", buildCommand.MintingScriptPath,
                    "--invalid-hereafter", buildCommand.TtlSlot,
                    "--fee", feeCalculationOutput,
                    "--out-file", mintTxBodyPath
                );
                var txBuildOutput = await Command.ReadAsync("cardano-cli", txBuildArgs, noEcho: true);
                //var txBuildOutput = "";
                Console.WriteLine($"Mint Tx built {txBuildArgs}{Environment.NewLine}{txBuildOutput}");

                var signedTxPath = Path.Combine(_settings.BasePath, $"{id}.txsigned");
                var txSignatureArgs = string.Join(" ",
                    "transaction", "sign",
                    "--signing-key-file", $"{policyId}.skey",
                    "--signing-key-file", $"{saleId}.sale.skey",
                    "--tx-body-file", mintTxBodyPath,
                    networkMagic,
                    "--out-file", signedTxPath
                );
                var txSignatureOutput = await Command.ReadAsync("cardano-cli", txSignatureArgs, noEcho: true);
                //var txSignatureOutput = "";
                Console.WriteLine($"Real Tx built {txSignatureArgs}{Environment.NewLine}{txSignatureOutput}");

                var signedTx = File.ReadAllBytes(signedTxPath);
                //var signedTx = Array.Empty<byte>();

                return signedTx;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        private string GetTxInArgs(TxBuildCommand command)
        {
            var sb = new StringBuilder();
            foreach (var input in command.Inputs)
            {
                sb.Append($"--tx-in {input.TxHash}#{input.OutputIndex} ");
            }
            return sb.ToString().TrimEnd();
        }

        private string GetTxOutArgs(TxBuildCommand command, long fee = 0)
        {
            var sb = new StringBuilder();
            foreach (var output in command.Outputs)
            {
                var lovelaceOut = output.Values.First(v => v.Unit == "lovelace");
                var lovelaces = lovelaceOut.Quantity;
                var nativeTokens = output.Values.Where(o => o.Unit != "lovelace").ToArray();
                // Special case to deduct fee for deposit address (no native tokens)
                var isDepositAddress = nativeTokens.Length == 0;
                if (isDepositAddress)
                {
                    lovelaces -= fee;
                }

                sb.Append($"--tx-out {output.Address}+{lovelaces} ");
                foreach (var value in nativeTokens)
                {
                    sb.Append($"+{value.Quantity} {value.Unit} ");
                }
            }
            return sb.ToString().TrimEnd();
        }

        private string GetMintArgs(TxBuildCommand command)
        {
            if (command.Mint.Length == 0)
                return string.Empty;
            
            var sb = new StringBuilder();
            sb.Append($"--mint ");
            foreach (var value in command.Mint)
            {
                sb.Append($"+{value.Quantity} {value.Unit} ");
            }
            return sb.ToString().TrimEnd();
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }
}
