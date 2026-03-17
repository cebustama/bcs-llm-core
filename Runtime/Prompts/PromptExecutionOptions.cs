using System.Collections.Generic;

namespace BCS.LLM.Core.Execution
{
    public sealed class PromptExecutionOptions
    {
        public bool IncludeConversationHistoryInRequest = true;
        public bool MergeNewTurnBackWhenHistorySuppressed = true;

        // Provider-level attachment ids, resolved outside the PromptBuilder.
        public IReadOnlyList<string> AttachedFileIds;
    }
}