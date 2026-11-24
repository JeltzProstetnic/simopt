# SIM-126: process-style modelling on SimOpt's event engine

2026-11-25, WSL. Owner ruling: top priority (`docs/decisions.md`, 2026-11-25). Builds on SIM-128
(events at the current instant) and SIM-127 (`Stock`).

## Goal

Write a behaviour as one method that reads top to bottom, the way SimPy models are written, instead of
a custom `StochasticEntity` with hand-wired events. Keep what SimPy lacks: seed IDs, the SIM-63
statistics, determinism and the replication workflow (`Reset` and rerun).

```csharp
IEnumerable<Wait> Filler(Process p)
{
    while (true)
    {
        yield return bulk.TakeWait(40);             // waits for 40 kg, takes all at once
        yield return p.Timeout(fillTime.Next());     // fill
        yield return shelf.PutWait(200);             // waits while the shelf is full
    }
}
model.StartProcess(Filler, id: "filler");
```

## Iterators or `async`

| | C# iterators (`IEnumerable<Wait>`) | `async` methods with a custom awaitable |
|---|---|---|
| Closest to SimPy | yes: a generator that yields events | close in syntax, different machinery |
| Determinism | single-threaded by construction | depends on every continuation running inline; one `Task.Delay` or `ConfigureAwait` slip leaves the simulation thread |
| Cost | one state machine per process, one object per wait | a state machine and builder per async call; less with a custom `ValueTask` builder |
| Sub-behaviours | yield a child process and wait for it to finish | `await Sub()` |
| Interrupts | a flag on the wait the process checks after resuming | an exception thrown at the `await` |
| Return values | through the wait object | natural |
| try/catch | no `yield` inside `catch` | full |

**Recommendation: iterators for v1.** They map one to one to SimPy's generators, cannot leave the
simulation thread, and are cheap, which matters for SIM-129. `async` stays possible later as a second
front end over the same `Wait` objects. That is decision **Q-ASYNC** in the handover.

## v1 design (this slice)

- `Process` is a `StochasticEntity`, so a process with an id gets its seed ID by default (SIM-125) and
  can draw from its own streams. It drives its iterator: it moves to the next `Wait`, subscribes, and
  is resumed at the instant the wait completes.
- **Starting.** `model.StartProcess(body, id, name)` schedules the first step as a zero-delay event, so a
  process started inside a handler runs after that handler.
- **Waits in v1.** `p.Timeout(delay)`, `stock.TakeWait(amount)`, `stock.PutWait(amount)` and
  `otherProcess` (a process converts to a wait that completes when it finishes, so yielding it joins).
- **Replications.** A process started while the model is stopped belongs to the model's structure. On
  `Reset` it starts again from the beginning at the start time, through a deferred zero-delay start
  (the SIM-91 idiom). A process started during a run is transient and is dropped by `Reset` (SIM-124).
  A generation counter makes any stale resume a no-op.
- **Not in v1:** any-of and all-of, interrupts, resource requests, waiting on a template's event, and
  wiring into the SIM-65 topology schema. They are the next slices.
