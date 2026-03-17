namespace BCS.LLM.Core.Orchestration
{
    public sealed class WorkflowStepResult
    {
        public WorkflowAdvance Advance = WorkflowAdvance.Continue;
        public string ReasonCode;
        public string Summary;

        public static WorkflowStepResult ContinueStep(string summary = null)
        {
            return new WorkflowStepResult
            {
                Advance = WorkflowAdvance.Continue,
                Summary = summary
            };
        }

        public static WorkflowStepResult Complete(string summary = null)
        {
            return new WorkflowStepResult
            {
                Advance = WorkflowAdvance.Complete,
                Summary = summary
            };
        }

        public static WorkflowStepResult Stop(string reasonCode, string summary)
        {
            return new WorkflowStepResult
            {
                Advance = WorkflowAdvance.Stop,
                ReasonCode = reasonCode,
                Summary = summary
            };
        }

        public static WorkflowStepResult RequestReentry(string reasonCode = null, string summary = null)
        {
            return new WorkflowStepResult
            {
                Advance = WorkflowAdvance.RequestReentry,
                ReasonCode = reasonCode,
                Summary = summary
            };
        }
    }
}