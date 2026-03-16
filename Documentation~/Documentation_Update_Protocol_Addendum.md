# Recommended addition — Documentation update protocol

Add a short block like this to `SSoT_INDEX.md` or `Documentation~/README.md`:

## Documentation update protocol
After every meaningful technical change, update documentation in this order:

1. Update the authoritative SSoT or `SSoT_CONTRACTS.md` for the concept that changed.
2. Update `CURRENT_STATE.md` if the operational reality, priorities, or known deltas changed.
3. Update `changelog-ssot.md` if meaning, contract, authority, or behavioral interpretation changed.
4. Update `coverage-matrix.md` only if the primary home of a concept changed.
5. Update reference/test docs if user-facing workflows or expected behaviors changed.
6. Archive or mark any superseded docs.

A technical change is not considered complete until the required documentation updates are done.

## Why this belongs here
The reusable guideline already defines this process formally, but the package should also keep a short local reminder in its own entry-point docs so the maintenance loop stays visible during day-to-day work.
