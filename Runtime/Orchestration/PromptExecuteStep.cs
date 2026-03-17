using System;
using System.Threading;
using System.Threading.Tasks;
using BCS.LLM.Core.Clients;
using BCS.LLM.Core.Execution;

namespace BCS.LLM.Core.Orchestration
{
    public sealed class PromptExecuteStep<TState, TInput> : IWorkflowStep<TState>
        where TState : PromptWorkflowState<TInput>
    {
        private readonly ILLMClient _client;

        public string StepId { get; }

        public PromptExecuteStep(
            ILLMClient client,
            string stepId = "prompt_execute")
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            StepId = string.IsNullOrWhiteSpace(stepId) ? "prompt_execute" : stepId;
        }

        public async Task<WorkflowStepResult> RunAsync(
            TState state,
            CancellationToken cancellationToken = default)
        {
            if (state == null)
            {
                return WorkflowStepResult.Stop(
                    "MissingState",
                    "Prompt execute step received a null workflow state.");
            }

            if (state.BuildResult == null)
            {
                return WorkflowStepResult.Stop(
                    "MissingBuildResult",
                    "Prompt execute step requires a non-null PromptBuildResult.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            state.CompletionResult = await PromptExecutionHelper.ExecuteAsync(
                _client,
                state.BuildResult,
                state.ExecutionOptions);

            state.OutputText = state.CompletionResult?.OutputText;

            return WorkflowStepResult.ContinueStep("Prompt execution completed.");
        }
    }
}