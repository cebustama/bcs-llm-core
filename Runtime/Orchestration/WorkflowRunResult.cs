namespace BCS.LLM.Core.Orchestration
{
    public sealed class WorkflowRunResult<TState>
    {
        public TState State;
        public int Attempts;
        public string StopStepId;
        public WorkflowAdvance FinalAdvance;
        public string Summary;
    }
}