using BCS.LLM.Core.Clients;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BCS.LLM.Core.OpenAI
{
    public class OpenAILLMClient : LLMClientBase, ILLMFileClient, ILLMResponsesFileClient
    {
        private readonly HttpClient httpClient;
        private readonly OpenAIClientData config;

        private readonly string apiUrl;
        private readonly string apiKey;
        private readonly string chatEndpoint;
        private readonly string responsesEndpoint;
        private readonly string filesEndpoint;

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
            filesEndpoint = NormalizeEndpoint(config.FilesEndpoint, "/v1/files");

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
                Debug.LogWarning("OPENAI_API_KEY is missing. " +
                    "Requests will fail until it is set.");
        }

        public override Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt)
            => CreateChatCompletionAsync(prompt, SystemInstructions);

        public override async Task<LLMCompletionResult> CreateChatCompletionAsync(string prompt, string instructions)
        {
            if (config.ApiVariant == OpenAIClientData.OpenAIApiVariant.Responses)
                return await CreateViaResponsesAsync(prompt, instructions);

            return await CreateViaChatCompletionsAsync(prompt, instructions);
        }

        /// <summary>
        /// Optional overload to include one or more uploaded PDF file_ids in a Responses request.
        /// If fileIds is null/empty, it behaves exactly like CreateChatCompletionAsync(prompt, instructions).
        /// </summary>
        public async Task<LLMCompletionResult> CreateChatCompletionAsync(
            string prompt,
            string instructions,
            IReadOnlyList<string> fileIds)
        {
            if (fileIds == null || fileIds.Count == 0)
                return await CreateChatCompletionAsync(prompt, instructions);

            if (config.ApiVariant != OpenAIClientData.OpenAIApiVariant.Responses)
            {
                Debug.LogWarning("fileIds were provided, but ApiVariant is not Responses. " +
                    "Falling back to text-only request.");
                return await CreateChatCompletionAsync(prompt, instructions);
            }

            return await CreateViaResponsesWithFilesAsync(prompt, instructions, fileIds);
        }

        // -------------------------
        // Chat Completions (text-only)
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
                    Debug.LogError($"OpenAI Chat API Error: " +
                        $"{response.StatusCode} - {errorResponse}");
                    return EmptyResult();
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
                return EmptyResult();
            }
        }

        // -------------------------
        // Responses (text-only) - KEEP EXISTING BEHAVIOR
        // -------------------------
        private async Task<LLMCompletionResult> CreateViaResponsesAsync(
            string prompt, string instructions)
        {
            var inputList = new List<ResponsesInputMessage>();

            // Keep existing behavior:
            // include history (except system/developer) + user prompt.
            if (ClientConversationHistory != null && ClientConversationHistory.Count > 0)
            {
                foreach (var m in ClientConversationHistory)
                {
                    if (m == null) continue;
                    if (string.IsNullOrWhiteSpace(m.role) || 
                        string.IsNullOrWhiteSpace(m.content)) continue;

                    var r = m.role.Trim().ToLowerInvariant();
                    if (r == "system" || r == "developer") continue;

                    inputList.Add(
                        new ResponsesInputMessage { role = r, content = m.content });
                }
            }

            inputList.Add(new ResponsesInputMessage { role = "user", content = prompt ?? "" });

            var body = new ResponsesRequestBody
            {
                model = Model,
                input = inputList.ToArray(),
                instructions = instructions ?? "",
                max_output_tokens = MaxOutputTokens,

                // Keep only params that are accepted by your current
                // Responses request shape.
                temperature = Temperature,
                top_p = TopP

                // IMPORTANT:
                // - Do NOT send frequency_penalty to /v1/responses
                // (can cause unknown_parameter).
                // - Do NOT send stop either (may cause unknown_parameter
                // depending on schema).
            };

            var requestBody = JsonConvert.SerializeObject(
                body,
                Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
            );

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(responsesEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError(
                        $"OpenAI Responses API Error: " +
                        $"{response.StatusCode} - {errorResponse}");
                    return EmptyResult();
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var parsed = 
                    JsonConvert.DeserializeObject<ResponsesResponseBody>(responseBody);

                string result = ExtractResponsesText(parsed);

                AddMessageToHistory("user", prompt);
                AddMessageToHistory("assistant", result);

                int inputTokens = parsed?.usage?.input_tokens ?? 0;
                int cachedInputTokens = 
                    parsed?.usage?.input_tokens_details?.cached_tokens ?? 0;
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
                return EmptyResult();
            }
        }

        // -------------------------
        // Responses (WITH FILE IDS) - OPT-IN
        // -------------------------
        private async Task<LLMCompletionResult> CreateViaResponsesWithFilesAsync(
            string prompt,
            string instructions,
            IReadOnlyList<string> fileIds)
        {
            // Per OpenAI "File inputs" guide: include one or more
            // { type:"input_file", file_id:"..." } parts
            // plus an { type:"input_text", text:"..." }
            // part in a user message.

            var inputList = new List<ResponsesFileInputMessage>();

            // In file-mode we keep things conservative:
            // - include ONLY user history as input_text parts
            // (avoids any role/type edge cases).
            if (ClientConversationHistory != null && ClientConversationHistory.Count > 0)
            {
                foreach (var m in ClientConversationHistory)
                {
                    if (m == null) continue;
                    if (string.IsNullOrWhiteSpace(m.role) || 
                        string.IsNullOrWhiteSpace(m.content)) continue;

                    var r = m.role.Trim().ToLowerInvariant();
                    if (r != "user") continue;

                    inputList.Add(new ResponsesFileInputMessage
                    {
                        role = "user",
                        content = new[]
                        {
                            ResponsesContentPart.InputText(m.content)
                        }
                    });
                }
            }

            var parts = new List<ResponsesContentPart>();

            foreach (var id in fileIds)
            {
                if (string.IsNullOrWhiteSpace(id)) continue;
                parts.Add(ResponsesContentPart.InputFileId(id.Trim()));
            }

            parts.Add(ResponsesContentPart.InputText(prompt ?? ""));

            inputList.Add(new ResponsesFileInputMessage
            {
                role = "user",
                content = parts.ToArray()
            });

            var body = new ResponsesFileRequestBody
            {
                model = Model,
                input = inputList.ToArray(),
                instructions = instructions ?? "",
                max_output_tokens = MaxOutputTokens,
                temperature = Temperature,
                top_p = TopP
            };

            var requestBody = JsonConvert.SerializeObject(
                body,
                Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
            );

            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync(responsesEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Debug.LogError(
                        $"OpenAI Responses (files) API Error: " +
                        $"{response.StatusCode} - {errorResponse}");
                    return EmptyResult();
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var parsed = 
                    JsonConvert.DeserializeObject<ResponsesResponseBody>(responseBody);

                string result = ExtractResponsesText(parsed);

                // History: store only the text prompt + assistant result
                // (file ids are request-scoped)
                AddMessageToHistory("user", prompt);
                AddMessageToHistory("assistant", result);

                int inputTokens = parsed?.usage?.input_tokens ?? 0;
                int cachedInputTokens = 
                    parsed?.usage?.input_tokens_details?.cached_tokens ?? 0;
                int outputTokens = parsed?.usage?.output_tokens ?? 0;
                int reasoningTokens = 
                    parsed?.usage?.output_tokens_details?.reasoning_tokens ?? 0;

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
                Debug.LogError($"OpenAI Responses (files) exception: {ex}");
                return EmptyResult();
            }
        }

        private static LLMCompletionResult EmptyResult()
        {
            return new LLMCompletionResult
            {
                OutputText = null,
                InputTokens = 0,
                CachedInputTokens = 0,
                OutputTokens = 0,
                ReasoningTokens = 0
            };
        }

        private List<ChatMessage> BuildChatMessages(
            string instructions, string prompt, bool includeHistory)
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { role = "system", content = instructions ?? "" }
            };

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

        public async Task<LLMFileUploadResult> UploadFileAsync(
            string filePath, string purpose = "user_data")
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("filePath is null/empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            if (!string.Equals(
                Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only PDF files are supported.", nameof(filePath));

            // Multipart form: purpose + file
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(
                string.IsNullOrWhiteSpace(purpose) ? "user_data" : purpose), "purpose");

            await using var fs = File.OpenRead(filePath);
            using var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await httpClient.PostAsync(filesEndpoint, form);

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Debug.LogError($"OpenAI Files API Error: {response.StatusCode} - {body}");
                throw new InvalidOperationException(
                    $"OpenAI file upload failed: {response.StatusCode}");
            }

            var parsed = JsonConvert.DeserializeObject<OpenAIFileUploadResponseDto>(body);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.id))
                throw new InvalidOperationException(
                    "OpenAI file upload returned no file id.");

            return new LLMFileUploadResult
            {
                FileId = parsed.id,
                Filename = parsed.filename ?? Path.GetFileName(filePath),
                Bytes = parsed.bytes
            };
        }

        public Task<LLMCompletionResult> CreateResponseWithFilesAsync(
        string prompt,
        string instructions,
        IReadOnlyList<string> fileIds)
        {
            return CreateChatCompletionAsync(prompt, instructions, fileIds);
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
        // DTOs: Responses (text-only)
        // -------------------------
        private class ResponsesRequestBody
        {
            public string model { get; set; }
            public ResponsesInputMessage[] input { get; set; }
            public string instructions { get; set; }
            public int max_output_tokens { get; set; }

            public float temperature { get; set; }
            public float top_p { get; set; }
        }

        private class ResponsesInputMessage
        {
            public string role { get; set; }
            public string content { get; set; }
        }

        // -------------------------
        // DTOs: Responses (with file parts)
        // -------------------------
        private class ResponsesFileRequestBody
        {
            public string model { get; set; }
            public ResponsesFileInputMessage[] input { get; set; }
            public string instructions { get; set; }
            public int max_output_tokens { get; set; }
            public float temperature { get; set; }
            public float top_p { get; set; }
        }

        private class ResponsesFileInputMessage
        {
            public string role { get; set; }
            public ResponsesContentPart[] content { get; set; }
        }

        private class ResponsesContentPart
        {
            public string type { get; set; }   // "input_text" | "input_file"

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string text { get; set; }   // only for input_text

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string file_id { get; set; } // only for input_file

            public static ResponsesContentPart InputText(string t)
                => new ResponsesContentPart { type = "input_text", text = t ?? "" };

            public static ResponsesContentPart InputFileId(string id)
                => new ResponsesContentPart { type = "input_file", file_id = id };
        }

        // -------------------------
        // DTOs: Responses response
        // -------------------------
        private class ResponsesResponseBody
        {
            public OutputItem[] output { get; set; }
            public ResponsesUsage usage { get; set; }
        }

        private class OpenAIFileUploadResponseDto
        {
            public string id { get; set; }
            public string filename { get; set; }
            public long bytes { get; set; }
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
