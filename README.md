# BCS LLM Core (Unity) — v0

A small, portable **Unity ↔ LLM interaction layer** with:
- **provider-agnostic runtime** (`ILLMClient`, `LLMClientBase`, `LLMClientFactory`)
- an **OpenAI v0 provider** (`OpenAILLMClient`) supporting **Chat Completions** *and* **Responses**
- **agent configuration** via ScriptableObjects (`LLMAgentData`, `LLMAgentInstructionsData`, `LLMClientData/OpenAIClientData`)
- **Editor tooling** for local setup + testing:
  - `LLMEnvSetupWindow` (writes only `OPENAI_API_KEY` to `.env`)
  - `LLMAgentWizardWindow` (load agent, rebuild client, ping, prompt console, token usage, optional cost estimate, history tools)
- a **pricing estimation pipeline** (token usage → USD estimate) via `LLMModelPricingCatalogSO` + `LLMPricingEstimator`

> Note on naming: some namespaces/menu paths still reflect the original extraction (e.g., `Eon.Narrative.LLM.*`). Renaming to `BCS.LLM.*` is a planned cleanup step.

---

## What you get (v0)

### Runtime
- `ILLMClient` + `LLMClientBase`: shared parameters, system instructions, and a local conversation history store.
- `LLMClientFactory`: builds a concrete runtime client from a `LLMClientData` asset.
- `OpenAILLMClient`: OpenAI provider implementation with two API variants:
  - **Chat Completions** (`/v1/chat/completions`)
  - **Responses** (`/v1/responses`) *(recommended)*

### Editor tools
- **Environment setup**
  - Minimal `.env` writing: `OPENAI_API_KEY=...`
  - Optional `LLMEnvSettings` asset (non-secret defaults like base URL + endpoints)
- **Agent Wizard**
  - Select `LLMAgentData`, rebuild the runtime client, run a “ping”, send prompts, inspect token usage, optionally estimate cost, and view/clear history.

### Pricing estimation (optional)
- `LLMCompletionResult` exposes token usage:
  - input tokens
  - cached input tokens (when reported)
  - output tokens
  - reasoning tokens (when reported)
- `LLMPricingEstimator` computes an approximate USD cost from:
  - token usage
  - pricing rates (USD per 1M tokens)
- Rates can live in:
  - per-client fields (`LLMClientData`), or
  - a central catalog asset (`LLMModelPricingCatalogSO`) *(recommended for tools)*

---

## Install

### Option A — Embedded package (local dev)
Copy this repo folder into:

```
<YourUnityProject>/Packages/com.bcs.llm-core/
```

### Option B — Git URL (recommended for teams)
In Unity: **Package Manager → Add package from git URL…** and use your repo URL.

### Dependencies
The OpenAI client uses JSON parsing. Ensure your project includes:

- `com.unity.nuget.newtonsoft-json`

(Usually you add this to `package.json` as a dependency.)

---

## Quick start (Editor workflow)

### 1) Add your OpenAI API key (local only)
Use the **LLM Env Setup** Editor window (search for `LLMEnvSetupWindow` via Unity’s menu search) to write:

```
OPENAI_API_KEY=your_key_here
```

- The tool writes **only** the key to `.env`.
- `.env` should be ignored by git (see `.gitignore`).

### 2) (Optional) Create `LLMEnvSettings.asset`
If your project uses `LLMEnvSettings`, place it at:

```
Assets/Resources/LLMEnvSettings.asset
```

This contains **non-secret defaults** (base URL and endpoints) and can auto-load on startup.

### 3) Create your agent assets
Create assets (menu paths reflect current extraction):

1. **OpenAI client config**
   - **Create → LLM → OpenAI Client Configuration**
   - Choose:
     - model (enum)
     - API variant (Responses vs Chat Completions)
     - sampling (temperature/top_p) and token limits
2. **Instructions**
   - **Create → Eon → LLMAgentInstructions**
   - Write your system prompt / rules.
3. **Agent**
   - **Create → Eon → LLMAgentData**
   - Assign:
     - `AgentInstructionsData`
     - `LlmClientData` (your OpenAI client config)

### 4) Open the Agent Wizard and test
Open **LLMAgentWizardWindow** (via menu search), then:

1. Assign your `LLMAgentData`
2. Click **Rebuild Client**
3. Click **Ping** (expects `pong`)
4. Send a simple prompt (e.g., `Say "ok".`)
5. Verify:
   - output text displays
   - token usage displays (when provided by the endpoint)
   - history updates as expected

---

## Conversation history behavior (important)

- The runtime client **always stores** conversation history after each call.
- The Wizard can toggle whether that history is **included** in the outgoing request (“Use Conversation History”).
- v0 achieves this without complicating the runtime API by using a UI-only snapshot/clear/merge approach.

---

## Pricing estimation setup (optional)

### 1) Create a pricing catalog asset
Create:

- **Create → LLM → Pricing → Model Pricing Catalog**

This asset is safe to commit (non-secret).

### 2) Populate pricing rates
You have two typical approaches:

- **Catalog-first (recommended for tooling):**
  - Add entries per model you use (provider/model/tier + USD/1M rates).
  - Optionally call `OpenAIPricingCatalogExtensions.ApplyOpenAIStandardTextDefaults(...)` to bootstrap common model IDs.

- **Per-client (quick tests):**
  - Fill `InputUSDPerMTokens`, `CachedInputUSDPerMTokens`, `OutputUSDPerMTokens` on your `LLMClientData`.

The Agent Wizard can then display an **estimated cost** per request (approximate; not an invoice).

---

## Manual test cases

See:
- `llm-agent-wizard-test-cases.md`

---

## Security notes

- Do **not** commit `.env` files.
- Avoid storing API keys in ScriptableObjects.
- Prefer OS environment variables for CI / build agents, and local `.env` only for development.

---

## Roadmap (next sensible steps)

- Rename namespaces/menu paths from `Eon.*` → `BCS.LLM.*`
- Add `Samples~/` with:
  - a tiny “Hello LLM” MonoBehaviour
  - a pre-configured Agent + ClientData sample (no secrets)
- Add an Editor “pricing updater” flow (semi-automated monthly refresh)
- Split providers into separate packages (`com.bcs.llm-openai`, etc.) if/when needed

---

## License
TBD (internal extraction; add your preferred license before publishing publicly).
