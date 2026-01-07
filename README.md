# BCS LLM Core (Unity) — v0

A small, portable **Unity ↔ LLM interaction layer** with:

- **Provider-agnostic runtime** (`ILLMClient`, `LLMClientBase`, `LLMClientFactory`)
- An **OpenAI provider** (`OpenAILLMClient`) supporting:
  - **Chat Completions** (`/v1/chat/completions`)
  - **Responses** (`/v1/responses`) *(recommended, and required for file attachments)*
- **Optional OpenAI Files support** (Editor-focused):
  - Upload a **PDF** to OpenAI Files → get a `file_id`
  - Attach `file_id` in a **Responses** request so the model can read the PDF
- **Agent configuration** via ScriptableObjects (`LLMAgentData`, `LLMAgentInstructionsData`, `LLMClientData/OpenAIClientData`)
- **Editor tooling** for local setup + testing:
  - `LLMEnvSetupWindow` (writes only `OPENAI_API_KEY` to `.env`)
  - `LLMAgentWizardWindow` (load agent, rebuild client, ping, prompt console, token usage, optional cost estimate, history tools, **PDF upload**)
- A **pricing estimation pipeline** (token usage → USD estimate) via `LLMModelPricingCatalogSO` + `LLMPricingEstimator`

> Note on naming: this package is being migrated toward `BCS.LLM.Core.*`. Some projects/branches may still contain legacy `Eon.Narrative.LLM.*` copies for backwards compatibility.

---

## What you get (v0)

### Runtime

- `ILLMClient` + `LLMClientBase`: shared parameters, system instructions, and a local conversation history store.
- `LLMClientFactory`: builds a concrete runtime client from a `LLMClientData` asset.
- `OpenAILLMClient`: OpenAI provider implementation with two API variants:
  - **Chat Completions** (`/v1/chat/completions`)
  - **Responses** (`/v1/responses`) *(recommended)*
- `ILLMFileClient` + `LLMFileUploadResult`: optional capability interface for **uploading files** (OpenAI only in v0).

### Editor tools

- **Environment setup**
  - Minimal `.env` writing: `OPENAI_API_KEY=...`
  - Optional `LLMEnvSettings` asset (non-secret defaults like base URL + endpoints)
- **Agent Wizard** (`Tools → LLM → Agent Wizard (v0)`)
  - Select `LLMAgentData`, rebuild the runtime client, run a “ping” request, send prompts,
    inspect token usage, optionally estimate cost, and view/clear history.
  - Includes an optional **Files (PDF Upload)** panel (OpenAI only).

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

1. Copy this package into your Unity project under:

```
Packages/com.bcs.llm-core
```

2. Ensure your assembly definitions reference the `Runtime` asmdef.

### Option B — Git URL (recommended for teams)

Add to your `manifest.json`:

```json
{
  "dependencies": {
    "com.bcs.llm-core": "https://your.git.repo/com.bcs.llm-core.git?path=/Packages/com.bcs.llm-core#<tag-or-commit>"
  }
}
```

### Dependencies

- Newtonsoft Json (either via UPM or your own dependency pipeline)

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

Create:

- **Create → LLM → Env Settings**

Place the asset at:

```
Assets/Resources/LLMEnvSettings.asset
```

This contains **non-secret defaults** (base URL and endpoints) and can auto-load on startup.

Default OpenAI endpoint fields include:

- `openAIBaseUrl` (default `https://api.openai.com`)
- `openAIChatEndpoint` (default `/v1/chat/completions`)
- `openAIResponsesEndpoint` (default `/v1/responses`)
- `openAIFilesEndpoint` (default `/v1/files`)

### 3) Create your agent assets

1. **OpenAI client config**
   - **Create → LLM → OpenAI Client Configuration**
   - Choose:
     - Model (enum)
     - API variant (**Responses** vs Chat Completions)
     - Sampling (temperature/top_p) and token limits

2. **Instructions**
   - **Create → BCS → LLM → Agent Instructions**
   - Write your system prompt / rules.
   - (Optional) You can also rely on `LLMClientData.SystemInstructions` and skip this asset.

3. **Agent**
   - **Create → BCS → LLM → Agent Data**
   - Assign:
     - `AgentInstructionsData` (optional but recommended)
     - `LlmClientData` (your OpenAI client config)

> Legacy note: if you still have `Eon.Narrative.LLM.*` assets in your project, the Create menu paths may show `Eon/...` equivalents. Prefer the `BCS/...` versions going forward.

### 4) Open the Agent Wizard and test

Open **Tools → LLM → Agent Wizard (v0)**, then:

1. Assign your `LLMAgentData`
2. Click **Rebuild Client**
3. Click **Ping** (expects `pong`)
4. Send a simple prompt (e.g., `Say "ok".`)
5. Verify:
   - output text displays
   - token usage displays (when provided by the endpoint)
   - history updates as expected

### 5) (Optional) PDF upload + attach in a Responses request (OpenAI only)

This is the minimal smoke test for “model reads a PDF” in Editor:

1. In your **OpenAI client config**, set **API Variant = Responses**.
2. In the Agent Wizard, open **Files (PDF Upload)**.
3. Choose a **PDF** file and click **Upload PDF → file_id**.
4. In the Console panel, enable **Attach last uploaded PDF to request** (if shown).
5. Send a prompt that references the attached file, e.g.:

```
Read the attached PDF and summarize the diagnostic rules it contains.
```

**Notes**
- This workflow is **Editor-only** and intended for tools/import pipelines.
- This package’s file support is **PDF-only** in v0.
- Attaching files requires the **Responses** API.

---

## Environment variables (non-secret + secret)

This package supports config via:
- `.env` (local dev)
- OS environment variables (CI / build agents)

Common keys:

- `OPENAI_API_KEY` *(secret)*
- `OPENAI_BASE_URL` *(non-secret; default from `LLMEnvSettings`)*
- `OPENAI_CHAT_ENDPOINT` *(non-secret; default `/v1/chat/completions`)*
- `OPENAI_RESPONSES_ENDPOINT` *(non-secret; default `/v1/responses`)*
- `OPENAI_FILES_ENDPOINT` *(non-secret; default `/v1/files`)*

Precedence (typical):
1) Explicit env var → 2) `LLMEnvSettings` → 3) hard-coded default

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

- Remove remaining legacy `Eon.Narrative.LLM.*` duplicates (finish namespace migration).
- Add `Samples~/` with:
  - a tiny “Hello LLM” MonoBehaviour
  - a pre-configured Agent + ClientData sample (no secrets)
- Add an Editor “pricing updater” flow (semi-automated monthly refresh)
- Split providers into separate packages (`com.bcs.llm-openai`, etc.) if/when needed
- Decide on (or remove) the deprecated “file-capability” interfaces that are no longer used in code paths.

---

## License

TBD (internal extraction; add your preferred license before publishing publicly).
