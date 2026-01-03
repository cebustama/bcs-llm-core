using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Eon.Narrative.LLM.Clients
{
    public class OpenAILLMClient : LLMClientBase
    {
        private readonly HttpClient httpClient;
        private readonly OpenAIClientData config;

        private readonly string apiUrl;
        private readonly string apiKey;
        private readonly string chatEndpoint;
        private readonly string responsesEndpoint;

        public OpenAILLMClient(OpenAIClientData config)
        {
            this.config = config;

            Model = config.ModelString;
            Temperature = config.Temperature;
            MaxOutputTokens = config.MaxOutputTokens;
            TopP = config.TopP;
            FrequencyPenalty = config.FrequencyPenalty;
            StopSequences = config.StopSequences;

            apiUrl = NormalizeBaseUrl(config.BaseUrl);
            apiKey = config.ApiKey;

            chatEndpoint = NormalizeEndpoint(config.ChatEndpoint, "/v1/chat/completions");
            responsesEndpoint = NormalizeEndpoint(config.ResponsesEndpoint, "/v1/responses");

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiUrl, UriKind.Absolute)
            };
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            SystemInstructions = config.SystemInstructions;
            ClientConversationHistory = new List<ChatMessage>();

            InputUSDPerMTokens = config.InputUSDPerMTokens;
            CachedInputUSDPerMTokens = config.CachedInputUSDPerMTokens;
            OutputUSDPerMTokens = config.OutputUSDPerMTokens;

            if (string.IsNullOrWhiteSpace(apiKey))
                Debug.LogWarning("OPENAI_API_KEY is missing. Requests will fail until it is set.");
        }

        public override Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt)
            => CreateChatCompletionAsync(prompt, SystemInstructions);

        public override async Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt, string instructions)
        {
            if (config.ApiVariant == OpenAIClientData.OpenAIApiVariant.Responses)
                return await CreateViaResponsesAsync(prompt, instructions);

            return await CreateViaChatCompletionsAsync(prompt, instructions);
        }

        // -------------------------
        // Chat Completions
        // -------------------------
        private async Task<LLMCompletionResult> CreateViaChatCompletionsAsync(string prompt, string instructions)
        {
            var messages = BuildChatMessages(instructions, prompt, includeHistory: true);

            var body = new ChatRequestBody
            {
                model = Model,
                temperature = Temperature,
                max_completion_tokens = MaxOutputTokens,
                top_p = TopP,
                frequency_penalty = FrequencyPenalty,
                messages = messages.ToArray(),
                stop = (StopSequences != null && StopSequences.Count > 0) ?
                    StopSequences.ToArray() : null
            };

            var requestBody = JsonConvert.SerializeObject(body, Formatting.None);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(chatEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"OpenAI Chat API Error: {response.StatusCode} - {errorResponse}");
                    return new LLMCompletionResult
                    {
                        OutputText = null,
                        InputTokens = 0,
                        CachedInputTokens = 0,
                        OutputTokens = 0,
                        ReasoningTokens = 0
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<ChatResponseBody>(responseBody);

                string result = parsed?.choices?.FirstOrDefault()?.message?.content;

                AddMessageToHistory("user", prompt);
                AddMessageToHistory("assistant", result);

                int inputTokens = parsed?.usage?.prompt_tokens ?? 0;
                int cachedInputTokens = parsed?.usage?.prompt_tokens_details?.cached_tokens ?? 0;
                int outputTokens = parsed?.usage?.completion_tokens ?? 0;
                int reasoningTokens = parsed?.usage?.completion_tokens_details?.reasoning_tokens ?? 0;

                if (cachedInputTokens > inputTokens) cachedInputTokens = inputTokens;

                return new LLMCompletionResult
                {
                    OutputText = result,
                    InputTokens = inputTokens,
                    CachedInputTokens = cachedInputTokens,
                    OutputTokens = outputTokens,
                    ReasoningTokens = reasoningTokens
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenAI Chat exception: {ex}");
                return new LLMCompletionResult
                {
                    OutputText = null,
                    InputTokens = 0,
                    CachedInputTokens = 0,
                    OutputTokens = 0,
                    ReasoningTokens = 0
                };
            }
        }

        // -------------------------
        // Responses
        // -------------------------
        private async Task<LLMCompletionResult> CreateViaResponsesAsync(string prompt, string instructions)
        {
            var inputList = new List<ResponsesInputMessage>();

            // Keep existing behavior: include history (except system/developer) + user prompt.
            if (ClientConversationHistory != null && ClientConversationHistory.Count > 0)
            {
                foreach (var m in ClientConversationHistory)
                {
                    if (m == null) continue;
                    if (string.IsNullOrWhiteSpace(m.role) || string.IsNullOrWhiteSpace(m.content)) continue;

                    var r = m.role.Trim().ToLowerInvariant();
                    if (r == "system" || r == "developer") continue;

                    inputList.Add(new ResponsesInputMessage { role = r, content = m.content });
                }
            }

            inputList.Add(new ResponsesInputMessage { role = "user", content = prompt ?? "" });

            var body = new ResponsesRequestBody
            {
                model = Model,
                input = inputList.ToArray(),
                instructions = instructions ?? "",
                max_output_tokens = MaxOutputTokens,

                // Keep only params that are accepted by your current Responses request shape.
                temperature = Temperature,
                top_p = TopP

                // IMPORTANT (Option B):
                // - Do NOT send frequency_penalty to /v1/responses (causes unknown_parameter).
                // - Do NOT send stop either (likely to cause unknown_parameter depending on schema).
            };

            var requestBody = JsonConvert.SerializeObject(body, Formatting.None);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(responsesEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"OpenAI Responses API Error: {response.StatusCode} - {errorResponse}");
                    return new LLMCompletionResult
                    {
                        OutputText = null,
                        InputTokens = 0,
                        CachedInputTokens = 0,
                        OutputTokens = 0,
                        ReasoningTokens = 0
                    };
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<ResponsesResponseBody>(responseBody);

                string result = ExtractResponsesText(parsed);

                AddMessageToHistory("user", prompt);
                AddMessageToHistory("assistant", result);

                int inputTokens = parsed?.usage?.input_tokens ?? 0;
                int cachedInputTokens = parsed?.usage?.input_tokens_details?.cached_tokens ?? 0;
                int outputTokens = parsed?.usage?.output_tokens ?? 0;
                int reasoningTokens = parsed?.usage?.output_tokens_details?.reasoning_tokens ?? 0;

                if (cachedInputTokens > inputTokens) cachedInputTokens = inputTokens;

                return new LLMCompletionResult
                {
                    OutputText = result,
                    InputTokens = inputTokens,
                    CachedInputTokens = cachedInputTokens,
                    OutputTokens = outputTokens,
                    ReasoningTokens = reasoningTokens
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"OpenAI Responses exception: {ex}");
                return new LLMCompletionResult
                {
                    OutputText = null,
                    InputTokens = 0,
                    CachedInputTokens = 0,
                    OutputTokens = 0,
                    ReasoningTokens = 0
                };
            }
        }

        private List<ChatMessage> BuildChatMessages(string instructions, string prompt, bool includeHistory)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { role = "system", content = instructions ?? "" }
            };

            if (includeHistory && ClientConversationHistory != null && ClientConversationHistory.Count > 0)
            {
                foreach (var m in ClientConversationHistory)
                {
                    if (m == null) continue;
                    if (string.IsNullOrWhiteSpace(m.role) || string.IsNullOrWhiteSpace(m.content)) continue;

                    var r = m.role.Trim().ToLowerInvariant();
                    if (r == "system" || r == "developer") continue;

                    messages.Add(m);
                }
            }

            messages.Add(new ChatMessage { role = "user", content = prompt ?? "" });
            return messages;
        }

        private static string ExtractResponsesText(ResponsesResponseBody parsed)
        {
            if (parsed?.output == null) return null;

            foreach (var item in parsed.output)
            {
                if (!string.Equals(item.type, "message", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (item.content == null) continue;

                var textPart = item.content.FirstOrDefault(c =>
                    string.Equals(c.type, "output_text", StringComparison.OrdinalIgnoreCase));

                if (textPart != null && !string.IsNullOrWhiteSpace(textPart.text))
                    return textPart.text;
            }

            return null;
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return "https://api.openai.com/";

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
        // DTOs: Chat
        // -------------------------
        private class ChatRequestBody
        {
            public string model { get; set; }
            public float temperature { get; set; }
            public int max_completion_tokens { get; set; }
            public float top_p { get; set; }
            public float frequency_penalty { get; set; }
            public string[] stop { get; set; }
            public ChatMessage[] messages { get; set; }
        }

        private class ChatResponseBody
        {
            public Choice[] choices { get; set; }
            public Usage usage { get; set; }
        }

        private class Choice
        {
            public ChatMessage message { get; set; }
            public string finish_reason { get; set; }
        }

        private class Usage
        {
            public int prompt_tokens { get; set; }
            public PromptTokensDetails prompt_tokens_details { get; set; }

            public int completion_tokens { get; set; }
            public CompletionTokensDetails completion_tokens_details { get; set; }

            public int total_tokens { get; set; }
        }

        private class PromptTokensDetails
        {
            public int cached_tokens { get; set; }
        }

        private class CompletionTokensDetails
        {
            public int reasoning_tokens { get; set; }
        }

        // -------------------------
        // DTOs: Responses
        // -------------------------
        private class ResponsesRequestBody
        {
            public string model { get; set; }
            public ResponsesInputMessage[] input { get; set; }
            public string instructions { get; set; }
            public int max_output_tokens { get; set; }

            public float temperature { get; set; }
            public float top_p { get; set; }

            // Option B: removed unsupported fields:
            // - frequency_penalty
            // - stop
        }

        private class ResponsesInputMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        private class ResponsesResponseBody
        {
            public OutputItem[] output { get; set; }
            public ResponsesUsage usage { get; set; }
        }

        private class OutputItem
        {
            public string type { get; set; }     // "message"
            public string role { get; set; }     // "assistant"
            public ContentItem[] content { get; set; }
        }

        private class ContentItem
        {
            public string type { get; set; }     // "output_text"
            public string text { get; set; }
        }

        private class ResponsesUsage
        {
            public int input_tokens { get; set; }
            public InputTokensDetails input_tokens_details { get; set; }

            public int output_tokens { get; set; }
            public OutputTokensDetails output_tokens_details { get; set; }

            public int total_tokens { get; set; }
        }

        private class InputTokensDetails
        {
            public int cached_tokens { get; set; }
        }

        private class OutputTokensDetails
        {
            public int reasoning_tokens { get; set; }
        }
    }
}
