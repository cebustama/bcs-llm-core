using BCS.LLM.Core.Clients;
using BCS.LLM.Core.Env;
using System.Collections.Generic;
using UnityEngine;

namespace BCS.LLM.Core.Anthropic
{
    [CreateAssetMenu(fileName = "NewAnthropicClientData", menuName = "LLM/Anthropic Client Configuration")]
    public class AnthropicClientData : LLMClientData
    {
        // Current (recommended) models only.
        // Legacy / older Claude versions are intentionally omitted (D-AC3).
        // If older models become necessary, append at the end of the enum to
        // preserve serialization safety for existing assets.
        public enum AnthropicModel
        {
            Opus_4_7,
            Opus_4_6,
            Sonnet_4_6,
            Haiku_4_5,
        }

        public override LLMProvider Provider => LLMProvider.Anthropic;

        [Header("Anthropic Model")]
        [Tooltip("Select the Anthropic model to use. Sonnet_4_6 is a balanced default for general use; Haiku_4_5 is cheaper/faster; Opus_4_7 is most capable.")]
        public AnthropicModel selectedModel = AnthropicModel.Sonnet_4_6;

        private static readonly Dictionary<AnthropicModel, string> ModelStrings = new()
        {
            { AnthropicModel.Opus_4_7,   "claude-opus-4-7" },
            { AnthropicModel.Opus_4_6,   "claude-opus-4-6" },
            { AnthropicModel.Sonnet_4_6, "claude-sonnet-4-6" },
            { AnthropicModel.Haiku_4_5,  "claude-haiku-4-5-20251001" },
        };

        public override string ModelString =>
            ModelStrings.TryGetValue(selectedModel, out var id) ? id : "claude-sonnet-4-6";

        // === Secrets only come from env ===
        public override string ApiKey => LLMEnvLoader.Get("ANTHROPIC_API_KEY");

        // === Non-secret defaults from LLMEnvSettings (with optional env overrides) ===
        public override string BaseUrl
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultBase = settings != null && !string.IsNullOrWhiteSpace(settings.anthropicBaseUrl)
                    ? settings.anthropicBaseUrl
                    : "https://api.anthropic.com";

                return LLMEnvLoader.GetOrDefault("ANTHROPIC_BASE_URL", defaultBase);
            }
        }

        public string MessagesEndpoint
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultEndpoint = settings != null && !string.IsNullOrWhiteSpace(settings.anthropicMessagesEndpoint)
                    ? settings.anthropicMessagesEndpoint
                    : "/v1/messages";

                return LLMEnvLoader.GetOrDefault("ANTHROPIC_MESSAGES_ENDPOINT", defaultEndpoint);
            }
        }

        /// <summary>
        /// The Anthropic API version header. Pinned to a known-good version;
        /// update when Anthropic releases a new stable version and the client
        /// is tested against it.
        /// </summary>
        public const string ApiVersion = "2023-06-01";
    }
}