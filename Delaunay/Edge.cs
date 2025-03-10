﻿namespace csDelaunay.Delaunay;

/// <summary>
/// The line segment connecting the two Sites is part of the Delaunay triangulation.
/// The line segment connecting the two Vertices is part of the Voronoi diagram
/// </summary>
public class Edge
{
    #region Pool

    private static Queue<Edge> pool = new();

    private static int nEdges = 0;
    /*
     * This is the only way to create a new Edge
     * @param site0
     * @param site1
     * @return
     */

    public static Edge CreateBisectingEdge(Site s0, Site s1)
    {
        float dx, dy;
        float absdx, absdy;
        float a, b, c;

        dx = s1.x - s0.x;
        dy = s1.y - s0.y;
        absdx = dx > 0 ? dx : -dx;
        absdy = dy > 0 ? dy : -dy;
        c = s0.x * dx + s0.y * dy + (dx * dx + dy * dy) * 0.5f;

        if (absdx > absdy)
        {
            a = 1;
            b = dy / dx;
            c /= dx;
        }
        else
        {
            b = 1;
            a = dx / dy;
            c /= dy;
        }

        var edge = Create();

        edge.LeftSite = s0;
        edge.RightSite = s1;
        s0.AddEdge(edge);
        s1.AddEdge(edge);

        edge.a = a;
        edge.b = b;
        edge.c = c;

        return edge;
    }

    private static Edge Create()
    {
        Edge edge;
        if (pool.Count > 0)
        {
            edge = pool.Dequeue();
            edge.Init();
        }
        else
        {
            edge = new Edge();
        }

        return edge;
    }

    #endregion Pool

    public static readonly Edge DELETED = new Edge();

    public static List<Edge> SelectEdgesForSitePoint(Vector2 coord, List<Edge> edgesToTest)
    {
        return edgesToTest.FindAll(
        delegate (Edge e)
        {
            if (e.LeftSite != null)
            {
                if (e.LeftSite.Coord == coord) return true;
            }
            if (e.RightSite != null)
            {
                if (e.RightSite.Coord == coord) return true;
            }
            return false;
        });
    }

    #region Object

    // The equation of the edge: ax + by = c
    public float a, b, c;

    // The two Voronoi vertices that the edge connects (if one of them is null, the edge extends to infinity)
    private Vertex leftVertex;

    private Vertex rightVertex;

    // Once clipVertices() is called, this Disctinary will hold two Points
    // representing the clipped coordinates of the left and the right ends...
    private LRCollection<Vector2> clippedVertices;

    // The two input Sites for which this Edge is a bisector:
    private LRCollection<Site> sites;

    private int edgeIndex;

    public Edge()
    {
        edgeIndex = nEdges++;
        Init();
    }

    public Vertex LeftVertex
    { get { return leftVertex; } }

    public Vertex RightVertex
    { get { return rightVertex; } }

    public LRCollection<Vector2> ClippedEnds
    { get { return clippedVertices; } }

    public Site LeftSite
    { get { return sites[LR.LEFT]; } set { sites[LR.LEFT] = value; } }

    public Site RightSite
    { get { return sites[LR.RIGHT]; } set { sites[LR.RIGHT] = value; } }

    public int EdgeIndex
    { get { return edgeIndex; } }

    public static int CompareSitesDistances_MAX(Edge edge0, Edge edge1)
    {
        var length0 = edge0.SitesDistance();
        var length1 = edge1.SitesDistance();
        if (length0 < length1)
        {
            return 1;
        }
        if (length0 > length1)
        {
            return -1;
        }
        return 0;
    }

    public static int CompareSitesDistances(Edge edge0, Edge edge1)
    {
        return -CompareSitesDistances_MAX(edge0, edge1);
    }

    public Vertex Vertex(LR leftRight)
    {
        return leftRight == LR.LEFT ? leftVertex : rightVertex;
    }

    public void SetVertex(LR leftRight, Vertex v)
    {
        if (leftRight == LR.LEFT)
        {
            leftVertex = v;
        }
        else
        {
            rightVertex = v;
        }
    }

    public bool IsPartOfConvexHull()
    {
        return leftVertex == null || rightVertex == null;
    }

    public float SitesDistance()
    {
        return (LeftSite.Coord - RightSite.Coord).Length();
    }

    // Unless the entire Edge is outside the bounds.
    // In that case visible will be false:
    public bool Visible()
    {
        return clippedVertices != null;
    }

    public Site Site(LR leftRight)
    {
        return sites[leftRight];
    }

    public void Dispose()
    {
        leftVertex = null;
        rightVertex = null;
        if (clippedVertices != null)
        {
            clippedVertices.Clear();
            clippedVertices = null;
        }
        sites.Clear();
        sites = null;

        pool.Enqueue(this);
    }

    public Edge Init()
    {
        sites = new LRCollection<Site>();

        return this;
    }

    public override string ToString()
    {
        return "Edge " + edgeIndex + "; sites " + sites[LR.LEFT] + ", " + sites[LR.RIGHT] +
            "; endVertices " + (leftVertex != null ? leftVertex.VertexIndex.ToString() : "null") + ", " +
                (rightVertex != null ? rightVertex.VertexIndex.ToString() : "null") + "::";
    }

    /// <summary>
    /// Set clippedVertices to contain the two ends of the portion of the Voronoi edge that is visible
    /// within the bounds. If no part of the Edge falls within the bounds, leave clippedVertices null.
    /// </summary>
    /// <param name="bounds">The bounds.</param>
    public void ClipVertices(RectangleF bounds)
    {
        float xmin = bounds.X;
        float ymin = bounds.Y;
        float xmax = bounds.Right;
        float ymax = bounds.Bottom;

        Vertex vertex0, vertex1;
        float x0, x1, y0, y1;

        if (a == 1 && b >= 0)
        {
            vertex0 = rightVertex;
            vertex1 = leftVertex;
        }
        else
        {
            vertex0 = leftVertex;
            vertex1 = rightVertex;
        }

        if (a == 1)
        {
            y0 = ymin;
            if (vertex0 != null && vertex0.y > ymin)
            {
                y0 = vertex0.y;
            }
            if (y0 > ymax)
            {
                return;
            }
            x0 = c - b * y0;

            y1 = ymax;
            if (vertex1 != null && vertex1.y < ymax)
            {
                y1 = vertex1.y;
            }
            if (y1 < ymin)
            {
                return;
            }
            x1 = c - b * y1;

            if (x0 > xmax && x1 > xmax || x0 < xmin && x1 < xmin)
            {
                return;
            }

            if (x0 > xmax)
            {
                x0 = xmax;
                y0 = (c - x0) / b;
            }
            else if (x0 < xmin)
            {
                x0 = xmin;
                y0 = (c - x0) / b;
            }

            if (x1 > xmax)
            {
                x1 = xmax;
                y1 = (c - x1) / b;
            }
            else if (x1 < xmin)
            {
                x1 = xmin;
                y1 = (c - x1) / b;
            }
        }
        else
        {
            x0 = xmin;
            if (vertex0 != null && vertex0.x > xmin)
            {
                x0 = vertex0.x;
            }
            if (x0 > xmax)
            {
                return;
            }
            y0 = c - a * x0;

            x1 = xmax;
            if (vertex1 != null && vertex1.x < xmax)
            {
                x1 = vertex1.x;
            }
            if (x1 < xmin)
            {
                return;
            }
            y1 = c - a * x1;

            if (y0 > ymax && y1 > ymax || y0 < ymin && y1 < ymin)
            {
                return;
            }

            if (y0 > ymax)
            {
                y0 = ymax;
                x0 = (c - y0) / a;
            }
            else if (y0 < ymin)
            {
                y0 = ymin;
                x0 = (c - y0) / a;
            }

            if (y1 > ymax)
            {
                y1 = ymax;
                x1 = (c - y1) / a;
            }
            else if (y1 < ymin)
            {
                y1 = ymin;
                x1 = (c - y1) / a;
            }
        }

        clippedVertices = new LRCollection<Vector2>();
        if (vertex0 == leftVertex)
        {
            clippedVertices[LR.LEFT] = new Vector2(x0, y0);
            clippedVertices[LR.RIGHT] = new Vector2(x1, y1);
        }
        else
        {
            clippedVertices[LR.RIGHT] = new Vector2(x0, y0);
            clippedVertices[LR.LEFT] = new Vector2(x1, y1);
        }
    }

    #endregion Object
}
