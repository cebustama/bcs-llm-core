# BCS LLM Core (Unity)

Provider-agnostic Unity ↔ LLM client layer with ScriptableObject-based agent/config assets, an OpenAI provider, editor setup tooling, optional file upload support for OpenAI Responses, and an optional pricing estimation pipeline.

This file is the **package overview**, not the authoritative home for subsystem semantics.

## Read this first
- `Documentation~/README.md`
- `Documentation~/SSoT_INDEX.md`
- `Documentation~/CURRENT_STATE.md`

## Package scope
- Runtime abstractions for LLM requests
- OpenAI provider implementation
- Agent/config assets
- Editor setup and test tooling
- Optional pricing estimation utilities

## Current package shape
- `BCS.LLM.Core.Runtime` asmdef
- `BCS.LLM.Core.Editor` asmdef
- Unity 2022.3+
- Newtonsoft Json dependency

## Quick start
1. Set `OPENAI_API_KEY` via local `.env` or OS environment.
2. Optionally create `LLMEnvSettings.asset` under `Assets/Resources/`.
3. Create `OpenAIClientData`, `LLMAgentInstructionsData`, and `LLMAgentData` assets.
4. Open **Tools → LLM → Agent Wizard (v0)**.
5. Rebuild the client, ping, and send a prompt.

## Important operational notes
- Secrets belong in env, not assets.
- Base URL and endpoints are non-secret defaults/configuration.
- The runtime contract remains provider-agnostic; file upload and file-attach support are optional capabilities.
- Pricing estimates are approximate and intended for tooling, not billing reconciliation.

## Documentation authority
- Cross-cutting rules: `Documentation~/SSoT_CONTRACTS.md`
- Runtime + OpenAI provider truth: `Documentation~/SSoT_Runtime_and_OpenAI_Provider.md`
- Wizard/editor policy truth: `Documentation~/SSoT_Editor_Tooling_and_Wizard.md`
- Pricing truth: `Documentation~/SSoT_Pricing_Pipeline.md`
- Planning: `Documentation~/planning/Roadmap_LLM_Core.md`

## Notes on naming and maturity labels
- The package identifier is `com.bcs.llm-core`.
- Some docs/UI strings still say `v0` / `v0.1`; package metadata is currently `1.0.0`.
- Some older projects may still use legacy `Eon.Narrative.LLM.*` naming; `BCS.LLM.Core.*` is the preferred terminology going forward.

## Reference docs
- `Documentation~/reference/LLM_Core_EditorWindow_Integration_Guide.md`
- `Documentation~/reference/llm-agent-wizard-test-cases.md`

## Archive
Superseded mixed-state docs and absorbed historical notes live under `Documentation~/archive/`.
