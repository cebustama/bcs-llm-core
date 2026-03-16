# BCS / Eon LLM Core — Documentation

This folder contains the governed documentation system for the package.

## Reading order
1. `SSoT_INDEX.md`
2. `CURRENT_STATE.md`
3. `planning/Roadmap_LLM_Core.md`
4. Relevant subsystem SSoT:
   - `SSoT_Runtime_and_OpenAI_Provider.md`
   - `SSoT_Editor_Tooling_and_Wizard.md`
   - `SSoT_Pricing_Pipeline.md`

## What belongs here
- authoritative subsystem truth,
- cross-cutting contracts,
- current operational state,
- active roadmap,
- reference docs,
- archived superseded docs.

## What does not belong here
- secrets,
- temporary scratch notes,
- mixed state+plan docs with no authority boundary,
- raw tree snapshots as if they were documentation.

## Conflict rule
- If a reference doc conflicts with an SSoT, the SSoT wins.
- If planning conflicts with implemented truth, the SSoT wins and the roadmap must be corrected.
- If an archived doc conflicts with anything current, the archived doc loses.

## Maintenance rule
A technical change is not complete until the relevant SSoT, `CURRENT_STATE.md`,
`coverage-matrix.md`, and `changelog-ssot.md` are updated as required.
