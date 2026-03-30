using System;
using BCS.LLM.Core.Clients;

namespace BCS.LLM.Core.Pricing
{
    public enum LLMPricingSourceKind
    {
        None,
        Catalog,
        ClientFallback
    }

    [Serializable]
    public sealed class LLMPricingResolutionResult
    {
        public bool HasPricing;
        public string ProviderId;
        public string ModelId;
        public LLMModelPricingCatalogSO.ServiceTier Tier;

        public LLMPricingSourceKind SourceKind;
        public string PricingSource;

        public double InputUsdPer1M;
        public double CachedInputUsdPer1M;
        public double OutputUsdPer1M;

        public LLMModelPricingCatalogSO.ModelPriceEntry CatalogEntry;
    }

    /// <summary>
    /// Shared pricing-resolution helper for editor/runtime-adjacent tooling.
    /// Precedence:
    /// 1) catalog entry for provider/model/tier
    /// 2) per-client fallback pricing fields
    /// 3) no pricing
    /// </summary>
    public static class LLMPricingResolution
    {
        public static LLMPricingResolutionResult Resolve(
            ILLMClient client,
            LLMModelPricingCatalogSO catalog,
            LLMModelPricingCatalogSO.ServiceTier tier = LLMModelPricingCatalogSO.ServiceTier.Standard,
            string providerId = null)
        {
            var provider = InferProviderId(client, providerId);
            var model = (client?.Model ?? string.Empty).Trim();

            var result = new LLMPricingResolutionResult
            {
                HasPricing = false,
                ProviderId = provider,
                ModelId = model,
                Tier = tier,
                SourceKind = LLMPricingSourceKind.None,
                PricingSource = BuildNoPricingSource(provider, model, tier)
            };

            if (client == null || string.IsNullOrWhiteSpace(model))
                return result;

            if (catalog != null && catalog.TryGet(provider, model, tier, out var entry) && entry != null)
            {
                result.HasPricing = true;
                result.SourceKind = LLMPricingSourceKind.Catalog;
                result.PricingSource = BuildCatalogSource(provider, model, tier, catalog);
                result.InputUsdPer1M = entry.inputUsdPer1M;
                result.CachedInputUsdPer1M = entry.cachedInputUsdPer1M;
                result.OutputUsdPer1M = entry.outputUsdPer1M;
                result.CatalogEntry = entry;
                return result;
            }

            if (HasAnyClientFallbackRate(client))
            {
                result.HasPricing = true;
                result.SourceKind = LLMPricingSourceKind.ClientFallback;
                result.PricingSource = $"Client fallback ({provider} / {model})";
                result.InputUsdPer1M = client.InputUSDPerMTokens;
                result.CachedInputUsdPer1M = client.CachedInputUSDPerMTokens;
                result.OutputUsdPer1M = client.OutputUSDPerMTokens;
                return result;
            }

            return result;
        }

        private static bool HasAnyClientFallbackRate(ILLMClient client)
        {
            if (client == null) return false;

            return client.InputUSDPerMTokens > 0f
                || client.CachedInputUSDPerMTokens > 0f
                || client.OutputUSDPerMTokens > 0f;
        }

        private static string InferProviderId(ILLMClient client, string explicitProviderId)
        {
            if (!string.IsNullOrWhiteSpace(explicitProviderId))
                return explicitProviderId.Trim();

            var typeName = client?.GetType().Name ?? string.Empty;

            if (typeName.IndexOf("OpenAI", StringComparison.OrdinalIgnoreCase) >= 0)
                return "OpenAI";
            if (typeName.IndexOf("Gemini", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Gemini";
            if (typeName.IndexOf("Azure", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Azure";

            return "Unknown";
        }

        private static string BuildCatalogSource(
            string providerId,
            string modelId,
            LLMModelPricingCatalogSO.ServiceTier tier,
            LLMModelPricingCatalogSO catalog)
        {
            var core = $"Catalog ({providerId} / {modelId} / {tier})";
            if (!string.IsNullOrWhiteSpace(catalog?.source))
                return $"{core} — {catalog.source}";
            return core;
        }

        private static string BuildNoPricingSource(
            string providerId,
            string modelId,
            LLMModelPricingCatalogSO.ServiceTier tier)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return "No pricing available (client/model unresolved).";

            return $"No pricing found for {providerId} / {modelId} / {tier}.";
        }
    }
}
