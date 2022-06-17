using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using SimpleExec;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class CardanoCliException : ApplicationException
{
    public string Network { get; }
    public string Args { get; }

    public CardanoCliException(
        string message,
        Exception inner,
        string network,
        string args = "") : base(message, inner)
    {
        Network = network;
        Args = args;
    }
}

public class CardanoCliTxBuilder : ITxBuilder
{
    private readonly ILogger<CardanoCliTxBuilder> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;
    private readonly string _networkMagic;

    public CardanoCliTxBuilder(
        ILogger<CardanoCliTxBuilder> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
        _networkMagic = _settings.Network == Network.Mainnet
            ? "--mainnet"
            : "--testnet-magic 1097911063";
    }

    public async Task<byte[]> BuildTxAsync(
        TxBuildCommand buildCommand,
        CancellationToken ct = default)
    {
        var buildId = Guid.NewGuid();
        var actualTxBuildArgs = string.Empty;
        var isSuccessful = false;
        var sw = Stopwatch.StartNew();
        try
        {
            var protocolParamsOutputPath = Path.Combine(_settings.BasePath, "protocol.json");
            if (!File.Exists(protocolParamsOutputPath))
            {
                await Command.ReadAsync(
                    "cardano-cli",
                    $"query protocol-parameters {_networkMagic} --out-file {protocolParamsOutputPath}",
                    noEcho: true,
                    cancellationToken: ct);
            }

            var feeCalculationTxBodyOutputPath = Path.Combine(_settings.BasePath, $"fee-{buildId}.txraw");
            actualTxBuildArgs = string.Join(" ",
                "transaction", "build-raw",
                GetTxInArgs(buildCommand),
                GetTxOutArgs(buildCommand),
                GetMetadataArgs(buildCommand),
                GetMintArgs(buildCommand),
                GetMintingScriptFileArgs(buildCommand),
                GetInvalidHereafterArgs(buildCommand),
                "--fee", "0",
                "--out-file", feeCalculationTxBodyOutputPath
            );
            var feeTxBuildCliOutput = await Command.ReadAsync(
                "cardano-cli",
                actualTxBuildArgs,
                noEcho: true,
                cancellationToken: ct);
            _logger.LogDebug($"Fee Tx built {actualTxBuildArgs}{Environment.NewLine}{feeTxBuildCliOutput}");

            var feeCalculationArgs = string.Join(" ",
                "transaction", "calculate-min-fee",
                "--tx-body-file", feeCalculationTxBodyOutputPath,
                "--tx-in-count", buildCommand.Inputs.Length,
                "--tx-out-count", buildCommand.Outputs.Length,
                "--witness-count", buildCommand.SigningKeyFiles.Length,
                _networkMagic,
                "--protocol-params-file", protocolParamsOutputPath
            );
            var feeCalculationCliOutput = await Command.ReadAsync(
                "cardano-cli",
                feeCalculationArgs,
                noEcho: true,
                cancellationToken: ct);
            var feeLovelaceQuantity = ulong.Parse(feeCalculationCliOutput.Split(' ')[0]); // Parse "199469 Lovelace"
            _logger.LogDebug($"Fee Calculated {feeCalculationArgs}{Environment.NewLine}{feeCalculationCliOutput}");

            var actualTxBodyOutputPath = Path.Combine(_settings.BasePath, $"{buildId}.txraw");
            actualTxBuildArgs = string.Join(" ",
                "transaction", "build-raw",
                GetTxInArgs(buildCommand),
                GetTxOutArgs(buildCommand, feeLovelaceQuantity),
                GetMetadataArgs(buildCommand),
                GetMintArgs(buildCommand),
                GetMintingScriptFileArgs(buildCommand),
                GetInvalidHereafterArgs(buildCommand),
                "--fee", feeLovelaceQuantity,
                "--out-file", actualTxBodyOutputPath
            );
            var actualTxBuildCliOutput = await Command.ReadAsync(
                "cardano-cli",
                actualTxBuildArgs,
                noEcho: true,
                cancellationToken: ct);
            _logger.LogDebug($"Actual Tx built {actualTxBuildArgs}{Environment.NewLine}{actualTxBuildCliOutput}");

            var signedTxOutputPath = Path.Combine(_settings.BasePath, $"{buildId}.txsigned");
            var txSignatureArgs = string.Join(" ",
                "transaction", "sign",
                GetSigningKeyFiles(buildCommand),
                "--tx-body-file", actualTxBodyOutputPath,
                _networkMagic,
                "--out-file", signedTxOutputPath
            );
            var txSignatureCliOutput = await Command.ReadAsync(
                "cardano-cli",
                txSignatureArgs,
                noEcho: true,
                cancellationToken: ct);
            _logger.LogDebug($"Signed Tx built {txSignatureArgs}{Environment.NewLine}{txSignatureCliOutput}");

            // Extract bytes from cborHex field of JSON in signed tx file
            var cborJson = File.ReadAllText(signedTxOutputPath);
            _logger.LogDebug(cborJson);
            var doc = JsonDocument.Parse(cborJson);
            var cborHex = doc.RootElement.GetProperty("cborHex").GetString();
            if (string.IsNullOrWhiteSpace(cborHex))
            {
                // TODO: typed exception
                throw new ApplicationException("cborHex field from generated signature is null");
            }
            var signedTxCborBytes = HexStringToByteArray(cborHex);
            isSuccessful = true;
            return signedTxCborBytes;
        }
        catch (Exception ex)
        {
            throw new CardanoCliException($"Unhandled exception in {nameof(CardanoCliTxBuilder)}", ex, _settings.Network.ToString(), actualTxBuildArgs);
        }
        finally
        {
            TryCleanupTempFiles(
                Path.Combine(_settings.BasePath, $"fee-{buildId}.txraw"),
                Path.Combine(_settings.BasePath, $"{buildId}.txraw"),
                Path.Combine(_settings.BasePath, $"{buildId}.txsigned"),
                buildCommand.MetadataJsonPath);
            _instrumentor.TrackDependency(
                EventIds.TxBuilderElapsed,
                sw.ElapsedMilliseconds,
                DateTime.UtcNow,
                nameof(CardanoCliTxBuilder),
                Path.Combine(_settings.BasePath, $"{buildId}.txsigned"),
                nameof(BuildTxAsync),
                isSuccessful: isSuccessful);
        }
    }

    private static string GetTxInArgs(TxBuildCommand command)
    {
        var sb = new StringBuilder();
        foreach (var input in command.Inputs)
        {
            sb.Append($"--tx-in {input.TxHash}#{input.OutputIndex} ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
    }

    private static string GetTxOutArgs(TxBuildCommand command, ulong fee = 0)
    {
        var sb = new StringBuilder();
        foreach (var output in command.Outputs)
        {
            // Determine the txout that will pay for the fee (i.e. the sale proceeds address and not the buyer)
            var lovelacesOut = output.Values.Lovelaces;
            if (output.IsFeeDeducted)
            {
                lovelacesOut -= fee;
            }

            sb.Append($"--tx-out \"{output.Address}+{lovelacesOut}");

            var nativeTokens = output.Values.NativeAssets;
            foreach (var value in nativeTokens)
            {
                sb.Append($"+{value.Quantity} {value.PolicyId}.{value.AssetName}");
            }
            sb.Append("\" ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
    }

    private static string GetMintArgs(TxBuildCommand command)
    {
        if (command.Mint.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append($"--mint \"");
        foreach (var value in command.Mint)
        {
            sb.Append($"{value.Quantity} {value.PolicyId}.{value.AssetName}+");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing + 
        sb.Append('"');
        return sb.ToString();
    }

    private static string GetMetadataArgs(TxBuildCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MetadataJsonPath))
            return string.Empty;

        return $"--metadata-json-file {command.MetadataJsonPath}";
    }

    private static string GetMintingScriptFileArgs(TxBuildCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MintingScriptPath))
            return string.Empty;

        return $"--minting-script-file {command.MintingScriptPath}";
    }

    private static string GetInvalidHereafterArgs(TxBuildCommand command)
    {
        if (command.TtlSlot <= 0)
            return string.Empty;

        return $"--invalid-hereafter {command.TtlSlot}";
    }

    private static string GetSigningKeyFiles(TxBuildCommand command)
    {
        if (command.SigningKeyFiles.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var skeyFile in command.SigningKeyFiles)
        {
            sb.Append($"--signing-key-file {skeyFile} ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
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

    private void TryCleanupTempFiles(params string[] filePaths)
    {
        foreach (var filePath in filePaths)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}

public class FakeTxBuilder : ITxBuilder
{
    private readonly ILogger<FakeTxBuilder> _logger;
    private readonly MintsafeAppSettings _settings;
    private readonly string _networkMagic;

    public FakeTxBuilder(
        ILogger<FakeTxBuilder> logger, MintsafeAppSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _networkMagic = _settings.Network == Network.Mainnet
            ? "--mainnet"
            : "--testnet-magic 1097911063";
    }

    public async Task<byte[]> BuildTxAsync(
        TxBuildCommand buildCommand,
        CancellationToken ct = default)
    {
        var buildId = Guid.NewGuid();

        await Task.Delay(100, ct);

        var protocolParamsPath = Path.Combine(_settings.BasePath, "protocol.json");

        var feeCalculationTxBodyPath = Path.Combine(_settings.BasePath, $"fee-{buildId}.txraw");
        var feeTxBuildArgs = string.Join(" ",
            "transaction", "build-raw", $"{Environment.NewLine}",
            GetTxInArgs(buildCommand), $"{Environment.NewLine}",
            GetTxOutArgs(buildCommand), $"{Environment.NewLine}",
            GetMetadataArgs(buildCommand), $"{Environment.NewLine}",
            GetMintArgs(buildCommand), $"{Environment.NewLine}",
            GetMintingScriptFileArgs(buildCommand), $"{Environment.NewLine}",
            GetInvalidHereafterArgs(buildCommand), $"{Environment.NewLine}",
            "--fee", "0", $"{Environment.NewLine}",
            "--out-file", feeCalculationTxBodyPath
        );
        _logger.LogDebug($"Building fee calculation tx from:{Environment.NewLine}{feeTxBuildArgs}");

        var feeCalculationArgs = string.Join(" ",
            "transaction", "calculate-min-fee", $"{Environment.NewLine}",
            "--tx-body-file", feeCalculationTxBodyPath, $"{Environment.NewLine}",
            "--tx-in-count", buildCommand.Inputs.Length, $"{Environment.NewLine}",
            "--tx-out-count", buildCommand.Outputs.Length, $"{Environment.NewLine}",
            "--witness-count", buildCommand.SigningKeyFiles.Length, $"{Environment.NewLine}",
            _networkMagic, $"{Environment.NewLine}",
            "--protocol-params-file", protocolParamsPath
        );

        _logger.LogDebug("Calculating fee using fee calculation tx (199469) from:");
        _logger.LogDebug(feeCalculationArgs);
        var feeLovelaceQuantity = 199469UL;

        var actualTxBodyPath = Path.Combine(_settings.BasePath, $"mint-{buildId}.txraw");
        var txBuildArgs = string.Join(" ",
            "transaction", "build-raw", $"{Environment.NewLine}",
            GetTxInArgs(buildCommand), $"{Environment.NewLine}",
            GetTxOutArgs(buildCommand, feeLovelaceQuantity), $"{Environment.NewLine}",
            GetMetadataArgs(buildCommand), $"{Environment.NewLine}",
            GetMintArgs(buildCommand), $"{Environment.NewLine}",
            GetMintingScriptFileArgs(buildCommand), $"{Environment.NewLine}",
            GetInvalidHereafterArgs(buildCommand), $"{Environment.NewLine}",
            "--fee", feeLovelaceQuantity, $"{Environment.NewLine}",
            "--out-file", actualTxBodyPath
        );
        _logger.LogDebug($"Actual Tx built from command:{Environment.NewLine}{txBuildArgs}");

        var signedTxOutputPath = Path.Combine(_settings.BasePath, $"{buildId}.txsigned");
        var txSignatureArgs = string.Join(" ",
            "transaction", "sign", $"{Environment.NewLine}",
            GetSigningKeyFiles(buildCommand), $"{Environment.NewLine}",
            "--tx-body-file", actualTxBodyPath, $"{Environment.NewLine}",
            _networkMagic, $"{Environment.NewLine}",
            "--out-file", signedTxOutputPath
        );
        _logger.LogDebug($"Signed Tx from command:{Environment.NewLine}{txSignatureArgs}");

        return Array.Empty<byte>();
    }

    private static string GetTxInArgs(TxBuildCommand command)
    {
        var sb = new StringBuilder();
        foreach (var input in command.Inputs)
        {
            sb.Append($"--tx-in {input.TxHash}#{input.OutputIndex} ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
    }

    private static string GetTxOutArgs(TxBuildCommand command, ulong fee = 0)
    {
        var sb = new StringBuilder();
        foreach (var output in command.Outputs)
        {
            // Determine the txout that will pay for the fee (i.e. the sale proceeds address and not the buyer)
            var lovelacesOut = output.Values.Lovelaces;
            if (output.IsFeeDeducted)
            {
                lovelacesOut -= fee;
            }

            sb.Append($"--tx-out \"{output.Address}+{lovelacesOut}");

            var nativeTokens = output.Values.NativeAssets;
            foreach (var value in nativeTokens)
            {
                sb.Append($"+{value.Quantity} {value.PolicyId}.{value.AssetName}");
            }

            sb.Append("\" ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
    }

    private static string GetMintArgs(TxBuildCommand command)
    {
        if (command.Mint.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.Append($"--mint \"");
        foreach (var value in command.Mint)
        {
            sb.Append($"{value.Quantity} {value.PolicyId}.{value.AssetName}+");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing + 
        sb.Append('"');
        return sb.ToString();
    }

    private static string GetMetadataArgs(TxBuildCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MetadataJsonPath))
            return string.Empty;

        return $"--metadata-json-file {command.MetadataJsonPath}";
    }

    private static string GetMintingScriptFileArgs(TxBuildCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.MintingScriptPath))
            return string.Empty;

        return $"--minting-script-file {command.MintingScriptPath}";
    }

    private static string GetInvalidHereafterArgs(TxBuildCommand command)
    {
        if (command.TtlSlot <= 0)
            return string.Empty;

        return $"--invalid-hereafter {command.TtlSlot}";
    }

    private static string GetSigningKeyFiles(TxBuildCommand command)
    {
        if (command.SigningKeyFiles.Length == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var skeyFile in command.SigningKeyFiles)
        {
            sb.Append($"--signing-key-file {skeyFile} ");
        }
        sb.Remove(sb.Length - 1, 1); // trim trailing space 
        return sb.ToString();
    }

}
