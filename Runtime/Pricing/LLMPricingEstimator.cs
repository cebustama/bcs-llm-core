using System;
using UnityEngine;

namespace BCS.LLM.Core.Pricing
{
    /// <summary>
    /// Pure estimation utilities based on token usage and USD/1M token rates.
    /// No API calls. No editor dependencies.
    /// </summary>
    public static class LLMPricingEstimator
    {
        [Serializable]
        public struct TokenUsage
        {
            public int inputTokens;
            public int cachedInputTokens;
            public int outputTokens;

            /// <summary>
            /// For models that report reasoning tokens separately.
            /// If you already include reasoning tokens in outputTokens, leave this at 0.
            /// If separate, you can add it into outputTokens when estimating (see helper below).
            /// </summary>
            public int reasoningTokens;
        }

        [Serializable]
        public struct CostBreakdown
        {
            public double nonCachedInputUsd;
            public double cachedInputUsd;
            public double outputUsd;

            public double totalUsd => nonCachedInputUsd + cachedInputUsd + outputUsd;

            public override string ToString()
                => $"Total: {totalUsd:0.000000} (input: {nonCachedInputUsd:0.000000}, cached: {cachedInputUsd:0.000000}, output: {outputUsd:0.000000})";
        }

        /// <summary>
        /// Estimate cost using explicit USD/1M rates.
        /// </summary>
        public static CostBreakdown Estimate(
            TokenUsage usage,
            double inputUsdPer1M,
            double cachedInputUsdPer1M,
            double outputUsdPer1M,
            bool treatReasoningAsOutput = true)
        {
            var input = Math.Max(0, usage.inputTokens);
            var cached = Math.Max(0, usage.cachedInputTokens);
            var output = Math.Max(0, usage.outputTokens);
            var reasoning = Math.Max(0, usage.reasoningTokens);

            // Guard: cached can't exceed input
            if (cached > input) cached = input;

            var nonCached = input - cached;

            // Reasoning tokens are billed like output tokens for estimation purposes.
            // If your API usage already includes reasoning in outputTokens, set treatReasoningAsOutput=false.
            if (treatReasoningAsOutput && reasoning > 0)
                output += reasoning;

            return new CostBreakdown
            {
                nonCachedInputUsd = TokensToUsd(nonCached, inputUsdPer1M),
                cachedInputUsd = TokensToUsd(cached, cachedInputUsdPer1M),
                outputUsd = TokensToUsd(output, outputUsdPer1M)
            };
        }

        /// <summary>
        /// Estimate cost from a catalog entry.
        /// </summary>
        public static CostBreakdown Estimate(
            TokenUsage usage,
            LLMModelPricingCatalogSO.ModelPriceEntry priceEntry,
            bool treatReasoningAsOutput = true)
        {
            if (priceEntry == null)
                return default;

            return Estimate(
                usage,
                priceEntry.inputUsdPer1M,
                priceEntry.cachedInputUsdPer1M,
                priceEntry.outputUsdPer1M,
                treatReasoningAsOutput);
        }

        public static double TokensToUsd(int tokens, double usdPer1M)
        {
            if (tokens <= 0 || usdPer1M <= 0) return 0.0;
            return (tokens / 1_000_000.0) * usdPer1M;
        }

        public static string FormatUsd(double usd, int decimals = 6)
        {
            decimals = Mathf.Clamp(decimals, 2, 10);
            return usd.ToString("0." + new string('0', decimals));
        }
    }
}
