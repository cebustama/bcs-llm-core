using BCS.LLM.Core.Clients;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BCS.LLM.Core.Anthropic
{
    /// <summary>
    /// Anthropic Messages API client. Mirrors OpenAILLMClient's structural
    /// conventions (HttpClient + Newtonsoft.Json, extends LLMClientBase) but
    /// targets <c>POST {BaseUrl}/v1/messages</c> with Anthropic's request shape:
    /// system prompt as top-level field, messages array of user/assistant pairs,
    /// <c>x-api-key</c> and <c>anthropic-version</c> headers.
    /// </summary>
    public class AnthropicLLMClient : LLMClientBase
    {
        private readonly HttpClient httpClient;
        private readonly AnthropicClientData config;

        private readonly string apiUrl;
        private readonly string apiKey;
        private readonly string messagesEndpoint;

        public AnthropicLLMClient(AnthropicClientData config)
        {
            this.config = config;

            Model = config.ModelString;
            Temperature = config.Temperature;
            MaxOutputTokens = config.MaxOutputTokens;
            TopP = config.TopP;
            FrequencyPenalty = config.FrequencyPenalty; // Anthropic ignores this; kept for parity
            StopSequences = config.StopSequences;

            apiUrl = NormalizeBaseUrl(config.BaseUrl);
            apiKey = config.ApiKey;
            messagesEndpoint = NormalizeEndpoint(config.MessagesEndpoint, "/v1/messages");

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiUrl, UriKind.Absolute)
            };
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey ?? "");
            httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicClientData.ApiVersion);

            SystemInstructions = config.SystemInstructions;
            ClientConversationHistory = new List<ChatMessage>();

            InputUSDPerMTokens = config.InputUSDPerMTokens;
            CachedInputUSDPerMTokens = config.CachedInputUSDPerMTokens;
            OutputUSDPerMTokens = config.OutputUSDPerMTokens;

            if (string.IsNullOrWhiteSpace(apiKey))
                Debug.LogWarning("ANTHROPIC_API_KEY is missing. " +
                    "Requests will fail until it is set.");
        }

        public override Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt)
            => CreateChatCompletionAsync(prompt, SystemInstructions);

        public override async Task<LLMCompletionResult> CreateChatCompletionAsync(
            string prompt, string instructions)
        {
            var messages = BuildMessages(prompt, includeHistory: true);

            // Anthropic rejects specifying BOTH temperature and top_p for newer
            // models ("`temperature` and `top_p` cannot both be specified").
            // We send temperature only and omit top_p. top_p = 1.0 is a no-op
            // anyway. The DTO fields are nullable + NullValueHandling.Ignore, so
            // a null is dropped from the JSON. To sample by top_p instead, set
            // top_p here and leave temperature null.
            var body = new MessagesRequestBody
            {
                model = Model,
                max_tokens = MaxOutputTokens > 0 ? MaxOutputTokens : 1024,
                system = string.IsNullOrEmpty(instructions) ? null : instructions,
                messages = messages.ToArray(),
                temperature = Temperature,
                top_p = null,
                stop_sequences = (StopSequences != null && StopSequences.Count > 0)
                    ? StopSequences.ToArray() : null,
            };

            var requestBody = JsonConvert.SerializeObject(
                body,
                Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(messagesEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"Anthropic Messages API Error: " +
                        $"{response.StatusCode} - {errorResponse}");
                    return EmptyResult();
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<MessagesResponseBody>(responseBody);

                string result = ExtractText(parsed);

                AddMessageToHistory("user", prompt);
                AddMessageToHistory("assistant", result);

                int inputTokens = parsed?.usage?.input_tokens ?? 0;
                int cachedInputTokens = parsed?.usage?.cache_read_input_tokens ?? 0;
                int outputTokens = parsed?.usage?.output_tokens ?? 0;

                if (cachedInputTokens > inputTokens) cachedInputTokens = inputTokens;

                return new LLMCompletionResult
                {
                    OutputText = result,
                    InputTokens = inputTokens,
                    CachedInputTokens = cachedInputTokens,
                    OutputTokens = outputTokens,
                    ReasoningTokens = 0, // Anthropic does not report this separately for non-thinking models
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Anthropic Messages exception: {ex}");
                return EmptyResult();
            }
        }

        // -------------------------
        // Helpers
        // -------------------------

        /// <summary>
        /// Build the Anthropic messages array. Anthropic only accepts
        /// user/assistant roles in the messages array; system goes in the
        /// top-level <c>system</c> field, so any system/developer entries
        /// in conversation history are dropped here.
        /// </summary>
        private List<AnthropicMessage> BuildMessages(string prompt, bool includeHistory)
        {
            var messages = new List<AnthropicMessage>();

            if (includeHistory
                && ClientConversationHistory != null
                && ClientConversationHistory.Count > 0)
            {
                foreach (var m in ClientConversationHistory)
                {
                    if (m == null) continue;
                    if (string.IsNullOrWhiteSpace(m.role)
                        || string.IsNullOrWhiteSpace(m.content)) continue;

                    var r = m.role.Trim().ToLowerInvariant();
                    if (r != "user" && r != "assistant") continue;

                    messages.Add(new AnthropicMessage { role = r, content = m.content });
                }
            }

            messages.Add(new AnthropicMessage { role = "user", content = prompt ?? "" });
            return messages;
        }

        private static string ExtractText(MessagesResponseBody parsed)
        {
            if (parsed?.content == null) return null;

            var textBlock = parsed.content.FirstOrDefault(c =>
                string.Equals(c.type, "text", StringComparison.OrdinalIgnoreCase));

            return textBlock?.text;
        }

        private static LLMCompletionResult EmptyResult() => new LLMCompletionResult
        {
            OutputText = null,
            InputTokens = 0,
            CachedInputTokens = 0,
            OutputTokens = 0,
            ReasoningTokens = 0,
        };

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "https://api.anthropic.com/";

            baseUrl = baseUrl.Trim();
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            return baseUrl;
        }

        private static string NormalizeEndpoint(string endpoint, string fallback)
        {
            var e = string.IsNullOrWhiteSpace(endpoint) ? fallback : endpoint.Trim();
            if (!e.StartsWith("/")) e = "/" + e;
            return e;
        }

        // -------------------------
        // DTOs: Messages
        // -------------------------

        private class MessagesRequestBody
        {
            public string model { get; set; }
            public int max_tokens { get; set; }
            public string system { get; set; }
            public AnthropicMessage[] messages { get; set; }
            public float? temperature { get; set; }
            public float? top_p { get; set; }
            public string[] stop_sequences { get; set; }
        }

        private class AnthropicMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        private class MessagesResponseBody
        {
            public string id { get; set; }
            public string type { get; set; }
            public string role { get; set; }
            public string model { get; set; }
            public ContentBlock[] content { get; set; }
            public string stop_reason { get; set; }
            public string stop_sequence { get; set; }
            public Usage usage { get; set; }
        }

        private class ContentBlock
        {
            public string type { get; set; }
            public string text { get; set; }
        }

        private class Usage
        {
            public int input_tokens { get; set; }
            public int cache_creation_input_tokens { get; set; }
            public int cache_read_input_tokens { get; set; }
            public int output_tokens { get; set; }
        }
    }
}