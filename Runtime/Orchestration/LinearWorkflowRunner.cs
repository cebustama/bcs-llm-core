using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BCS.LLM.Core.Orchestration
{
    public sealed class LinearWorkflowRunner<TState>
    {
        private readonly IReadOnlyList<IWorkflowStep<TState>> _steps;
        private readonly IWorkflowReentryAdapter<TState> _reentryAdapter;

        public int MaxAttempts { get; }

        public LinearWorkflowRunner(
            IReadOnlyList<IWorkflowStep<TState>> steps,
            IWorkflowReentryAdapter<TState> reentryAdapter = null,
            int maxAttempts = 1)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _reentryAdapter = reentryAdapter;
            MaxAttempts = maxAttempts < 1 ? 1 : maxAttempts;
        }

        public async Task<WorkflowRunResult<TState>> RunAsync(
            TState state,
            CancellationToken cancellationToken = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var attempts = 0;

            while (true)
            {
                attempts++;
                TrySetAttemptIndex(state, attempts - 1);

                for (int i = 0; i < _steps.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var step = _steps[i];
                    if (step == null)
                    {
                        return new WorkflowRunResult<TState>
                        {
                            State = state,
                            Attempts = attempts,
                            StopStepId = null,
                            FinalAdvance = WorkflowAdvance.Stop,
                            Summary = $"Workflow step at index {i} is null."
                        };
                    }

                    var result = await step.RunAsync(state, cancellationToken);
                    result ??= WorkflowStepResult.Stop(
                        "NullStepResult",
                        $"Step '{step.StepId}' returned null.");

                    switch (result.Advance)
                    {
                        case WorkflowAdvance.Continue:
                            continue;

                        case WorkflowAdvance.Complete:
                            return new WorkflowRunResult<TState>
                            {
                                State = state,
                                Attempts = attempts,
                                StopStepId = step.StepId,
                                FinalAdvance = WorkflowAdvance.Complete,
                                Summary = result.Summary
                            };

                        case WorkflowAdvance.Stop:
                            return new WorkflowRunResult<TState>
                            {
                                State = state,
                                Attempts = attempts,
                                StopStepId = step.StepId,
                                FinalAdvance = WorkflowAdvance.Stop,
                                Summary = result.Summary
                            };

                        case WorkflowAdvance.RequestReentry:
                            if (_reentryAdapter == null)
                            {
                                return new WorkflowRunResult<TState>
                                {
                                    State = state,
                                    Attempts = attempts,
                                    StopStepId = step.StepId,
                                    FinalAdvance = WorkflowAdvance.Stop,
                                    Summary = "Workflow requested re-entry but no re-entry adapter was provided."
                                };
                            }

                            if (attempts >= MaxAttempts)
                            {
                                return new WorkflowRunResult<TState>
                                {
                                    State = state,
                                    Attempts = attempts,
                                    StopStepId = step.StepId,
                                    FinalAdvance = WorkflowAdvance.Stop,
                                    Summary = string.IsNullOrWhiteSpace(result.Summary)
                                        ? $"Workflow reached MaxAttempts ({MaxAttempts}) before re-entry could continue."
                                        : result.Summary
                                };
                            }

                            if (!_reentryAdapter.TryPrepareNextAttempt(state, out var failure))
                            {
                                return new WorkflowRunResult<TState>
                                {
                                    State = state,
                                    Attempts = attempts,
                                    StopStepId = step.StepId,
                                    FinalAdvance = WorkflowAdvance.Stop,
                                    Summary = string.IsNullOrWhiteSpace(failure)
                                        ? "Workflow re-entry adapter failed to prepare the next attempt."
                                        : failure
                                };
                            }

                            goto NextAttempt;

                        default:
                            return new WorkflowRunResult<TState>
                            {
                                State = state,
                                Attempts = attempts,
                                StopStepId = step.StepId,
                                FinalAdvance = WorkflowAdvance.Stop,
                                Summary = $"Step '{step.StepId}' returned unsupported advance '{result.Advance}'."
                            };
                    }
                }

                return new WorkflowRunResult<TState>
                {
                    State = state,
                    Attempts = attempts,
                    StopStepId = _steps.Count > 0 ? _steps[_steps.Count - 1]?.StepId : null,
                    FinalAdvance = WorkflowAdvance.Complete,
                    Summary = "Workflow completed all steps."
                };

                NextAttempt:
                continue;
            }
        }

        private static void TrySetAttemptIndex(TState state, int attemptIndex)
        {
            if (state == null)
                return;

            var property = typeof(TState).GetProperty("AttemptIndex");
            if (property != null && property.CanWrite && property.PropertyType == typeof(int))
            {
                property.SetValue(state, attemptIndex);
                return;
            }

            var field = typeof(TState).GetField("AttemptIndex");
            if (field != null && field.FieldType == typeof(int))
                field.SetValue(state, attemptIndex);
        }
    }
}