using SimpleExec;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
                var buildId = Guid.NewGuid();
                var networkMagic = GetNetworkParameter();

                var protocolParamsPath = Path.Combine(_settings.BasePath, "protocol.json");
                await Command.ReadAsync(
                    "cardano-cli", $"query protocol-parameters {networkMagic} --out-file {protocolParamsPath}",
                    noEcho: true);

                var feeCalculationTxBodyPath = Path.Combine(_settings.BasePath, $"fee-{buildId}.txraw");
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
                Console.WriteLine(feeTxBuildArgs);
                var feeTxBuildOutput = await Command.ReadAsync("cardano-cli", feeTxBuildArgs, noEcho: true);
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
                var feeLovelaceQuantity = long.Parse(feeCalculationOutput.Split(' ')[0]); // Parse "199469 Lovelace"
                Console.WriteLine($"Fee Calculated {feeCalculationArgs}{Environment.NewLine}{feeCalculationOutput}");

                var mintTxBodyPath = Path.Combine(_settings.BasePath, $"mint-{buildId}.txraw");
                var txBuildArgs = string.Join(" ",
                    "transaction", "build-raw",
                    GetTxInArgs(buildCommand),
                    GetTxOutArgs(buildCommand, feeLovelaceQuantity),
                    "--metadata-json-file", buildCommand.MetadataJsonPath,
                    GetMintArgs(buildCommand),
                    "--minting-script-file", buildCommand.MintingScriptPath,
                    "--invalid-hereafter", buildCommand.TtlSlot,
                    "--fee", feeLovelaceQuantity,
                    "--out-file", mintTxBodyPath
                );
                var txBuildOutput = await Command.ReadAsync("cardano-cli", txBuildArgs, noEcho: true);
                Console.WriteLine($"Mint Tx built {txBuildArgs}{Environment.NewLine}{txBuildOutput}");

                var policySigningKeyPath = Path.Combine(_settings.BasePath, $"{policyId}.skey");
                var saleAddressKeySigningPath = Path.Combine(_settings.BasePath, $"{saleId}.sale.skey");
                var signedTxOutputPath = Path.Combine(_settings.BasePath, $"{buildId}.txsigned");
                var txSignatureArgs = string.Join(" ",
                    "transaction", "sign",
                    "--signing-key-file", policySigningKeyPath,
                    "--signing-key-file", saleAddressKeySigningPath,
                    "--tx-body-file", mintTxBodyPath,
                    networkMagic,
                    "--out-file", signedTxOutputPath
                );
                var txSignatureOutput = await Command.ReadAsync("cardano-cli", txSignatureArgs, noEcho: true);
                Console.WriteLine($"Signed Tx built {txSignatureArgs}{Environment.NewLine}{txSignatureOutput}");

                var cborJson = File.ReadAllText(signedTxOutputPath);
                Console.WriteLine(cborJson);
                var doc = JsonDocument.Parse(cborJson);
                var cborHex = doc.RootElement.GetProperty("cborHex").GetString();
                if (string.IsNullOrWhiteSpace(cborHex))
                {
                    throw new ApplicationException("cborHex field from generated signature is null");
                }
                var signedTxCborBytes = HexStringToByteArray(cborHex);

                return signedTxCborBytes;
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
                // Determine the txout that will pay for the fee (i.e. the sale proceeds address and not the buyer)
                var lovelacesOut = output.Values.First(v => v.Unit == "lovelace").Quantity;
                if (output.IsFeeDeducted)
                {
                    lovelacesOut -= fee;
                }

                sb.Append($"--tx-out \"{output.Address}+{lovelacesOut}");
                
                var nativeTokens = output.Values.Where(o => o.Unit != "lovelace").ToArray();
                foreach (var value in nativeTokens)
                {
                    sb.Append($"+{value.Quantity} {value.Unit}");
                }
                sb.Append("\" ");
            }
            return sb.ToString().TrimEnd();
        }

        private string GetMintArgs(TxBuildCommand command)
        {
            if (command.Mint.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append($"--mint \"");
            foreach (var value in command.Mint)
            {
                sb.Append($"{value.Quantity} {value.Unit}+");
            }
            sb.Remove(sb.Length - 1, 1); // trim trailing + 
            sb.Append("\"");
            return sb.ToString();
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            static int GetHexVal(char hex)
            {
                int val = (int)hex;
                return val - (val < 58 ? 48 : 87);
            }

            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }
    }

    public class FakeTxBuilder : ITxBuilder
    {
        private readonly NiftyLaunchpadSettings _settings;

        public FakeTxBuilder(NiftyLaunchpadSettings settings)
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
                var buildId = Guid.NewGuid();
                var networkMagic = GetNetworkParameter();

                await Task.Delay(100);

                var protocolParamsPath = Path.Combine(_settings.BasePath, "protocol.json");

                var feeCalculationTxBodyPath = Path.Combine(_settings.BasePath, $"fee-{buildId}.txraw");
                var feeTxBuildArgs = string.Join(" ",
                    "transaction", "build-raw",  $"{Environment.NewLine}",
                    GetTxInArgs(buildCommand),  $"{Environment.NewLine}",
                    GetTxOutArgs(buildCommand),  $"{Environment.NewLine}",
                    "--metadata-json-file", buildCommand.MetadataJsonPath,  $"{Environment.NewLine}",
                    GetMintArgs(buildCommand),  $"{Environment.NewLine}",
                    "--minting-script-file", buildCommand.MintingScriptPath,  $"{Environment.NewLine}",
                    "--invalid-hereafter", buildCommand.TtlSlot,  $"{Environment.NewLine}",
                    "--fee", "0",  $"{Environment.NewLine}",
                    "--out-file", feeCalculationTxBodyPath  
                );
                Console.WriteLine($"Building fee calculation tx from:{Environment.NewLine}{feeTxBuildArgs}");
                Console.WriteLine();

                var feeCalculationArgs = string.Join(" ",
                    "transaction", "calculate-min-fee",  $"{Environment.NewLine}",
                    "--tx-body-file", feeCalculationTxBodyPath,  $"{Environment.NewLine}",
                    "--tx-in-count", buildCommand.Inputs.Length,  $"{Environment.NewLine}",
                    "--tx-out-count", buildCommand.Outputs.Length,  $"{Environment.NewLine}",
                    "--witness-count", 2,  $"{Environment.NewLine}",
                    networkMagic,  $"{Environment.NewLine}",
                    "--protocol-params-file", protocolParamsPath 
                );

                Console.WriteLine("Calculating fee using fee calculation tx (199469) from:");
                Console.WriteLine(feeCalculationArgs);
                Console.WriteLine();
                var feeLovelaceQuantity = 199469;

                var mintTxBodyPath = Path.Combine(_settings.BasePath, $"mint-{buildId}.txraw");
                var txBuildArgs = string.Join(" ",
                    "transaction", "build-raw", $"{Environment.NewLine}",
                    GetTxInArgs(buildCommand), $"{Environment.NewLine}",
                    GetTxOutArgs(buildCommand, feeLovelaceQuantity), $"{Environment.NewLine}",
                    "--metadata-json-file", buildCommand.MetadataJsonPath,$"{Environment.NewLine}",
                    GetMintArgs(buildCommand),$"{Environment.NewLine}",
                    "--minting-script-file", buildCommand.MintingScriptPath,$"{Environment.NewLine}",
                    "--invalid-hereafter", buildCommand.TtlSlot,$"{Environment.NewLine}",
                    "--fee", feeLovelaceQuantity,$"{Environment.NewLine}",
                    "--out-file", mintTxBodyPath
                );
                Console.WriteLine($"Mint Tx built from command:{Environment.NewLine}{txBuildArgs}");
                Console.WriteLine();

                var policySigningKeyPath = Path.Combine(_settings.BasePath, $"{policyId}.skey");
                var saleAddressKeySigningPath = Path.Combine(_settings.BasePath, $"{saleId}.sale.skey");
                var signedTxOutputPath = Path.Combine(_settings.BasePath, $"{buildId}.txsigned");
                var txSignatureArgs = string.Join(" ",
                    "transaction", "sign",$"{Environment.NewLine}",
                    "--signing-key-file", policySigningKeyPath,$"{Environment.NewLine}",
                    "--signing-key-file", saleAddressKeySigningPath,$"{Environment.NewLine}",
                    "--tx-body-file", mintTxBodyPath,$"{Environment.NewLine}",
                    networkMagic,$"{Environment.NewLine}",
                    "--out-file", signedTxOutputPath
                );
                Console.WriteLine($"Signed Tx from command:{Environment.NewLine}{txSignatureArgs}");
                Console.WriteLine();

                return Array.Empty<byte>();
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

                sb.Append($"--tx-out {output.Address}+{lovelaces}");
                foreach (var value in nativeTokens)
                {
                    sb.Append($"+\"{value.Quantity} {value.Unit}\" ");
                }
            }
            return sb.ToString().TrimEnd();
        }

        private string GetMintArgs(TxBuildCommand command)
        {
            if (command.Mint.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append($"--mint \"");
            foreach (var value in command.Mint)
            {
                sb.Append($"{value.Quantity} {value.Unit} + ");
            }
            sb.Remove(sb.Length-3, 3); // trim trailing + and space
            sb.Append("\"");
            return sb.ToString();
        }

        private string GetNetworkParameter()
        {
            return _settings.Network == Network.Mainnet
                ? "--mainnet"
                : "--testnet-magic 1097911063";
        }
    }
}
