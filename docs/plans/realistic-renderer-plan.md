# SIM-32: Realistic 2D Factory Floor Renderer — Plan

## Architecture

Strategy pattern. Extract `INodeRenderer` interface from `SimulationCanvas`, keep canvas as orchestrator.

```
Controls/Rendering/
├── INodeRenderer.cs         — interface (~30 lines)
├── SchematicRenderer.cs     — extracted from current SimulationCanvas (~350 lines)
├── RealisticRenderer.cs     — new (~600-800 lines)
└── RenderPalette.cs         — shared colors, brushes, typefaces (~80 lines)
```

Canvas keeps: tick/timer, entity animation, layout. Renderers handle all drawing.

## Toggle

- `RenderMode` enum (Schematic, Realistic) on canvas
- `R` key shortcut, button in control bar
- Swap pre-allocated renderer instance, call `InvalidateVisual()` — no reload

## Rendering Techniques

### Floor
- Programmatic concrete tile texture (seeded random lightness per tile, expansion joints)
- Vignette: 4 edge gradient rectangles
- Yellow safety markings around machine footprints

### Server (Machine)
- Metallic 3-stop gradient casing with bevel effect
- Inset work surface, edge shadow (cheap AO)
- Control panel sub-rectangle with LED dots (idle=green, busy=amber pulse, damaged=red)
- Busy animation: oscillating arcs/sparks

### Buffer (Storage Rack)
- Grid of small slots (rows × cols from capacity)
- Filled slots colored, empty slots dark — natural fill indicator
- Steel-gray rack with side rails

### Source (Loading Dock)
- Rectangular bay, one open end facing downstream
- Interior truck-bed with line hatching
- Flashing green indicator on entity generation

### Sink (Shipping Dock)
- Mirror of source dock
- Stacked box shapes showing count
- "Manifest board" with count text

### Conveyor Belt
- Side rails (2px metallic lines, 8px apart)
- Belt surface (dark rubber gradient between rails)
- Animated rollers: perpendicular lines shifting with `_frame`
- Use `PushTransform(rotation)` for clean drawing

### Entities
- Small isometric box (3-face parallelogram via StreamGeometry)
- Color from source node, darker side faces
- Direction-aware (4 pre-built orientations)

## Performance
- Static geometry caching (floor, casings) — recompute only on resize
- ~100 roller draws + ~240 entity face draws per frame — well within DrawingContext budget
- Stopwatch-based frame budget monitoring

## Implementation Phases

1. **Extract + scaffold** — INodeRenderer, SchematicRenderer, RenderPalette. No visual change.
2. **Toggle mechanism** — enum, R key, stub RealisticRenderer
3. **Floor + background** — concrete tiles, vignette, safety markings
4. **Machine sprites** — server, buffer, source, sink
5. **Conveyor belts** — rails, surface, animated rollers
6. **Entity sprites** — isometric boxes
7. **Polish** — legend, performance profiling, color tuning
