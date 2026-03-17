using System;
using System.Threading;
using System.Threading.Tasks;
using BCS.LLM.Core.Prompts;

namespace BCS.LLM.Core.Orchestration
{
    public sealed class PromptBuildStep<TState, TInput> : IWorkflowStep<TState>
        where TState : PromptWorkflowState<TInput>
    {
        private readonly IPromptBuilder<TInput> _promptBuilder;

        public string StepId { get; }

        public PromptBuildStep(
            IPromptBuilder<TInput> promptBuilder,
            string stepId = "prompt_build")
        {
            _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            StepId = string.IsNullOrWhiteSpace(stepId) ? "prompt_build" : stepId;
        }

        public Task<WorkflowStepResult> RunAsync(
            TState state,
            CancellationToken cancellationToken = default)
        {
            if (state == null)
            {
                return Task.FromResult(
                    WorkflowStepResult.Stop("MissingState", "Prompt build step received a null workflow state."));
            }

            state.BuildResult = _promptBuilder.Build(state.Input);

            if (state.BuildResult == null)
            {
                return Task.FromResult(
                    WorkflowStepResult.Stop("NullBuildResult", "Prompt builder returned null."));
            }

            return Task.FromResult(WorkflowStepResult.ContinueStep("Prompt build completed."));
        }
    }
}