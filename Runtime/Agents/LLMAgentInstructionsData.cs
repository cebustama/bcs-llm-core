using UnityEngine;

namespace Eon.Narrative.LLM.Agents
{
    [CreateAssetMenu(
        fileName = "NewLLMAgentInstructions", 
        menuName = "BCS/LLM/Agent Instructions")]
    public class LLMAgentInstructionsData : ScriptableObject
    {
        [TextArea(10, 80)] public string InstructionsText = "You are a helpful AI agent.";
    }
}
