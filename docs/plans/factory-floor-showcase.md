# Factory Floor Showcase — Implementation Plan

## Vision
A realistic electronics assembly factory with conveyors (Fließbänder), workstations, quality control, parallel lines, and rejection flows. Physical 2D layout with real dimensions (meters). The most impressive SimOpt demo.

## Factory Layout (~50m × 30m)

```
     DOCK A              DOCK B
       ↓                   ↓
  [Source A]           [Source B]
       ↓                   ↓
  ═══════════════════════════════  Conveyor C1 (incoming, 20m)
                 ↓
           [Inspection]  ← rejects 5% → [Waste Bin]
                 ↓
  ═══════════════  Conveyor C2 (to staging, 10m)
                 ↓
           [Staging Buffer]  (capacity 30)
          ↙       ↓       ↘
   [Asm Line 1] [Asm Line 2] [Asm Line 3]   ← 3 parallel assembly stations
          ↘       ↓       ↙
  ═══════════════  Conveyor C3 (to QC, 12m)
                 ↓
           [Quality Control]  ← rejects 3% → [Rework Buffer]
                 ↓
  ═══════════════  Conveyor C4 (to packing, 8m)
                 ↓
           [Packing Station]
                 ↓
  ═══════════════  Conveyor C5 (to shipping, 15m)
                 ↓
           [Shipping Dock]  (sink)
```

## Entity Types
- **PCB** (green) — circuit boards from Dock A
- **Casing** (blue) — metal casings from Dock B
- **Assembly** (yellow) — combined at assembly stations
- **Reject** (red) — failed items

## Realistic Parameters
- Source A: PCBs every ~45 seconds (Gaussian, σ=5s)
- Source B: Casings every ~50 seconds (Gaussian, σ=8s)
- Inspection: 20s per item, 5% rejection
- Assembly: 90s per unit (needs 1 PCB + 1 Casing)
- QC: 30s per unit, 3% rejection
- Packing: 15s per unit
- Conveyors: 0.5 m/s, various lengths

## Implementation Phases

### Phase A — Physical Layout Engine (this session)
1. Extend VizTopology with physical coordinates: `x`, `y`, `width`, `height` per node
2. Add `"conveyor"` node type with `start_x/y`, `end_x/y`, `belt_width`
3. Canvas renders with physical coordinates (meters → pixels via zoom/scale)
4. Conveyor rendering: long thin rectangle with directional arrows
5. Scale bar and dimension labels

### Phase B — Factory Model (next session)
1. Build the full factory topology JSON
2. Implement rejection flows (server with probability output split)
3. Multiple entity types (color-coded)
4. Assembly stations that consume 2 inputs (PCB + Casing → Assembly)

### Phase C — Polish (session after)
1. Statistics panel: utilization per station, throughput, WIP, bottleneck highlighting
2. Zoom/pan controls
3. Time-lapse mode (fast forward)
4. Machine state color transitions (idle→busy→damaged animation)
5. Conveyor items shown as small colored rectangles moving along the belt
6. Floor grid with dimension markers

## Technical Decisions
- Physical coordinates in meters, canvas converts via scale factor
- Default scale: 1m = 15px (50m factory = 750px width — fits most screens)
- Conveyors rendered as filled rectangles with animated stripe pattern
- Nodes positioned absolutely (no auto-layout for factory — physical positions matter)
- Keep auto-layout as fallback for non-physical topologies
