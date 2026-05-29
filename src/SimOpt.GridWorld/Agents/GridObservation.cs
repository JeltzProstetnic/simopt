using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public class GridObservation<TCoord> where TCoord : struct, IEquatable<TCoord>
{
    public IReadOnlyDictionary<TCoord, CellType> Cells { get; }
    public TCoord AgentPosition { get; }
    public int ViewRadius { get; }
    public IReadOnlyList<VisibleAgent<TCoord>> VisibleAgents { get; }

    public GridObservation(IReadOnlyDictionary<TCoord, CellType> cells,
        TCoord agentPosition, int viewRadius,
        IReadOnlyList<VisibleAgent<TCoord>> visibleAgents)
    {
        Cells = cells;
        AgentPosition = agentPosition;
        ViewRadius = viewRadius;
        VisibleAgents = visibleAgents;
    }

    public static GridObservation<TCoord> FromGrid(Grid<TCoord> grid, TCoord agentPosition,
        int viewRadius, IEnumerable<(string Id, TCoord Position, bool Alive)>? otherAgents = null)
    {
        var cells = new Dictionary<TCoord, CellType>();
        foreach (var coord in grid.Topology.Neighborhood(agentPosition, viewRadius))
            cells[coord] = grid.GetOrDefault(coord, CellType.Wall);

        var visible = new List<VisibleAgent<TCoord>>();
        if (otherAgents != null)
        {
            foreach (var (id, pos, alive) in otherAgents)
            {
                if (grid.Topology.Distance(agentPosition, pos) <= viewRadius)
                    visible.Add(new VisibleAgent<TCoord>(id, pos, alive));
            }
        }

        return new GridObservation<TCoord>(cells, agentPosition, viewRadius, visible);
    }
}

public record VisibleAgent<TCoord>(string Id, TCoord Position, bool IsAlive)
    where TCoord : struct, IEquatable<TCoord>;
