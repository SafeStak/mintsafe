using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.Lib;

public class KeyVaultMintingKeychainRetriever : IMintingKeychainRetriever
{
    private readonly ILogger<KeyVaultMintingKeychainRetriever> _logger;
    private readonly IInstrumentor _instrumentor;
    private readonly MintsafeAppSettings _settings;

    public KeyVaultMintingKeychainRetriever(
        ILogger<KeyVaultMintingKeychainRetriever> logger,
        IInstrumentor instrumentor,
        MintsafeAppSettings settings)
    {
        _logger = logger;
        _instrumentor = instrumentor;
        _settings = settings;
    }

    public async Task<MintingKeyChain> GetMintingKeyChainAsync(
        SaleContext saleContext,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        var client = new SecretClient(
            new Uri(_settings.KeyVaultUrl ?? throw new ApplicationException("KeyVault settings invalid - null URL")),
            new DefaultAzureCredential(),
            new SecretClientOptions
            {
                Retry =
                {
                    Delay= TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(16),
                    MaxRetries = 5,
                    Mode = RetryMode.Exponential
                }
            });

        var signingKeyResponse = (await client.GetSecretAsync($"{saleContext.Collection.PolicyId}-MintSigningKey", cancellationToken: ct));
        var policyKeyResponse = (await client.GetSecretAsync($"{saleContext.Collection.PolicyId}-MintPolicyKey", cancellationToken: ct));
        if (signingKeyResponse == null || signingKeyResponse.GetRawResponse().IsError
            || policyKeyResponse == null || policyKeyResponse.GetRawResponse().IsError)
        {
            throw new ApplicationException("Unsuccessful responses retrieving minting keychain secrets");
        }

        _instrumentor.TrackDependency(
            EventIds.KeychainRetrievalElapsed,
            sw.ElapsedMilliseconds,
            DateTime.UtcNow,
            nameof(KeyVaultMintingKeychainRetriever),
            saleContext.Sale.Id.ToString(),
            nameof(GetMintingKeyChainAsync),
            isSuccessful: true);

        return new MintingKeyChain(
            new[] { signingKeyResponse.Value.Value }, 
            new BasicMintingPolicy(new[] { policyKeyResponse.Value.Value }, (uint)saleContext.Collection.SlotExpiry));
    }
}

