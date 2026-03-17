namespace BCS.LLM.Core.Validation
{
    public interface IResponseValidator<in T>
    {
        ValidationResult Validate(T value);
    }
}
