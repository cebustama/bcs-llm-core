namespace BCS.LLM.Core.Orchestration
{
    public enum WorkflowAdvance
    {
        Continue = 0,
        Complete = 1,
        Stop = 2,
        RequestReentry = 3
    }
}