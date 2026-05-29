using System;
using System.Collections.Generic;
using System.Linq;
using SimOpt.GridWorld.Environment;

namespace SimOpt.GridWorld.Agents;

public class GridObservation
{
    public CellType[,] LocalView { get; }
    public int AgentX { get; }
    public int AgentY { get; }
    public int ViewRadius { get; }
    public IReadOnlyList<VisibleAgent> VisibleAgents { get; }

    private GridObservation(CellType[,] localView, int agentX, int agentY,
        int viewRadius, IReadOnlyList<VisibleAgent> visibleAgents)
    {
        LocalView = localView;
        AgentX = agentX;
        AgentY = agentY;
        ViewRadius = viewRadius;
        VisibleAgents = visibleAgents;
    }

    public static GridObservation FromGrid(Grid grid, int agentX, int agentY,
        int viewRadius, IEnumerable<(string Id, int X, int Y, bool Alive)>? otherAgents = null)
    {
        int size = 2 * viewRadius + 1;
        var view = new CellType[size, size];

        for (int dx = -viewRadius; dx <= viewRadius; dx++)
        {
            for (int dy = -viewRadius; dy <= viewRadius; dy++)
            {
                int wx = agentX + dx;
                int wy = agentY + dy;
                int lx = dx + viewRadius;
                int ly = dy + viewRadius;

                view[lx, ly] = grid.InBounds(wx, wy) ? grid[wx, wy] : CellType.Wall;
            }
        }

        var visible = new List<VisibleAgent>();
        if (otherAgents != null)
        {
            foreach (var (id, x, y, alive) in otherAgents)
            {
                if (Math.Abs(x - agentX) <= viewRadius && Math.Abs(y - agentY) <= viewRadius)
                    visible.Add(new VisibleAgent(id, x, y, alive));
            }
        }

        return new GridObservation(view, agentX, agentY, viewRadius, visible);
    }
}

public record VisibleAgent(string Id, int X, int Y, bool IsAlive);
