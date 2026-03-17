namespace BCS.LLM.Core.Prompts
{
    public interface IPromptBuilder<in TInput>
    {
        PromptBuildResult Build(TInput input);
    }
}