# PLC Fault Manual Zone Recovery Design

**Goal:** Monitor the PLC health of the current transport task, pause both task zones on a hardware fault, and let an operator manually restore those zones only after the source and target PLC states are stable for three consecutive frames.

## Scope

- Continue using the first PLC response byte as the hardware-health source.
- A point is healthy only when bit 0 (device normal), bit 3 (communication normal), and bit 4 (not emergency stopped) are all `1`.
- A fault at either endpoint pauses both source and target RCS zones for the current executing task.
- PLC recovery is performed manually at the PLC/HMI.
- ECS never automatically sends `RUN` when a PLC becomes healthy.
- An operator restores the task zones from the task list after both endpoints pass the stability gate.
- Existing task submission, PLC Seq9/Seq10 matching, RCS callbacks, and cancellation behavior remain unchanged except for the conflict guards defined below.

## Health Tracking

Add a singleton in-memory health registry keyed by canonical PLC point code.

- Every complete PLC status frame updates the point's latest health state.
- A healthy frame increments the consecutive healthy-frame count.
- Any unhealthy frame or TCP disconnect resets the count to zero.
- Counts saturate at the required threshold of three.
- Reading and updating the registry is constant-time and does not query the database.
- A process restart resets all counts, which is fail-safe because another three healthy frames are required before manual recovery.

The health registry is observational only. It does not modify `RunState`, Seq9/Seq10, task status, or task dispatch decisions.

## Fault Pause Workflow

When a PLC endpoint reports a fault:

1. Find the newest executing transport task whose source or target point is the faulting point. Existing `RunState` ownership should make this unique; multiple matches are logged as an invariant violation.
2. Serialize zone operations for the task.
3. Re-read the task and stop if it has entered `Cancelling`, `CancelRecoveryRequired`, `Cancelled`, `Completed`, or `RcsFailed`.
4. Resolve `SourcePointCode` and `TargetPointCode` through `WorkPositions.DeviceName -> SiteName`.
5. Store both zone codes in the existing `SourceZoneCode` and `TargetZoneCode` fields.
6. Send `FREEZE` with `mapCode=AA` for each zone that is not already marked paused.
7. Persist `SourceZonePaused` and `TargetZonePaused` independently after each successful call.
8. Keep the existing transport-task status unchanged and do not cancel the RCS task.

If one zone operation fails, the other is still attempted. Later abnormal frames retry only the missing zone operation.

## Manual Fault Recovery

Add a dedicated task operation for PLC-fault recovery. It must not share the cancellation recovery endpoint.

The backend permits manual fault recovery only when:

- the task has at least one paused-zone flag;
- the task is not `Cancelling`, `CancelRecoveryRequired`, or `Cancelled`;
- source and target PLC connections are present;
- source and target PLC points each have three consecutive healthy frames.

The operation re-reads the task under the same task lock, then sends `RUN` only for zones still owned by this task. Successful calls clear the corresponding paused flag. The task status is not changed. If the task has already reached `Completed`, clearing the final paused flag also releases its source and target `RunState`; otherwise the original task continues under its existing RCS state.

The frontend shows a distinct `恢复故障区域` button for a non-cancelled task with paused-zone flags. The backend remains authoritative and rejects a click made before the three-frame gate is satisfied. The existing cancelled-task recovery button and wording remain separate.

## Cancellation Priority And Conflict Rules

Cancellation has higher priority than hardware-fault recovery.

- A fault-paused task that enters cancellation keeps its existing paused-zone flags. Cancellation sends `FREEZE` only for a missing zone.
- Once the task is `Cancelling`, `CancelRecoveryRequired`, or `Cancelled`, fault recovery cannot send `RUN`.
- Cancellation recovery still requires source Seq9 and target Seq10 to be zero.
- Cancellation recovery additionally requires both endpoint PLC states to have three consecutive healthy frames.
- A PLC fault received after cancellation starts does not create a second pause workflow.
- Task cancellation, hardware pause, cancellation recovery, and fault recovery are serialized per task.

Zone `RUN` operations also check whether another task still owns a paused flag for the same `zoneCode`. ECS sends `RUN` only when the current operation releases the final owner. This prevents one task from reopening a zone still required by another task.

## RCS Callback Guards

- RCS status callbacks must not overwrite `Cancelling`, `CancelRecoveryRequired`, or `Cancelled`.
- `quend` and `fanend` must not release point `RunState` while a task is cancelling, cancelled, or still owns a paused zone.
- When fault recovery clears the final pause for an already completed task, it performs the deferred `RunState` release.

These guards prevent late callbacks from reopening cancelled or fault-paused points.

## Isolation From Normal Task Flow

- Healthy PLC frames perform only an in-memory health-counter update.
- No RCS zone call or database query occurs for an ordinary healthy frame.
- Seq9/Seq10 parsing and edge matching remain unchanged.
- A fault with no related executing task records the health state and logs the condition but does not pause a zone.
- Only the current task's source and target zone codes are controlled.
- Tasks that do not use those points continue through the existing dispatch path.

RCS `FREEZE` operates at zone scope. If two configured work positions share the same `SiteName`, both are physically affected by that RCS zone; the final-owner check prevents an early `RUN`, but configuration must still keep port zone codes unique where operational isolation is required.

## Error Handling And Idempotency

- Persist each successful `FREEZE` or `RUN` independently.
- Retrying pause or recovery skips already completed zone operations.
- A partial recovery leaves the failed zone marked paused and keeps the recovery action available.
- Repeated button clicks are serialized and become no-ops after both flags are clear.
- RCS failures preserve task status and return the failing zone code to the operator.
- No new database columns or migrations are introduced.

## Verification

Automated tests must cover:

- health requires bits 0, 3, and 4;
- three consecutive healthy frames are required;
- any fault or disconnect resets the counter;
- a fault on either endpoint pauses both task zones;
- pause success persists zone codes and independent paused flags;
- normal frames do not invoke RCS zone control;
- fault recovery is rejected at zero, one, or two healthy frames;
- manual recovery sends `RUN` for both zones at three healthy frames;
- an unhealthy opposite endpoint blocks recovery;
- cancellation takes priority over fault recovery;
- a fault-paused task can transition into cancellation without duplicate pause calls;
- cancellation recovery still requires Seq9/Seq10 and stable health;
- another task's pause ownership prevents an early zone `RUN`;
- late RCS callbacks do not overwrite cancellation status or release locked points;
- normal Seq9/Seq10 task dispatch behavior remains unchanged.

Final verification includes all Application tests, a full solution build, frontend lint, and frontend production build.

## Non-Goals

- No automatic RCS zone recovery.
- No PLC protocol or frame-layout change.
- No automatic PLC reset command.
- No change to the RCS transport task when a hardware fault occurs.
- No new database schema.
