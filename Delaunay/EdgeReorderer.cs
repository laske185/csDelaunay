namespace csDelaunay.Delaunay;

public class EdgeReorderer
{
    private readonly List<Edge> edges = new();
    private readonly List<LR> edgeOrientations = new();

    public EdgeReorderer(List<Edge> origEdges, Type criterion)
    {
        if (origEdges.Count > 0)
        {
            edges = ReorderEdges(origEdges, criterion);
        }
    }

    public List<Edge> Edges => edges;

    public List<LR> EdgeOrientations => edgeOrientations;

    public void Dispose()
    {
        edges.Clear();
        edgeOrientations.Clear();
    }

    private List<Edge> ReorderEdges(List<Edge> origEdges, Type criterion)
    {
        int i;
        var n = origEdges.Count;
        Edge edge;
        // We're going to reorder the edges in order of traversal
        var done = new List<bool>();
        var nDone = 0;
        for (var b = 0; b < n; b++) done.Add(false);
        var newEdges = new List<Edge>();

        i = 0;
        edge = origEdges[i];
        newEdges.Add(edge);
        edgeOrientations.Add(LR.LEFT);
        ICoord firstPoint;
        ICoord lastPoint;
        if (criterion == typeof(Vertex))
        {
            firstPoint = edge.LeftVertex;
            lastPoint = edge.RightVertex;
        }
        else
        {
            firstPoint = edge.LeftSite;
            lastPoint = edge.RightSite;
        }

        if (firstPoint == Vertex.VERTEX_AT_INFINITY || lastPoint == Vertex.VERTEX_AT_INFINITY)
        {
            return new List<Edge>();
        }

        done[i] = true;
        nDone++;

        while (nDone < n)
        {
            for (i = 1; i < n; i++)
            {
                if (done[i])
                {
                    continue;
                }
                edge = origEdges[i];
                ICoord leftPoint;
                ICoord rightPoint;
                if (criterion == typeof(Vertex))
                {
                    leftPoint = edge.LeftVertex;
                    rightPoint = edge.RightVertex;
                }
                else
                {
                    leftPoint = edge.LeftSite;
                    rightPoint = edge.RightSite;
                }
                if (leftPoint == Vertex.VERTEX_AT_INFINITY || rightPoint == Vertex.VERTEX_AT_INFINITY)
                {
                    return new List<Edge>();
                }
                if (leftPoint == lastPoint)
                {
                    lastPoint = rightPoint;
                    edgeOrientations.Add(LR.LEFT);
                    newEdges.Add(edge);
                    done[i] = true;
                }
                else if (rightPoint == firstPoint)
                {
                    firstPoint = leftPoint;
                    edgeOrientations.Insert(0, LR.LEFT);
                    newEdges.Insert(0, edge);
                    done[i] = true;
                }
                else if (leftPoint == firstPoint)
                {
                    firstPoint = rightPoint;
                    edgeOrientations.Insert(0, LR.RIGHT);
                    newEdges.Insert(0, edge);
                    done[i] = true;
                }
                else if (rightPoint == lastPoint)
                {
                    lastPoint = leftPoint;
                    edgeOrientations.Add(LR.RIGHT);
                    newEdges.Add(edge);
                    done[i] = true;
                }
                if (done[i])
                {
                    nDone++;
                }
            }
        }
        return newEdges;
    }
}
