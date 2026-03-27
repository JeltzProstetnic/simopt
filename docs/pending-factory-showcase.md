Action: act

# Factory Floor Showcase — Multi-Session Handoff

Tracked-by: SIM-26, SIM-27, SIM-28

## What's Done (Phase A — this session)
- VizTopology extended with physical coordinates, labels, colors
- AutoLayout supports physical-to-pixel conversion
- Factory preset: 13-node electronics assembly (2 docks → inspect → 3 assembly lines → QC → pack → ship)
- Floor grid, multi-line labels, status dots, custom node colors
- Plan document: `docs/plans/factory-floor-showcase.md`

## What's Next

### Phase B — Professional Rendering (next session)
- [ ] Gradient fills instead of flat colors (industrial/SCADA look)
- [ ] Drop shadows on nodes
- [ ] Conveyor belt rendering: animated stripe pattern between nodes
- [ ] Glow effect on active/busy machines
- [ ] Legend panel (node types, colors, status indicators)
- [ ] Scale bar showing physical dimensions
- [ ] Professional dark industrial color palette
- [ ] Entity items as small colored shapes (not just dots) moving along connections

### Phase C — Detachable Controls + Multi-Monitor
- [ ] Separate controls window (Start/Stop/Speed) detachable from canvas
- [ ] Canvas-only fullscreen mode for projection/second monitor
- [ ] Keyboard shortcuts (Space=pause, +/-=speed, F=fullscreen)

### Phase D — Optimization Showcase
- [ ] Built-in optimization mode: vary buffer sizes / service times → find optimal config
- [ ] Before/after comparison view (split screen: baseline vs optimized)
- [ ] Fitness progression chart during optimization
- [ ] Parameter sensitivity heatmap

### Phase E — Ivoclar Dental Manufacturing Showcase
- [ ] Map Ivoclar production process to SimOpt topology
- [ ] Dental-specific terminology and visuals
- [ ] Real production parameters (if available)
- [ ] Showcase for internal presentation

### Phase F — Icon/Graphics Library
- [ ] Factory: machine shapes, conveyor belts, forklifts, storage racks
- [ ] Healthcare: beds, waiting rooms, triage stations
- [ ] Logistics: docks, trucks, sorting stations
- [ ] Generic: custom SVG icon loading for domain-specific visuals

## Learnings

### Phase A
- Source→Buffer connection must be explicit (Source.ConnectTo(IItemSink))
- Server needs explicit start trigger (ItemReceived handler on upstream buffer)
- Animation dots must be state-diff driven, not frame-based — otherwise phantom entities
- Step size 0.1 is good for smooth animation
- Layout recompute causes flicker — only on resize
- Physical layout needs auto-scaling (meters→pixels) with margin handling
- NegExponentialDistribution needs ConfigureMean, not constructor param — use Gaussian/Constant instead

### Phase B — Connection Wiring (critical learnings)
- **Buffer does NOT implement IItemSink** — casting Buffer to IItemSink causes InvalidCastException.
  Buffer has Put() but doesn't declare the interface. For Server→Buffer, use `buffer.ConnectTo(server)`
  which hooks Server's EntityCreatedEvent (a wrapper for entityFinishedSimpleEvent) to buffer.Put().
- **Server→Server requires PushAllowed=true** on the downstream server. Default is false; Put() throws
  without it. Also set AutoContinue=true so processing starts automatically on Put.
- **Server.EntityCreatedEvent == entityFinishedSimpleEvent** — they're the same event (line 319 of Server.cs).
  This means ConnectTo(IItemSource) patterns all hook the "entity finished processing" event, not "entity created".
- **Server default createProduct generates `new TProduct()` with NULL Identifier** — this crashes
  Buffer.Put() which uses Identifier as a dictionary key. ALWAYS provide `createProduct: m => m[0]`
  for pass-through servers in multi-stage pipelines. Only omit for single-stage Source→Buffer→Server→Sink.
- **Connection pattern order in Connect()**: Source→Sink (most general) first, then specific type pairs.
  If order is wrong, a Server→Buffer connection might match Source→IItemSink instead.
- **Five wiring patterns needed for complex topologies:**
  1. Source → IItemSink (source pushes via EntityCreated)
  2. Buffer → Server (server pulls from buffer; ItemReceived starts idle server)
  3. Server → Buffer (buffer subscribes to server's finish event via ConnectTo(IItemSource))
  4. Server → Server (direct push: PushAllowed + EntityCreatedEvent → Put)
  5. Server → Sink (sink subscribes to server's finish event)
- **Test headless first, visualize second.** Always validate model runs to completion with
  a headless test (xUnit) before presenting GUI to user. User is not a tester.
