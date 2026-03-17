namespace BCS.LLM.Core.Prompts
{
    public interface IContractHintProvider<in TInput>
    {
        PromptContractHint BuildContractHint(TInput input);
    }
}
