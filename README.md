# BCS LLM Core (Unity) — Phase 0

A small, portable **Unity ↔ LLM interaction layer** extracted from the Eon project.  
This package focuses on **providers/clients + agent configuration + prompt→response flow** and is intentionally **not** coupled to narrative systems or gameplay code.

> Status: **Phase 0** = *package skeleton + documentation* (safe to import, minimal/no runtime code required yet).

---

## Goals

- Provide a **provider-agnostic client API** to call LLMs from Unity.
- Keep configuration in Unity-friendly **ScriptableObjects** (clients + agents + instructions).
- Make it easy to:
  - swap providers (OpenAI / Gemini / Azure…)
  - define “agents” as data assets
  - send prompts and get structured results (text + token counts)
- Be packagable as a standalone **UPM (Unity Package Manager)** package.

## Non-goals (for this core)

- No narrative, quest, world-state, or Eon-specific gameplay integration.
- No editor tooling in Phase 0 (agent creation/testing UI comes later).
- No “agent runtime orchestration” yet (Phase 0/1 focuses on the core primitives).

---

## What’s in the design (core concepts)

The architecture is structured as three layers:

1) **Agent configuration (ScriptableObjects)**  
   - `LLMAgentData`: bundles an agent identity + instructions + a client config.
   - `LLMAgentInstructionsData`: stores the agent’s system prompt / behavior rules.

2) **Client configuration (ScriptableObjects)**  
   - `LLMClientData`: provider-agnostic configuration (model params, stop sequences, pricing, etc.).
   - Provider-specific subclasses (e.g., `OpenAIClientData`) resolve model ids + secrets.

3) **Client runtime (C# classes)**  
   - `ILLMClient`: common contract for making requests.
   - Provider clients (e.g., `OpenAILLMClient`) implement the actual HTTP call and parsing.
   - `LLMClientFactory`: creates a concrete client from a `LLMClientData` asset.
   - `EnvLoader`: optional helper to load secrets/endpoints from a `.env` or OS env vars.

---

## Phase 0: Package skeleton checklist

Phase 0 is about getting Unity to accept the package and showing clear intent.

### 0.1 Folder structure (embedded package)

Place this package inside your Unity project:

```
<YourProject>/
  Packages/
    com.bcs.llm-core/
      package.json
      README.md
      Runtime/        (Phase 1+)
      Editor/         (later)
      Samples~/       (later)
```

### 0.2 `package.json` (minimum valid)

> Important: `"name"` must be **lowercase** (UPM requirement).  
> `displayName` can use any casing.

Example:

```json
{
  "name": "com.bcs.llm-core",
  "version": "0.0.1",
  "displayName": "BCS LLM Core",
  "unity": "6000.0",
  "description": "Unity ↔ LLM interaction core (clients, providers, agent config)."
}
```

If you later include the OpenAI runtime client, you will also add:

```json
"dependencies": {
  "com.unity.nuget.newtonsoft-json": "3.2.1"
}
```

---

## Phase roadmap

### Phase 1 — Compile-safe runtime assembly
- Add `Runtime/` folder and an `.asmdef`:
  - `BCS.LLM.Core.Runtime.asmdef`
- Move the minimal core interfaces / data assets into `Runtime/`:
  - `ILLMClient`, `LLMCompletionResult`, `ChatMessage`
  - `LLMClientData`, `LLMAgentData`, `LLMAgentInstructionsData`

### Phase 2 — Provider: OpenAI (first supported provider)
- Add:
  - `OpenAIClientData`
  - `OpenAILLMClient`
- Add the `Newtonsoft.Json` package dependency.
- Verify a simple runtime call works (prompt → response).

### Phase 3 — Factory + provider modularization
- Decide:
  - **OpenAI-only factory** in core (simplest), or
  - split providers into separate packages:
    - `com.bcs.llm-openai`
    - `com.bcs.llm-gemini`
    - `com.bcs.llm-azure`

### Phase 4 — Env configuration cleanup (package-safe)
- Remove project-specific defaults and Eon naming.
- Provide either:
  - `LLMEnvSettings` ScriptableObject (Resources-loadable), or
  - OS env vars + `LLM_ENV_PATH` file override.

### Phase 5 — EditorWindow “Agent Wizard”
- Create/edit agent assets.
- Test connection (ping) and prompt/response in-editor.
- Optional: templated instruction presets and output validators.

---

## Security & configuration (planned)

This core is designed so you **do not store API keys inside ScriptableObjects**.  
Instead:
- Use a `.env` file (local dev) and `.gitignore` it
- or OS environment variables (CI / servers)

Once Phase 4 is reached, the package will standardize:
- required keys:
  - `OPENAI_API_KEY`
  - `OPENAI_BASE_URL`
  - `OPENAI_CHAT_ENDPOINT`

---

## Known implementation notes (from current Eon extraction)

When you move code into the package (Phase 1+), keep an eye on these:

- `LLMClientFactory` may reference providers you haven’t included yet (Gemini/Azure).  
  You’ll want to either guard those cases with `#if` defines or move provider factories into provider-specific packages.

- Some “conversation history” methods exist in the interface, but a provider implementation may choose not to send history in the request (stateless-by-default).  
  This is a deliberate design choice: history can be treated as local logging until you explicitly wire it into request payloads.

---

## License / ownership

Internal BCS project extraction (adapt as needed for your repo’s licensing rules).

---

## Reference

The original design document for this package is based on the extracted Eon LLM core pipeline summary:

- `unity-llm-pipeline-core.md`
