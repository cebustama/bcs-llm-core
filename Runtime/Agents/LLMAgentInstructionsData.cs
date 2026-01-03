using UnityEngine;

namespace Eon.Narrative.LLM.Agents
{
    [CreateAssetMenu(fileName = "NewLLMAgentInstructions", menuName = "Eon/LLMAgentInstructions")]
    public class LLMAgentInstructionsData : ScriptableObject
    {
        [TextArea(10, 80)] public string InstructionsText = "You are a helpful AI agent.";
    }
}
