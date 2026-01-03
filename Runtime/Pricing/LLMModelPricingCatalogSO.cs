using System;
using System.Collections.Generic;
using UnityEngine;

namespace BCS.LLM.Core.Pricing
{
    /// <summary>
    /// Central catalog of per-model pricing rates (USD per 1M tokens).
    /// Non-secret data, safe to commit.
    /// Intended as a single source of truth for display + estimation in tools.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMModelPricingCatalog", menuName = "LLM/Pricing/Model Pricing Catalog", order = 50)]
    public sealed class LLMModelPricingCatalogSO : ScriptableObject
    {
        public enum ServiceTier
        {
            Standard,
            Flex,
            Priority
        }

        [Serializable]
        public sealed class ModelPriceEntry
        {
            [Tooltip("Provider ID, e.g. 'OpenAI'. Keep consistent across your project.")]
            public string providerId = "OpenAI";

            [Tooltip("Exact model id as sent to the API, e.g. 'gpt-5.2', 'o4-mini'.")]
            public string modelId = "gpt-5.2";

            public ServiceTier tier = ServiceTier.Standard;

            [Header("USD per 1M tokens")]
            [Tooltip("Cost for NON-cached input tokens (USD per 1M tokens).")]
            public double inputUsdPer1M = 0.0;

            [Tooltip("Cost for CACHED input tokens (USD per 1M tokens). Use 0 if not applicable.")]
            public double cachedInputUsdPer1M = 0.0;

            [Tooltip("Cost for output tokens (USD per 1M tokens).")]
            public double outputUsdPer1M = 0.0;

            [Header("Metadata (optional)")]
            public string notes;
        }

        [Header("Catalog Entries")]
        public List<ModelPriceEntry> entries = new();

        [Header("Catalog Metadata")]
        [Tooltip("Where did these numbers come from? (e.g. pricing page URL, internal note)")]
        public string source;

        [Tooltip("UTC ISO timestamp (manually maintained or set by an updater tool)")]
        public string lastUpdatedUtcIso;

        // Non-serialized lookup cache
        private Dictionary<string, ModelPriceEntry> _cache;

        private void OnEnable() => RebuildCache();

        public void RebuildCache()
        {
            _cache = new Dictionary<string, ModelPriceEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in entries)
            {
                if (e == null) continue;

                var provider = Normalize(e.providerId);
                var model = Normalize(e.modelId);
                var key = MakeKey(provider, model, e.tier);

                // Last entry wins if duplicates exist (simple rule for now).
                _cache[key] = e;
            }
        }

        public bool TryGet(string providerId, string modelId, ServiceTier tier, out ModelPriceEntry entry)
        {
            entry = null;

            if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(modelId))
                return false;

            _cache ??= new Dictionary<string, ModelPriceEntry>(StringComparer.OrdinalIgnoreCase);

            var key = MakeKey(Normalize(providerId), Normalize(modelId), tier);
            return _cache.TryGetValue(key, out entry);
        }

        public ModelPriceEntry GetOrNull(string providerId, string modelId, ServiceTier tier)
        {
            return TryGet(providerId, modelId, tier, out var e) ? e : null;
        }

        private static string MakeKey(string providerId, string modelId, ServiceTier tier)
            => $"{providerId}::{modelId}::{tier}";

        private static string Normalize(string s) => (s ?? "").Trim();
    }
}
