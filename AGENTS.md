# HospitalityContinuedMP agent instructions

This repository is a RimWorld mod fork focused on Multiplayer compatibility.

## Goals
- Preserve existing Hospitality gameplay behavior unless a change is required for MP safety.
- Prefer minimal, targeted fixes over broad refactors.
- Treat determinism and replicated player actions as the priority.

## Multiplayer rules
- Prefer explicit synced wrapper methods for player-triggered state changes.
- Use watched fields only where UI draw code mutates known state during the draw cycle.
- Avoid wall-clock time, random behavior outside MP-safe paths, and unsynced static mutable state.
- When changing UI code, search for all callers that can mutate the same data.
- When proposing a fix, explain the mutation surface, why it was unsafe, and why the patch is MP-safe.

## Repo workflow
- Do not rename files or move defs unless necessary.
- Do not change XML balance/content unless explicitly asked.
- Summarize all changed files and exact sync surfaces affected.
- If unsure whether a change is safe, prefer producing an audit instead of code.