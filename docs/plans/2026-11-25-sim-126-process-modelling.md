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

## Slice 4: waiting on a template's event (2026-11-25, night)

```csharp
IEnumerable<Wait> Inspector(Process p)
{
    while (true)
    {
        var arrival = Wait.Next(buffer.ItemReceivedEvent);          // the next item the buffer receives
        yield return arrival;
        if (arrival.Args.Identifier.StartsWith("rush")) buffer.Get(arrival.Args.Identifier);
    }
}
```

- `Wait.Next(evnt, when)` for unary, binary and ternary events (every event the templates expose is one
  of these). The optional filter skips raises the process does not want. The wait holds what the
  raise carried: `Value`, or `Sender` and `Args`, plus `Data` for a ternary event.
- The handler records the raise, removes itself and resumes the process through a zero-delay event,
  as the other waits do, so the process continues in the same instant after the event and its other
  handlers, never inside a template's half-finished operation.
- A lost any-of or an interrupt removes the handler (`Cancel`). A reset removes it too, through a new
  `Wait.Abandon` that `Process` calls on its current wait: unlike `Cancel` it undoes nothing, because a
  stock or a resource resets its own state. Without it, a process waiting at the end of a replication
  left a handler that added an event to the next one (8 against 9 events in the test).
- Tests `TemplateEventWaitTests` (7).

**Next slices:** a random-draw helper per process, a process example in `examples/`, then the SIM-65
schema.
