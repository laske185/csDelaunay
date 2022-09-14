using csDelaunay.Geometries;

namespace csDelaunay.Delaunay;

public class Site : ICoord
{
    private const float EPSILON = 0.005f;
    private static readonly Queue<Site> pool = new();

    private int siteIndex;

    private Vector2 coord;

    private float weigth;

    // The edges that define this Site's Voronoi region:
    private List<Edge> edges;

    // which end of each edge hooks up with the previous edge in edges:
    private List<LR> edgeOrientations;

    // ordered list of points that define the region clipped to bounds:
    private List<Vector2> region;

    public Site(Vector2 p, int index, float weigth)
    {
        Init(p, index, weigth);
    }

    public int SiteIndex
    { get { return siteIndex; } set { siteIndex = value; } }

    public Vector2 Coord
    { get { return coord; } set { coord = value; } }

    public float x
    { get { return coord.X; } }

    public float y
    { get { return coord.Y; } }

    public float Weigth
    { get { return weigth; } }

    public List<Edge> Edges
    { get { return edges; } }

    public static Site Create(Vector2 p, int index, float weigth)
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue().Init(p, index, weigth);
        }
        else
        {
            return new Site(p, index, weigth);
        }
    }

    public static void SortSites(List<Site> sites)
    {
        sites.Sort(delegate (Site s0, Site s1)
        {
            var returnValue = Voronoi.CompareByYThenX(s0, s1);

            int tempIndex;

            if (returnValue == -1)
            {
                if (s0.siteIndex > s1.SiteIndex)
                {
                    tempIndex = s0.SiteIndex;
                    s0.SiteIndex = s1.SiteIndex;
                    s1.SiteIndex = tempIndex;
                }
            }
            else if (returnValue == 1)
            {
                if (s1.SiteIndex > s0.SiteIndex)
                {
                    tempIndex = s1.SiteIndex;
                    s1.SiteIndex = s0.SiteIndex;
                    s0.SiteIndex = tempIndex;
                }
            }

            return returnValue;
        });
    }

    public int Compare(Site s1, Site s2)
    {
        return s1.CompareTo(s2);
    }

    public int CompareTo(Site s1)
    {
        var returnValue = Voronoi.CompareByYThenX(this, s1);

        int tempIndex;

        if (returnValue == -1)
        {
            if (siteIndex > s1.SiteIndex)
            {
                tempIndex = SiteIndex;
                SiteIndex = s1.SiteIndex;
                s1.SiteIndex = tempIndex;
            }
        }
        else if (returnValue == 1)
        {
            if (s1.SiteIndex > SiteIndex)
            {
                tempIndex = s1.SiteIndex;
                s1.SiteIndex = SiteIndex;
                SiteIndex = tempIndex;
            }
        }

        return returnValue;
    }

    public override string ToString()
    {
        return "Site " + siteIndex + ": " + coord;
    }

    public void Dispose()
    {
        Clear();
        pool.Enqueue(this);
    }

    public void AddEdge(Edge edge)
    {
        edges.Add(edge);
    }

    public Edge NearestEdge()
    {
        edges.Sort(Edge.CompareSitesDistances);
        return edges[0];
    }

    public List<Site> NeighborSites()
    {
        if (edges == null || edges.Count == 0)
        {
            return new List<Site>();
        }
        if (edgeOrientations == null)
        {
            ReorderEdges();
        }
        var list = new List<Site>();
        foreach (var edge in edges)
        {
            list.Add(NeighborSite(edge));
        }
        return list;
    }

    public List<Vector2> Region(RectangleF clippingBounds)
    {
        if (edges == null || edges.Count == 0)
        {
            return new List<Vector2>();
        }
        if (edgeOrientations == null)
        {
            ReorderEdges();
        }
        if (region == null)
        {
            region = ClipToBounds(clippingBounds);
            if (new Polygon(region).PolyWinding() == Winding.CLOCKWISE)
            {
                region.Reverse();
            }
        }
        return region;
    }

    public float Dist(ICoord p)
    {
        return (Coord - p.Coord).Length();
    }

    private static bool CloseEnough(Vector2 p0, Vector2 p1)
    {
        return (p0 - p1).LengthSquared() < EPSILON;
    }

    private Site Init(Vector2 p, int index, float weigth)
    {
        coord = p;
        siteIndex = index;
        this.weigth = weigth;
        edges = new List<Edge>();
        region = null;

        return this;
    }

    private void Move(Vector2 p)
    {
        Clear();
        coord = p;
    }

    private void Clear()
    {
        if (edges != null)
        {
            edges.Clear();
            edges = null;
        }
        if (edgeOrientations != null)
        {
            edgeOrientations.Clear();
            edgeOrientations = null;
        }
        if (region != null)
        {
            region.Clear();
            region = null;
        }
    }

    private Site NeighborSite(Edge edge)
    {
        if (this == edge.LeftSite)
        {
            return edge.RightSite;
        }
        if (this == edge.RightSite)
        {
            return edge.LeftSite;
        }
        return null;
    }

    private void ReorderEdges()
    {
        var reorderer = new EdgeReorderer(edges, typeof(Vertex));
        edges = reorderer.Edges;
        edgeOrientations = reorderer.EdgeOrientations;
        reorderer.Dispose();
    }

    private List<Vector2> ClipToBounds(RectangleF bounds)
    {
        var points = new List<Vector2>();
        var n = edges.Count;
        var i = 0;
        Edge edge;

        while (i < n && !edges[i].Visible())
        {
            i++;
        }

        if (i == n)
        {
            // No edges visible
            return new List<Vector2>();
        }
        edge = edges[i];
        var orientation = edgeOrientations[i];
        points.Add(edge.ClippedEnds[orientation]);
        points.Add(edge.ClippedEnds[LR.Other(orientation)]);

        for (var j = i + 1; j < n; j++)
        {
            edge = edges[j];
            if (!edge.Visible())
            {
                continue;
            }
            Connect(ref points, j, bounds);
        }
        // Close up the polygon by adding another corner point of the bounds if needed:
        Connect(ref points, i, bounds, true);

        return points;
    }

    private void Connect(ref List<Vector2> points, int j, RectangleF bounds, bool closingUp = false)
    {
        var rightPoint = points[points.Count - 1];
        var newEdge = edges[j];
        var newOrientation = edgeOrientations[j];

        // The point that must be conected to rightPoint:
        var newPoint = newEdge.ClippedEnds[newOrientation];

        if (!CloseEnough(rightPoint, newPoint))
        {
            // The points do not coincide, so they must have been clipped at the bounds;
            // see if they are on the same border of the bounds:
            if (rightPoint != newPoint)
            {
                // They are on different borders of the bounds;
                // insert one or two corners of bounds as needed to hook them up:
                // (NOTE this will not be correct if the region should take up more than
                // half of the bounds rect, for then we will have gone the wrong way
                // around the bounds and included the smaller part rather than the larger)
                var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                var newCheck = BoundsCheck.Check(newPoint, bounds);
                float px, py;
                if ((rightCheck & BoundsCheck.RIGHT) != 0)
                {
                    px = bounds.Right;

                    if ((newCheck & BoundsCheck.BOTTOM) != 0)
                    {
                        py = bounds.Bottom;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.TOP) != 0)
                    {
                        py = bounds.Top;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.LEFT) != 0)
                    {
                        if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height)
                        {
                            py = bounds.Top;
                        }
                        else
                        {
                            py = bounds.Bottom;
                        }
                        points.Add(new Vector2(px, py));
                        points.Add(new Vector2(bounds.Left, py));
                    }
                }
                else if ((rightCheck & BoundsCheck.LEFT) != 0)
                {
                    px = bounds.Left;

                    if ((newCheck & BoundsCheck.BOTTOM) != 0)
                    {
                        py = bounds.Bottom;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.TOP) != 0)
                    {
                        py = bounds.Top;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.RIGHT) != 0)
                    {
                        if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height)
                        {
                            py = bounds.Top;
                        }
                        else
                        {
                            py = bounds.Bottom;
                        }
                        points.Add(new Vector2(px, py));
                        points.Add(new Vector2(bounds.Right, py));
                    }
                }
                else if ((rightCheck & BoundsCheck.TOP) != 0)
                {
                    py = bounds.Top;

                    if ((newCheck & BoundsCheck.RIGHT) != 0)
                    {
                        px = bounds.Right;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.LEFT) != 0)
                    {
                        px = bounds.Left;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.BOTTOM) != 0)
                    {
                        if (rightPoint.X - bounds.X + newPoint.X - bounds.X < bounds.Width)
                        {
                            px = bounds.Left;
                        }
                        else
                        {
                            px = bounds.Right;
                        }
                        points.Add(new Vector2(px, py));
                        points.Add(new Vector2(px, bounds.Bottom));
                    }
                }
                else if ((rightCheck & BoundsCheck.BOTTOM) != 0)
                {
                    py = bounds.Bottom;

                    if ((newCheck & BoundsCheck.RIGHT) != 0)
                    {
                        px = bounds.Right;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.LEFT) != 0)
                    {
                        px = bounds.Left;
                        points.Add(new Vector2(px, py));
                    }
                    else if ((newCheck & BoundsCheck.TOP) != 0)
                    {
                        if (rightPoint.X - bounds.X + newPoint.X - bounds.X < bounds.Width)
                        {
                            px = bounds.Left;
                        }
                        else
                        {
                            px = bounds.Right;
                        }
                        points.Add(new Vector2(px, py));
                        points.Add(new Vector2(px, bounds.Top));
                    }
                }
            }
            if (closingUp)
            {
                // newEdge's ends have already been added
                return;
            }
            points.Add(newPoint);
        }
        var newRightPoint = newEdge.ClippedEnds[LR.Other(newOrientation)];
        if (!CloseEnough(points[0], newRightPoint))
        {
            points.Add(newRightPoint);
        }
    }
}

public class BoundsCheck
{
    public const int TOP = 1;
    public const int BOTTOM = 2;
    public const int LEFT = 4;
    public const int RIGHT = 8;

    /*
     *
     * @param point
     * @param bounds
     * @return an int with the appropriate bits set if the Point lies on the corresponding bounds lines
     */

    public static int Check(Vector2 point, RectangleF bounds)
    {
        var value = 0;
        if (point.X == bounds.Left)
        {
            value |= LEFT;
        }
        if (point.X == bounds.Right)
        {
            value |= RIGHT;
        }
        if (point.Y == bounds.Top)
        {
            value |= TOP;
        }
        if (point.Y == bounds.Bottom)
        {
            value |= BOTTOM;
        }

        return value;
    }
}
