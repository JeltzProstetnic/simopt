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

## Slice 5: a process's own random streams (2026-11-25, night)

```csharp
IEnumerable<Wait> Machine(Process p)
{
    while (true)
    {
        yield return p.Timeout(p.Random("run", Exponential(1d / 480)).Next());
        yield return p.Timeout(p.Random("repair", Exponential(1d / 30)).Next());
    }
}
```

- `p.Random(name, distribution)` makes the stream on the first call and returns it afterwards, so the
  body may ask on every run; `p.Random(name)` returns one made earlier and names the missing stream
  otherwise.
- **Seeded by name, not by order.** A stream's seed is `GetRandomSeedFor(StableHash.Of("process-random:"
  + name))` on the process's seed, applied when it is made and again on every reset. Adding a stream, or
  making streams in another order, leaves the others' numbers alone; the same name in two processes
  gives two streams (each process has its own seed, by its id since SIM-125). Seeding by registration
  order, as the model's streams are (SIM-131), would have tied a stream to every stream made before it,
  and a body that makes its streams while it runs would have drawn other numbers after a reset than a
  rebuilt model does.
- A stream holds two generators' state (about 5 kB), so a process started per customer should draw from
  a model stream or a long-lived process's stream instead.
- Tests `ProcessRandomStreamTests` (4); without the reseed in `Reset` the rebuilt-against-reset test fails.

## Slice 6: the example (2026-11-25, night)

`examples/SimOpt.Examples.CarWash` is SimPy's classic car wash, extended to use every process feature:
a pool of bays (`Resource`), drivers who give up when their patience runs out (`AnyOf`, which withdraws
the lost request), a soap tank each wash draws from (`Stock`), a supplier who reacts when the tank runs
low (`Wait.Next` on its `LevelChangedEvent` with a filter), named streams in the arrival and supplier
processes, and 20 replicated days through `ReplicationRunner` with `TallyCollector`s and the stock and
resource collectors. `dotnet run --project examples/SimOpt.Examples.CarWash` prints:

| 20 days of 600 minutes | 2 bays | 3 bays |
|---|---:|---:|
| drivers who gave up | 0.170 ± 0.017 | 0.114 ± 0.015 |
| wait for a bay (minutes) | 3.24 ± 0.19 | 1.25 ± 0.21 |
| bays busy | 0.836 ± 0.018 | 0.650 ± 0.019 |
| soap tank empty | 0.186 ± 0.014 | 0.205 ± 0.012 |

A third bay cuts the wait, but one driver in nine still gives up: the tank is empty a fifth of the day,
and a car waiting for soap keeps its bay. The supply is the bottleneck, which is the kind of answer the
optimiser is for. `CarWashExampleTests` (3) runs the same file: reproducible, every figure in range, and
a third bay lowers both the share who give up and the wait.

**Next:** the SIM-65 schema (SIM-126's last part), which waits for SIM-65 step 1.
