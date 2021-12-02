using Mintsafe.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Mintsafe.Lib;

public static class TxUtils
{
    public static Value[] SubtractValues(
        this Value[] lhsValues, Value[] rhsValues)
    {
        static Value SubtractSingleValue(Value lhsValue, Value rhsValue)
        {
            return rhsValue == default
                ? lhsValue
                : new Value(lhsValue.Unit, lhsValue.Quantity - rhsValue.Quantity);
        };

        if (rhsValues.Length == 0)
            return lhsValues;

        var diff = lhsValues
            .Select(lv => SubtractSingleValue(lv, rhsValues.FirstOrDefault(rv => rv.Unit == lv.Unit)))
            .ToArray();

        return diff;
    }

    public static long CalculateMinUtxoLovelace(
        Value[] outputValues, 
        int lovelacePerUtxoWord = 34482, 
        int policyIdBytes = 28,
        bool hasDataHash = false)
    {
        // https://docs.cardano.org/native-tokens/minimum-ada-value-requirement#min-ada-valuecalculation
        // valueSize = prefix + (numDistinctPids * 28(policy bytes fixed) + numTokens * 12(per token fixed) + tokensNameLen + 7) ~/8 
        const int fixedUtxoPrefix = 6;
        const int fixedPerTokenCost = 12;
        const int utxoEntrySizeWithoutVal = 27;
        const int coinSize = 2; 
        const int adaOnlyUtxoSizeWords = utxoEntrySizeWithoutVal + coinSize;
        const int byteRoundUpAddition = 7;
        const int byteLength = 8;
        const int dataHashSizeWords = 10;

        var isAdaOnlyUtxo = outputValues.Length == 1 && outputValues[0].Unit == Assets.LovelaceUnit;
        if (isAdaOnlyUtxo)
            return lovelacePerUtxoWord * adaOnlyUtxoSizeWords; // 999978 lovelaces or 0.999978 ADA

        var customTokens = outputValues.Where(v => v.Unit != Assets.LovelaceUnit).ToArray();
        var policyIds = new HashSet<string>();
        var assetNames = new HashSet<string>();
        foreach (var customToken in customTokens)
        {
            var unitSegments = customToken.Unit.Split('.');
            policyIds.Add(unitSegments[0]);
            assetNames.Add(unitSegments[1]);
        }
        var sumAssetNameLengths = assetNames.Sum(an => an.Length);

        var valueSize = fixedUtxoPrefix + (
            (policyIds.Count * policyIdBytes) 
            + (customTokens.Length * fixedPerTokenCost) 
            + sumAssetNameLengths + byteRoundUpAddition) / byteLength;

        var dataHashSize = hasDataHash ? dataHashSizeWords : 0;

        var minUtxoLovelace = lovelacePerUtxoWord * (utxoEntrySizeWithoutVal + valueSize + dataHashSize);

        return minUtxoLovelace;
    }
}
