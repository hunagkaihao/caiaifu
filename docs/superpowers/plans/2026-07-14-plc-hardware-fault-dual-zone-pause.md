# PLC Hardware Fault Dual-Zone Pause Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pause both RCS zones for an executing transport task when PLC status bit 0, 3, or 4 is not set.

**Architecture:** The TCP worker parses a small immutable hardware-status value and uses a per-connection gate to suppress repeated successful alarms. A Domain interface carries the alarm across the layer boundary; its Application implementation finds executing tasks, resolves both device ports through the existing work-position mapping, and calls the existing RCS zone control API twice.

**Tech Stack:** .NET 6, ABP, EF Core repositories, xUnit, NSubstitute.

## Global Constraints

- Treat bit 0, bit 3, and bit 4 of the first response byte as healthy only when all three are `1`.
- Do not use or modify `RunState` for hardware-fault decisions.
- Do not change transport-task status, cancellation state, or automatically resume a zone.
- Attempt both source and target zone pauses even when one call fails.
- Retry on later abnormal frames after a failed handling attempt; suppress repeats after complete success until the PLC becomes healthy again.

---

### Task 1: Hardware status and alarm gate

**Files:**
- Create: `Abp_Ecs/Ecs.Domain/AgvPlcTcp/AgvPlcHardwareStatus.cs`
- Create: `Abp_Ecs/Ecs.Domain/AgvPlcTcp/AgvPlcHardwareFaultGate.cs`
- Create: `Abp_Ecs/Ecs.Application.Tests/AgvPlc/AgvPlcHardwareStatusTests.cs`

**Interfaces:**
- Produces: `AgvPlcHardwareStatus.FromStatusByte(byte)` and `AgvPlcHardwareFaultGate.ShouldHandle/MarkHandled`.

- [x] Write tests for `0x19`, each required bit being zero, successful suppression, failed retry, and healthy reset.
- [x] Run the focused tests and confirm they fail because the types do not exist.
- [x] Implement the two Domain types with no dependency on `RunState`.
- [x] Run the focused tests and confirm they pass.

### Task 2: Application fault handler

**Files:**
- Create: `Abp_Ecs/Ecs.Domain/AgvPlcTcp/IAgvPlcHardwareFaultHandler.cs`
- Create: `Abp_Ecs/Ecs.Application/AgvPlc/AgvPlcHardwareFaultHandler.cs`
- Modify: `Abp_Ecs/Ecs.Application/EcsApplicationModule.cs`
- Create: `Abp_Ecs/Ecs.Application.Tests/AgvPlc/AgvPlcHardwareFaultHandlerTests.cs`

**Interfaces:**
- Consumes: `IAgvTaskZoneResolver.ResolveAsync` and `IRcsApiClient.ControlZonePauseAsync`.
- Produces: `Task<bool> HandleAsync(string pointCode, AgvPlcHardwareStatus status, CancellationToken cancellationToken)`.

- [x] Write tests proving both zones receive `FREEZE`, terminal tasks are ignored, and target is still attempted after a source failure.
- [x] Run the focused tests and confirm they fail because the handler does not exist.
- [x] Implement active-task filtering, zone resolution, independent pause attempts, result aggregation, and logs.
- [x] Register the handler in `EcsApplicationModule`.
- [x] Run the focused tests and confirm they pass.

### Task 3: TCP receive integration

**Files:**
- Modify: `Abp_Ecs/Ecs.Domain/AgvPlcTcp/AgvPlcTcpConnectionWorker.cs`

**Interfaces:**
- Consumes: `AgvPlcHardwareStatus`, `AgvPlcHardwareFaultGate`, and scoped `IAgvPlcHardwareFaultHandler`.

- [x] Extract complete frames into a local list so each frame can be processed asynchronously without blocking inside the synchronous frame-extractor callback.
- [x] Merge the protocol snapshot exactly as before, then invoke the fault handler only when the gate requests handling.
- [x] Mark the alarm handled only when every applicable dual-zone pause succeeds; keep retry enabled after failure.
- [x] Run all Application tests and build the solution.

### Task 4: Graph and branch handoff

**Files:**
- Update: `graphify-out/*` through `graphify --update` without manually editing generated files.

- [x] Run focused tests, full Application tests, and solution build with fresh output.
- [x] Inspect the diff and stage only source, tests, plan, and required existing dependencies; exclude generated build/runtime artifacts.
- [x] Commit on `codex/plc-hardware-fault-dual-zone-pause`.
