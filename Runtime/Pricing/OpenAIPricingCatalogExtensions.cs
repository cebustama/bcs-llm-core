using System;
using System.Collections.Generic;

namespace BCS.LLM.Core.Pricing
{
    /// <summary>
    /// Helper methods to quickly populate a <see cref="LLMModelPricingCatalogSO"/> with OpenAI pricing defaults.
    ///
    /// Notes:
    /// - Values are USD per 1M tokens.
    /// - This fills the <see cref="LLMModelPricingCatalogSO.ServiceTier.Standard"/> tier.
    /// - Prompt caching (cached input tokens) is billed at the "Cached input" rate when available.
    /// - Some models list "-" for cached input; for those we set cachedInputUsdPer1M = 0.
    /// </summary>
    public static class OpenAIPricingCatalogExtensions
    {
        private readonly struct PriceRow
        {
            public readonly string modelId;
            public readonly double input;
            public readonly double cached;
            public readonly double output;
            public readonly string notes;

            public PriceRow(string modelId, double input, double cached, double output, string notes = null)
            {
                this.modelId = modelId;
                this.input = input;
                this.cached = cached;
                this.output = output;
                this.notes = notes;
            }
        }

        // Standard tier (Text tokens) as per OpenAI pricing page.
        // Keep this list small and focused on the models you're likely to use.
        private static readonly PriceRow[] StandardTextDefaults =
        {
            // GPT-5
            new("gpt-5.2", 1.75, 0.175, 14.00),
            new("gpt-5.1", 1.25, 0.125, 10.00),
            new("gpt-5",   1.25, 0.125, 10.00),
            new("gpt-5-mini", 0.25, 0.025, 2.00),
            new("gpt-5-nano", 0.05, 0.005, 0.40),

            // "latest" aliases (handy for tooling; priced in the same table)
            new("gpt-5.2-chat-latest", 1.75, 0.175, 14.00),
            new("gpt-5.1-chat-latest", 1.25, 0.125, 10.00),
            new("gpt-5-chat-latest",   1.25, 0.125, 10.00),

            // GPT-4.1 family
            new("gpt-4.1", 2.00, 0.50, 8.00),
            new("gpt-4.1-mini", 0.40, 0.10, 1.60),
            new("gpt-4.1-nano", 0.10, 0.025, 0.40),

            // GPT-4o family
            new("gpt-4o", 2.50, 1.25, 10.00),
            new("gpt-4o-mini", 0.15, 0.075, 0.60),

            // Reasoning / o-series
            new("o1", 15.00, 7.50, 60.00),
            new("o1-mini", 1.10, 0.55, 4.40),
            new("o1-pro", 150.00, 0.0, 600.00, "No cached-input rate listed on pricing page."),

            new("o3", 2.00, 0.50, 8.00),
            new("o3-mini", 1.10, 0.55, 4.40),
            new("o3-pro", 20.00, 0.0, 80.00, "No cached-input rate listed on pricing page."),

            new("o4-mini", 1.10, 0.275, 4.40),
        };

        /// <summary>
        /// Populates this catalog with OpenAI's STANDARD-tier text-token pricing defaults.
        ///
        /// If an entry already exists:
        /// - overwriteExisting=true: replace its rates.
        /// - overwriteExisting=false: only fill in rates that are currently 0.
        /// </summary>
        public static void ApplyOpenAIStandardTextDefaults(
            this LLMModelPricingCatalogSO catalog,
            bool overwriteExisting = false,
            bool updateMetadata = true)
        {
            if (catalog == null) return;

            if (updateMetadata)
            {
                catalog.source = "https://platform.openai.com/docs/pricing";
                catalog.lastUpdatedUtcIso = DateTime.UtcNow.ToString("o");
            }

            catalog.entries ??= new List<LLMModelPricingCatalogSO.ModelPriceEntry>();

            foreach (var row in StandardTextDefaults)
            {
                Upsert(
                    catalog.entries,
                    providerId: "OpenAI",
                    modelId: row.modelId,
                    tier: LLMModelPricingCatalogSO.ServiceTier.Standard,
                    inputUsdPer1M: row.input,
                    cachedInputUsdPer1M: row.cached,
                    outputUsdPer1M: row.output,
                    notes: row.notes,
                    overwriteExisting: overwriteExisting);
            }

            catalog.RebuildCache();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(catalog);
#endif
        }

        private static void Upsert(
            List<LLMModelPricingCatalogSO.ModelPriceEntry> entries,
            string providerId,
            string modelId,
            LLMModelPricingCatalogSO.ServiceTier tier,
            double inputUsdPer1M,
            double cachedInputUsdPer1M,
            double outputUsdPer1M,
            string notes,
            bool overwriteExisting)
        {
            if (entries == null) return;

            var existing = Find(entries, providerId, modelId, tier);

            if (existing == null)
            {
                existing = new LLMModelPricingCatalogSO.ModelPriceEntry
                {
                    providerId = providerId,
                    modelId = modelId,
                    tier = tier,
                    inputUsdPer1M = inputUsdPer1M,
                    cachedInputUsdPer1M = cachedInputUsdPer1M,
                    outputUsdPer1M = outputUsdPer1M,
                    notes = notes
                };

                entries.Add(existing);
                return;
            }

            if (overwriteExisting)
            {
                existing.inputUsdPer1M = inputUsdPer1M;
                existing.cachedInputUsdPer1M = cachedInputUsdPer1M;
                existing.outputUsdPer1M = outputUsdPer1M;
                if (!string.IsNullOrWhiteSpace(notes)) existing.notes = notes;
                return;
            }

            // Only fill missing rates.
            if (existing.inputUsdPer1M <= 0) existing.inputUsdPer1M = inputUsdPer1M;
            if (existing.cachedInputUsdPer1M <= 0) existing.cachedInputUsdPer1M = cachedInputUsdPer1M;
            if (existing.outputUsdPer1M <= 0) existing.outputUsdPer1M = outputUsdPer1M;
            if (string.IsNullOrWhiteSpace(existing.notes) && !string.IsNullOrWhiteSpace(notes)) existing.notes = notes;
        }

        private static LLMModelPricingCatalogSO.ModelPriceEntry Find(
            List<LLMModelPricingCatalogSO.ModelPriceEntry> entries,
            string providerId,
            string modelId,
            LLMModelPricingCatalogSO.ServiceTier tier)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null) continue;

                if (!string.Equals((e.providerId ?? "").Trim(), (providerId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.Equals((e.modelId ?? "").Trim(), (modelId ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                if (e.tier != tier) continue;

                return e;
            }

            return null;
        }
    }
}
