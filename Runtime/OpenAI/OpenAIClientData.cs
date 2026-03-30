using BCS.LLM.Core.Clients;
using BCS.LLM.Core.Env;
using System.Collections.Generic;
using UnityEngine;

namespace BCS.LLM.Core.OpenAI
{
    [CreateAssetMenu(fileName = "NewOpenAIClientData", menuName = "LLM/OpenAI Client Configuration")]
    public class OpenAIClientData : LLMClientData
    {
        public enum OpenAIApiVariant
        {
            ChatCompletions,
            Responses
        }

        // IMPORTANT:
        // - Keep legacy enum members in place to avoid breaking serialized Unity assets.
        // - Prefer newer/current models for new configs.
        // - Current GPT-5 family variants now include both the generic GPT-5 mini/nano IDs
        //   and the newer GPT-5.4 mini/nano IDs; they are distinct model IDs and should
        //   not be conflated.
        public enum OpenAIModel
        {
            // Legacy / compatibility (existing order preserved)
            GPT_4_5,
            GPT_o3_Mini,
            GPT_4o,
            GPT_4o_Mini,
            GPT_o1,
            GPT_o1_Mini,
            GPT_4_Turbo,
            GPT_3_5_Turbo,
            GPT_5,

            // Current / recommended additions (appended for serialization safety)
            GPT_5_2,
            GPT_5_2_Pro,
            GPT_5_4,
            GPT_5_4_Mini,
            GPT_5_4_Nano,
            GPT_5_4_Pro,
            GPT_5_Mini,
            GPT_5_Nano,

            // Other still-available API models worth keeping as compatibility options
            GPT_4_1,
            GPT_4_1_Mini,
            GPT_4_1_Nano,
            GPT_o3,
            GPT_o4_Mini,
            GPT_o1_Pro,

            // Specialized / optional
            GPT_5_Codex,
            GPT_5_2_Codex,
            GPT_5_3_Codex,
            GPT_5_2_Chat_Latest,
            GPT_5_3_Chat_Latest,
            GPT_o3_Deep_Research,
            GPT_o4_Mini_Deep_Research,
            Computer_Use_Preview
        }

        public override LLMProvider Provider => LLMProvider.OpenAI;

        [Header("OpenAI API")]
        public OpenAIApiVariant ApiVariant = OpenAIApiVariant.Responses;

        [Header("OpenAI Model")]
        [Tooltip("Select the OpenAI model to use. Prefer GPT_5_4, GPT_5_4_Mini, GPT_5_4_Nano, or GPT_5_2 for current production use.")]
        public OpenAIModel selectedModel = OpenAIModel.GPT_5_4;

        private static readonly Dictionary<OpenAIModel, string> ModelStrings = new()
        {
            // Legacy / compatibility mappings
            { OpenAIModel.GPT_4_5, "gpt-4.5-preview" },
            { OpenAIModel.GPT_o3_Mini, "o3-mini" },
            { OpenAIModel.GPT_4o, "gpt-4o" },
            { OpenAIModel.GPT_4o_Mini, "gpt-4o-mini" },
            { OpenAIModel.GPT_o1, "o1" },
            { OpenAIModel.GPT_o1_Mini, "o1-mini" },
            { OpenAIModel.GPT_4_Turbo, "gpt-4-turbo" },
            { OpenAIModel.GPT_3_5_Turbo, "gpt-3.5-turbo" },
            { OpenAIModel.GPT_5, "gpt-5" },

            // Current / recommended
            { OpenAIModel.GPT_5_2, "gpt-5.2" },
            { OpenAIModel.GPT_5_2_Pro, "gpt-5.2-pro" },
            { OpenAIModel.GPT_5_4, "gpt-5.4" },
            { OpenAIModel.GPT_5_4_Mini, "gpt-5.4-mini" },
            { OpenAIModel.GPT_5_4_Nano, "gpt-5.4-nano" },
            { OpenAIModel.GPT_5_4_Pro, "gpt-5.4-pro" },
            { OpenAIModel.GPT_5_Mini, "gpt-5-mini" },
            { OpenAIModel.GPT_5_Nano, "gpt-5-nano" },

            // Other available / compatibility options
            { OpenAIModel.GPT_4_1, "gpt-4.1" },
            { OpenAIModel.GPT_4_1_Mini, "gpt-4.1-mini" },
            { OpenAIModel.GPT_4_1_Nano, "gpt-4.1-nano" },
            { OpenAIModel.GPT_o3, "o3" },
            { OpenAIModel.GPT_o4_Mini, "o4-mini" },
            { OpenAIModel.GPT_o1_Pro, "o1-pro" },

            // Specialized / optional
            { OpenAIModel.GPT_5_Codex, "gpt-5-codex" },
            { OpenAIModel.GPT_5_2_Codex, "gpt-5.2-codex" },
            { OpenAIModel.GPT_5_3_Codex, "gpt-5.3-codex" },
            { OpenAIModel.GPT_5_2_Chat_Latest, "gpt-5.2-chat-latest" },
            { OpenAIModel.GPT_5_3_Chat_Latest, "gpt-5.3-chat-latest" },
            { OpenAIModel.GPT_o3_Deep_Research, "o3-deep-research" },
            { OpenAIModel.GPT_o4_Mini_Deep_Research, "o4-mini-deep-research" },
            { OpenAIModel.Computer_Use_Preview, "computer-use-preview" },
        };

        public override string ModelString => ModelStrings.TryGetValue(selectedModel, out var id) ? id : "gpt-5.4";

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

        public string FilesEndpoint
        {
            get
            {
                var settings = Resources.Load<LLMEnvSettings>(LLMEnvLoader.SettingsResourceName);
                var defaultEndpoint = settings != null && !string.IsNullOrWhiteSpace(settings.openAIFilesEndpoint)
                    ? settings.openAIFilesEndpoint
                    : "/v1/files";

                return LLMEnvLoader.GetOrDefault("OPENAI_FILES_ENDPOINT", defaultEndpoint);
            }
        }
    }
}
