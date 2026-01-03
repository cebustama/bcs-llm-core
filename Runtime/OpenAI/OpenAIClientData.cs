using BCS.LLM.Core.Env;
using System.Collections.Generic;
using UnityEngine;

namespace Eon.Narrative.LLM.Clients
{
    [CreateAssetMenu(fileName = "NewOpenAIClientData", menuName = "LLM/OpenAI Client Configuration")]
    public class OpenAIClientData : LLMClientData
    {
        public enum OpenAIApiVariant
        {
            ChatCompletions,
            Responses
        }

        // Keep existing enum members to avoid breaking serialized assets,
        // but update the model IDs + add newer ones at the end.
        public enum OpenAIModel
        {
            GPT_4_5,
            GPT_o3_Mini,
            GPT_4o,
            GPT_4o_Mini,
            GPT_o1,
            GPT_o1_Mini,
            GPT_4_Turbo,
            GPT_3_5_Turbo,
            GPT_5,

            // Newer / recommended additions (appended)
            GPT_5_2,
            GPT_5_Mini,
            GPT_5_Nano,
            GPT_4_1,
            GPT_4_1_Mini,
            GPT_4_1_Nano,
            GPT_o3,
            GPT_o4_Mini,
            GPT_o1_Pro
        }

        public override LLMProvider Provider => LLMProvider.OpenAI;

        [Header("OpenAI API")]
        public OpenAIApiVariant ApiVariant = OpenAIApiVariant.ChatCompletions;

        [Header("OpenAI Model")]
        [Tooltip("Select the OpenAI model to use.")]
        public OpenAIModel selectedModel = OpenAIModel.GPT_5_2;

        private static readonly Dictionary<OpenAIModel, string> ModelStrings = new()
        {
            // Note: gpt-4.5-preview is deprecated, but kept for compatibility.
            { OpenAIModel.GPT_4_5, "gpt-4.5-preview" },

            { OpenAIModel.GPT_o3_Mini, "o3-mini" },
            { OpenAIModel.GPT_4o, "gpt-4o" },
            { OpenAIModel.GPT_4o_Mini, "gpt-4o-mini" },

            // Replace old deprecated preview mapping with stable model IDs:
            { OpenAIModel.GPT_o1, "o1" },
            { OpenAIModel.GPT_o1_Mini, "o1-mini" }, // deprecated on the models page, but kept for compatibility

            { OpenAIModel.GPT_4_Turbo, "gpt-4-turbo" },
            { OpenAIModel.GPT_3_5_Turbo, "gpt-3.5-turbo" },
            { OpenAIModel.GPT_5, "gpt-5" },

            // Newer / recommended:
            { OpenAIModel.GPT_5_2, "gpt-5.2" },
            { OpenAIModel.GPT_5_Mini, "gpt-5-mini" },
            { OpenAIModel.GPT_5_Nano, "gpt-5-nano" },

            { OpenAIModel.GPT_4_1, "gpt-4.1" },
            { OpenAIModel.GPT_4_1_Mini, "gpt-4.1-mini" },
            { OpenAIModel.GPT_4_1_Nano, "gpt-4.1-nano" },

            { OpenAIModel.GPT_o3, "o3" },
            { OpenAIModel.GPT_o4_Mini, "o4-mini" },
            { OpenAIModel.GPT_o1_Pro, "o1-pro" },
        };

        public override string ModelString => ModelStrings.TryGetValue(selectedModel, out var id) ? id : "gpt-5.2";

        // === Secrets only come from env ===
        public override string ApiKey => LLMEnvLoader.Get("OPENAI_API_KEY");

        // === Non-secret defaults come from LLMEnvSettings (with optional env overrides) ===
        public override string BaseUrl
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultBase = settings != null && !string.IsNullOrWhiteSpace(settings.openAIBaseUrl)
                    ? settings.openAIBaseUrl
                    : "https://api.openai.com";

                return LLMEnvLoader.GetOrDefault("OPENAI_BASE_URL", defaultBase);
            }
        }

        public string ChatEndpoint
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultEndpoint = settings != null && !string.IsNullOrWhiteSpace(settings.openAIChatEndpoint)
                    ? settings.openAIChatEndpoint
                    : "/v1/chat/completions";

                return LLMEnvLoader.GetOrDefault("OPENAI_CHAT_ENDPOINT", defaultEndpoint);
            }
        }

        public string ResponsesEndpoint
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultEndpoint = settings != null && !string.IsNullOrWhiteSpace(settings.openAIResponsesEndpoint)
                    ? settings.openAIResponsesEndpoint
                    : "/v1/responses";

                return LLMEnvLoader.GetOrDefault("OPENAI_RESPONSES_ENDPOINT", defaultEndpoint);
            }
        }
    }
}
