using System.Threading;
using System.Threading.Tasks;

namespace BCS.LLM.Core.Orchestration
{
    public interface IWorkflowStep<TState>
    {
        string StepId { get; }

        Task<WorkflowStepResult> RunAsync(
            TState state,
            CancellationToken cancellationToken = default);
    }
}