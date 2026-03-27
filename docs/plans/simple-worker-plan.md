# SIM-34: SimpleWorker Entity — Design Plan

## Architecture

`SimpleWorker : SimpleEntity` — mobile worker that walks between stations at constant speed.

**Not** MobileEntity (too complex — accel/decel physics). Walking = scheduled teleport event.

## State
- `WalkingSpeed` (m/s), `HomePosition`, `RepairTime` (seconds)
- `Queue<RepairTask>` — FIFO task queue
- `IsWalking`, `IsRepairing`, `IsIdle` flags
- `CurrentTarget` — server being walked to / repaired

## Core Methods
1. `DispatchRepair(SimpleServer server)` — public API. Enqueues task. If idle, starts.
2. `ProcessNextTask()` — dequeue, compute walk time = distance/speed, schedule arrival
3. `OnArrival()` — update position, call `server.ScheduleRecovery(repairDelay)`, schedule OnRepairFinished
4. `OnRepairFinished()` — fire event, process next task or return home

## Events
- `RepairStartedEvent` — `BinaryEvent<SimpleWorker, SimpleServer>`
- `RepairFinishedEvent` — `BinaryEvent<SimpleWorker, SimpleServer>`
- `ArrivalEvent` — `BinaryEvent<SimpleWorker, Point>`

## Time Conversion
Must use `TimeSpan.FromSeconds(seconds).ToDouble()` — engine uses ticks internally.

## Repair via Server API
Uses existing `server.ScheduleRecovery(double delay)` — no Server modifications needed.
Server needs `AutoRecover = false` so worker controls recovery.

## v1 Scope
- Walk to damaged server, repair it
- FIFO task queue for multiple dispatches
- User calls `DispatchRepair()` explicitly (no auto-detection)
- Return to home position when idle

## v2 (future)
- Add public `FailedEvent` to Server for auto-detection
- Multiple task types (unstick conveyor, pull item from buffer)
- Pathfinding around obstacles
- Visual rendering (walking sprite on floor)

## Tests Needed
1. `DispatchRepair_WalksToServerAndRepairs` — full integration
2. `WalkTime_IsDistanceOverSpeed` — timing verification
3. `Constructor_DefaultState_IsIdle`
4. `Reset_ClearsTaskQueueAndState`
