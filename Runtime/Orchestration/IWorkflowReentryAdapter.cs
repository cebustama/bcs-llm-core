namespace BCS.LLM.Core.Orchestration
{
    public interface IWorkflowReentryAdapter<in TState>
    {
        bool TryPrepareNextAttempt(TState state, out string failure);
    }
}