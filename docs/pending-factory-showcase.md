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

## Learnings from This Session
- Source→Buffer connection must be explicit (Source.ConnectTo(IItemSink))
- Server needs explicit start trigger (ItemReceived handler on upstream buffer)
- Animation dots must be state-diff driven, not frame-based — otherwise phantom entities
- Step size 0.1 is good for smooth animation
- Layout recompute causes flicker — only on resize
- Physical layout needs auto-scaling (meters→pixels) with margin handling
- NegExponentialDistribution needs ConfigureMean, not constructor param — use Gaussian/Constant instead
