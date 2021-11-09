using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mintsafe.Abstractions;
using Mintsafe.Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mintsafe.SaleWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly MintsafeSaleWorkerSettings _settings;
        private readonly INiftyDataService _niftyDataService;
        private readonly IUtxoRetriever _utxoRetriever;
        private readonly ITokenAllocator _tokenAllocator;
        private readonly ITokenDistributor _tokenDistributor;
        private readonly IUtxoRefunder _utxoRefunder;

        public Worker(
            ILogger<Worker> logger,
            MintsafeSaleWorkerSettings settings,
            INiftyDataService niftyDataService,
            IUtxoRetriever utxoRetriever,
            ITokenAllocator tokenAllocator,
            ITokenDistributor tokenDistributor,
            IUtxoRefunder utxoRefunder)
        {
            _logger = logger;
            _settings = settings;
            _niftyDataService = niftyDataService;
            _utxoRetriever = utxoRetriever;
            _tokenAllocator = tokenAllocator;
            _tokenDistributor = tokenDistributor;
            _utxoRefunder = utxoRefunder;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            // Get { Collection * Sale[] * Token[] }
            var collectionId = Guid.Parse("d5b35d3d-14cc-40ba-94f4-fe3b28bd52ae");
            var collection = await _niftyDataService.GetCollectionAggregateAsync(collectionId, ct);
            if (collection.ActiveSales.Length == 0)
            {
                _logger.LogWarning($"{collection.Collection.Name} with {collection.Tokens.Length} mintable tokens has no active sales!");
                return;
            }

            var activeSale = collection.ActiveSales[0];
            var mintableTokens = collection.Tokens.Where(t => t.IsMintable).ToList();
            if (mintableTokens.Count < activeSale.TotalReleaseQuantity)
            {
                _logger.LogWarning($"{collection.Collection.Name} has {mintableTokens.Count} mintable tokens which is less than {activeSale.TotalReleaseQuantity} sale release quantity.");
                return;
            }

            _logger.LogInformation($"{collection.Collection.Name} has an active sale '{activeSale.Name}' for {activeSale.TotalReleaseQuantity} nifties (out of {mintableTokens.Count} total mintable) at {activeSale.SaleAddress}{Environment.NewLine}{activeSale.LovelacesPerToken} lovelaces per NFT ({activeSale.LovelacesPerToken / 1000000} ADA) and {activeSale.MaxAllowedPurchaseQuantity} max allowed");

            var saleAllocatedTokens = new List<Nifty>();
            var utxosLocked = new HashSet<string>();
            var utxosSuccessfullyProcessed = new HashSet<string>();
            var utxosRefunded = new HashSet<string>();
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
            var stopwatch = Stopwatch.StartNew();

            do
            {
                var saleUtxos = await _utxoRetriever.GetUtxosAtAddressAsync(activeSale.SaleAddress, ct);
                _logger.LogInformation($"{stopwatch.ElapsedMilliseconds} Querying SaleAddress UTxOs for sale {activeSale.Name} of {collection.Collection.Name} by {string.Join(",", collection.Collection.Publishers)}");
                _logger.LogInformation($"Found {saleUtxos.Length} UTxOs at {activeSale.SaleAddress}");

                foreach (var saleUtxo in saleUtxos)
                {
                    if (utxosLocked.Contains(saleUtxo.ToString()))
                    {
                        _logger.LogInformation($"Utxo {saleUtxo.TxHash}[{saleUtxo.OutputIndex}]({saleUtxo.Lovelaces()}) skipped (already locked)");
                        continue;
                    }

                    var shouldRefundUtxo = false;
                    var refundReason = string.Empty;
                    try
                    {
                        var purchaseRequest = SalePurchaseGenerator.FromUtxo(saleUtxo, activeSale);
                        _logger.LogInformation($"Successfully built purchase request: {purchaseRequest.NiftyQuantityRequested} NFTs for {saleUtxo.Lovelaces()} and {purchaseRequest.ChangeInLovelace} change");

                        var tokens = await _tokenAllocator.AllocateTokensForPurchaseAsync(
                            purchaseRequest, saleAllocatedTokens, mintableTokens, activeSale, ct);
                        _logger.LogInformation($"Successfully allocated {tokens.Length} tokens");
                        saleAllocatedTokens.AddRange(tokens);

                        var txHash = await _tokenDistributor.DistributeNiftiesForSalePurchase(tokens, purchaseRequest, collection.Collection, activeSale, ct);
                        _logger.LogInformation($"Successfully minted {tokens.Length} tokens from Tx {txHash}");

                        utxosSuccessfullyProcessed.Add(saleUtxo.ToString());
                    }
                    catch (SaleInactiveException ex)
                    {
                        _logger.LogError(LogEventIds.SaleInactive, ex, "Sale is Inactive (flagged by publisher)");
                        shouldRefundUtxo = true;
                        refundReason = "saleinactive";
                    }
                    catch (SalePeriodOutOfRangeException ex)
                    {
                        _logger.LogError(LogEventIds.SalePeriodOutOfRange, ex, "Sale is Inactive (outside start/end period)");
                        shouldRefundUtxo = true;
                        refundReason = "saleperiodout";
                    }
                    catch (InsufficientPaymentException ex)
                    {
                        _logger.LogError(LogEventIds.InsufficientPayment, ex, "Insufficient payment received for sale");
                        shouldRefundUtxo = true;
                        refundReason = "salepaymentinsufficient";
                    }
                    catch (MaxAllowedPurchaseQuantityExceededException ex)
                    {
                        _logger.LogError(LogEventIds.MaxAllowedPurchaseQuantityExceeded, ex, "Payment attempted to purchase too many for sale");
                        shouldRefundUtxo = true;
                        refundReason = "salemaxallowedexceeded";
                    }
                    catch (CannotAllocateMoreThanSaleReleaseException ex)
                    {
                        _logger.LogError(LogEventIds.CannotAllocateMoreThanSaleRelease, ex, "Sale allocation exceeded release");
                        shouldRefundUtxo = true;
                        refundReason = "salefullyallocated";
                    }
                    catch (CannotAllocateMoreThanMintableException ex)
                    {
                        _logger.LogError(LogEventIds.CannotAllocateMoreThanSaleRelease, ex, "Sale allocation exceeded mintable");
                        shouldRefundUtxo = true;
                        refundReason = "collectionfullyminted";
                    }
                    catch (BlockfrostResponseException ex)
                    {
                        _logger.LogError(LogEventIds.BlockfrostServerErrorResponse, ex, "Blockfrost API response error");
                    }
                    catch (CardanoCliException ex)
                    {
                        _logger.LogError(LogEventIds.CardanoCliUnhandledError, ex, "cardano-cli Error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(LogEventIds.UnhandledError, ex, "Unhandled exception");
                    }
                    finally
                    {
                        utxosLocked.Add(saleUtxo.ToString());
                        if (shouldRefundUtxo)
                        {
                            var saleAddressSigningKey = Path.Combine(_settings.BasePath, $"{activeSale.Id}.sale.skey");
                            var refundTxHash = await _utxoRefunder.ProcessRefundForUtxo(saleUtxo, saleAddressSigningKey, refundReason, ct);
                            if (!string.IsNullOrEmpty(refundTxHash))
                            {
                                utxosRefunded.Add(saleUtxo.ToString());
                            }
                        }
                    }
                }
                _logger.LogInformation(
                    $"Successful: {utxosSuccessfullyProcessed.Count} UTxOs | Refunded: {utxosRefunded.Count} | Locked: {utxosLocked.Count} UTxOs");
            } while (await timer.WaitForNextTickAsync(ct));

        }
    }
}