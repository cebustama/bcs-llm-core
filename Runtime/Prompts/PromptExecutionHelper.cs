using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BCS.LLM.Core.Clients;

namespace BCS.LLM.Core.Execution
{
    public static class PromptExecutionHelper
    {
        public static async Task<LLMCompletionResult> ExecuteAsync(
            ILLMClient client,
            string prompt,
            string instructions,
            PromptExecutionOptions options = null)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            options ??= new PromptExecutionOptions();

            if (options.IncludeConversationHistoryInRequest)
            {
                return await CreateCompletionMaybeWithFilesAsync(
                    client,
                    prompt,
                    instructions,
                    options.AttachedFileIds);
            }

            var snapshot = CloneHistory(client.ClientConversationHistory);
            client.ClientConversationHistory = new List<ChatMessage>();

            var result = await CreateCompletionMaybeWithFilesAsync(
                client,
                prompt,
                instructions,
                options.AttachedFileIds);

            var newTurn = CloneHistory(client.ClientConversationHistory);
            client.ClientConversationHistory = snapshot;

            if (options.MergeNewTurnBackWhenHistorySuppressed && newTurn.Count > 0)
                client.ClientConversationHistory.AddRange(newTurn);

            return result;
        }

        public static Task<LLMCompletionResult> ExecuteAsync(
            ILLMClient client,
            BCS.LLM.Core.Prompts.PromptBuildResult build,
            PromptExecutionOptions options = null)
        {
            if (build == null) throw new ArgumentNullException(nameof(build));

            return ExecuteAsync(
                client,
                build.UserPromptText,
                build.InstructionsText,
                options);
        }

        private static Task<LLMCompletionResult> CreateCompletionMaybeWithFilesAsync(
            ILLMClient client,
            string prompt,
            string instructions,
            IReadOnlyList<string> fileIds)
        {
            if (fileIds == null || fileIds.Count == 0)
                return client.CreateChatCompletionAsync(prompt, instructions);

            if (client is ILLMResponsesFileClient responsesFileClient)
                return responsesFileClient.CreateResponseWithFilesAsync(
                    prompt, instructions, fileIds);

            var mi = client.GetType().GetMethod(
                "CreateChatCompletionAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string), typeof(string), typeof(IReadOnlyList<string>) },
                modifiers: null);

            if (mi != null && typeof(Task<LLMCompletionResult>).IsAssignableFrom(mi.ReturnType))
            {
                return (Task<LLMCompletionResult>)mi.Invoke(
                    client,
                    new object[] { prompt, instructions, fileIds });
            }

            return client.CreateChatCompletionAsync(prompt, instructions);
        }

        private static List<ChatMessage> CloneHistory(List<ChatMessage> src)
        {
            if (src == null) return new List<ChatMessage>();

            var dst = new List<ChatMessage>(src.Count);
            foreach (var m in src)
            {
                if (m == null) continue;
                dst.Add(new ChatMessage
                {
                    role = m.role,
                    content = m.content
                });
            }

            return dst;
        }
    }
}