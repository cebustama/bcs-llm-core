using System.Collections.Generic;
using BCS.LLM.Core.Clients;
using BCS.LLM.Core.Execution;
using BCS.LLM.Core.Prompts;
using BCS.LLM.Core.Retry;
using BCS.LLM.Core.Validation;

namespace BCS.LLM.Core.Orchestration
{
    public abstract class PromptWorkflowState<TInput>
    {
        public TInput Input;
        public int AttemptIndex;

        public PromptBuildResult BuildResult;
        public PromptExecutionOptions ExecutionOptions;

        public LLMCompletionResult CompletionResult;
        public string OutputText;

        public readonly List<ValidationResult> ValidationTrail = new();
        public readonly List<RepairHint> RepairHints = new();
        public readonly List<RetryDirective> RetryDirectives = new();
    }
}