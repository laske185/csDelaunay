namespace csDelaunay.Delaunay;

public class Halfedge
{
    #region Pool

    private static readonly Queue<Halfedge> pool = new();

    public static Halfedge Create(Edge edge, LR lr)
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue().Init(edge, lr);
        }
        else
        {
            return new Halfedge(edge, lr);
        }
    }

    public static Halfedge CreateDummy()
    {
        return Create(null, null);
    }

    #endregion Pool

    #region Object

    public Halfedge(Edge edge, LR lr)
    {
        Init(edge, lr);
    }

    // The vertex's y-coordinate in the transformed Voronoi space V
    public float YStar { get; set; }

    public Edge Edge { get; set; }
    public LR LeftRight { get; set; }
    public Vertex Vertex { get; set; }
    public Halfedge EdgeListLeftNeighbor { get; set; }
    public Halfedge EdgeListRightNeighbor { get; set; }
    public Halfedge NextInPriorityQueue { get; set; }

    public override string ToString()
    {
        return $"Halfedge (LeftRight: {LeftRight}; vertex: {Vertex})";
    }

    public void Dispose()
    {
        if (EdgeListLeftNeighbor != null || EdgeListRightNeighbor != null)
        {
            // still in EdgeList
            return;
        }
        if (NextInPriorityQueue != null)
        {
            // still in PriorityQueue
            return;
        }
        Edge = null;
        LeftRight = null;
        Vertex = null;
        pool.Enqueue(this);
    }

    public void ReallyDispose()
    {
        EdgeListLeftNeighbor = null;
        EdgeListRightNeighbor = null;
        NextInPriorityQueue = null;
        Edge = null;
        LeftRight = null;
        Vertex = null;
        pool.Enqueue(this);
    }

    public bool IsLeftOf(Vector2 p)
    {
        Site topSite;
        bool rightOfSite, above, fast;
        float dxp, dyp, dxs, t1, t2, t3, y1;

        topSite = Edge.RightSite;
        rightOfSite = p.X > topSite.x;
        if (rightOfSite && LeftRight == LR.LEFT)
        {
            return true;
        }
        if (!rightOfSite && LeftRight == LR.RIGHT)
        {
            return false;
        }

        if (Edge.a == 1)
        {
            dyp = p.Y - topSite.y;
            dxp = p.X - topSite.x;
            fast = false;
            if (!rightOfSite && Edge.b < 0 || rightOfSite && Edge.b >= 0)
            {
                above = dyp >= Edge.b * dxp;
                fast = above;
            }
            else
            {
                above = p.X + p.Y * Edge.b > Edge.c;
                if (Edge.b < 0)
                {
                    above = !above;
                }
                if (!above)
                {
                    fast = true;
                }
            }
            if (!fast)
            {
                dxs = topSite.x - Edge.LeftSite.x;
                above = Edge.b * (dxp * dxp - dyp * dyp) < dxs * dyp * (1 + 2 * dxp / dxs + Edge.b * Edge.b);
                if (Edge.b < 0)
                {
                    above = !above;
                }
            }
        }
        else
        {
            y1 = Edge.c - Edge.a * p.X;
            t1 = p.Y - y1;
            t2 = p.X - topSite.x;
            t3 = y1 - topSite.y;
            above = t1 * t1 > t2 * t2 + t3 * t3;
        }
        return LeftRight == LR.LEFT ? above : !above;
    }

    private Halfedge Init(Edge edge, LR lr)
    {
        Edge = edge;
        LeftRight = lr;
        NextInPriorityQueue = null;
        Vertex = null;

        return this;
    }

    #endregion Object
}
