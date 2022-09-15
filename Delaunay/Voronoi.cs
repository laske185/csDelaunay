using csDelaunay.Geometries;

namespace csDelaunay.Delaunay;

public class Voronoi : IDisposable
{
    private SiteList sites;
    private List<Triangle> triangles;

    private List<Edge> edges;

    // TODO generalize this so it doesn't have to be a rectangle;
    // then we can make the fractal voronois-within-voronois
    private RectangleF plotBounds;

    private Dictionary<Vector2, Site> sitesIndexedByLocation;
    private Random weigthDistributor;

    public Voronoi(List<Vector2> points, RectangleF plotBounds)
    {
        weigthDistributor = new Random();
        Init(points, plotBounds);
    }

    public Voronoi(List<Vector2> points, RectangleF plotBounds, int lloydIterations)
        : this(points, plotBounds)
    {
        LloydRelaxation(lloydIterations);
    }

    public List<Edge> Edges => edges;

    public RectangleF PlotBounds => plotBounds;

    public Dictionary<Vector2, Site> SitesIndexedByLocation => sitesIndexedByLocation;

    public static int CompareByYThenX(Site s1, Site s2)
    {
        if (s1.y < s2.y) return -1;
        if (s1.y > s2.y) return 1;
        if (s1.x < s2.x) return -1;
        if (s1.x > s2.x) return 1;
        return 0;
    }

    public static int CompareByYThenX(Site s1, Vector2 s2)
    {
        if (s1.y < s2.Y) return -1;
        if (s1.y > s2.Y) return 1;
        if (s1.x < s2.X) return -1;
        if (s1.x > s2.X) return 1;
        return 0;
    }

    public void Dispose()
    {
        sites.Dispose();
        sites = null;

        foreach (var t in triangles)
        {
            t.Dispose();
        }
        triangles.Clear();

        foreach (var e in edges)
        {
            e.Dispose();
        }
        edges.Clear();

        plotBounds = RectangleF.Empty;
        sitesIndexedByLocation.Clear();
        sitesIndexedByLocation = null;
    }

    public List<Vector2> Region(Vector2 p)
    {
        if (sitesIndexedByLocation.TryGetValue(p, out var site))
        {
            return site.Region(plotBounds);
        }
        else
        {
            return new List<Vector2>();
        }
    }

    public List<Vector2> NeighborSitesForSite(Vector2 coord)
    {
        var points = new List<Vector2>();
        Site site;
        if (sitesIndexedByLocation.TryGetValue(coord, out site))
        {
            var sites = site.NeighborSites();
            foreach (var neighbor in sites)
            {
                points.Add(neighbor.Coord);
            }
        }

        return points;
    }

    public List<Circle> Circles()
    {
        return sites.Circles();
    }

    public List<LineSegment> VoronoiBoundarayForSite(Vector2 coord)
    {
        return LineSegment.VisibleLineSegments(Edge.SelectEdgesForSitePoint(coord, edges));
    }

    public List<LineSegment> VoronoiDiagram()
    {
        return LineSegment.VisibleLineSegments(edges);
    }

    public List<Edge> HullEdges()
    {
        return edges.FindAll(edge => edge.IsPartOfConvexHull());
    }

    public List<Vector2> HullPointsInOrder()
    {
        var hullEdges = HullEdges();

        var points = new List<Vector2>();
        if (hullEdges.Count == 0)
        {
            return points;
        }

        var reorderer = new EdgeReorderer(hullEdges, typeof(Site));
        hullEdges = reorderer.Edges;
        var orientations = reorderer.EdgeOrientations;
        reorderer.Dispose();

        LR orientation;
        for (var i = 0; i < hullEdges.Count; i++)
        {
            var edge = hullEdges[i];
            orientation = orientations[i];
            points.Add(edge.Site(orientation).Coord);
        }
        return points;
    }

    public List<List<Vector2>> Regions()
    {
        return sites.Regions(plotBounds);
    }

    public List<Vector2> SiteCoords()
    {
        return sites.SiteCoords();
    }

    public void LloydRelaxation(int nbIterations)
    {
        // Reapeat the whole process for the number of iterations asked
        for (var i = 0; i < nbIterations; i++)
        {
            var newPoints = new List<Vector2>();
            // Go thourgh all sites
            sites.ResetListIndex();
            var site = sites.Next();

            while (site != null)
            {
                // Loop all corners of the site to calculate the centroid
                var region = site.Region(plotBounds);
                if (region.Count < 1)
                {
                    site = sites.Next();
                    continue;
                }

                Vector2 centroid = Vector2.Zero;
                float signedArea = 0;
                float x0;
                float y0;
                float x1;
                float y1;
                float a;
                // For all vertices except last
                for (var j = 0; j < region.Count - 1; j++)
                {
                    x0 = region[j].X;
                    y0 = region[j].Y;
                    x1 = region[j + 1].X;
                    y1 = region[j + 1].Y;
                    a = x0 * y1 - x1 * y0;
                    signedArea += a;
                    centroid.X += (x0 + x1) * a;
                    centroid.Y += (y0 + y1) * a;
                }
                // Do last vertex
                x0 = region[region.Count - 1].X;
                y0 = region[region.Count - 1].Y;
                x1 = region[0].X;
                y1 = region[0].Y;
                a = x0 * y1 - x1 * y0;
                signedArea += a;
                centroid.X += (x0 + x1) * a;
                centroid.Y += (y0 + y1) * a;

                signedArea *= 0.5f;
                centroid.X /= 6 * signedArea;
                centroid.Y /= 6 * signedArea;
                // Move site to the centroid of its Voronoi cell
                newPoints.Add(centroid);
                site = sites.Next();
            }

            // Between each replacement of the cendroid of the cell,
            // we need to recompute Voronoi diagram:
            var origPlotBounds = plotBounds;
            Dispose();
            Init(newPoints, origPlotBounds);
        }
    }

    private void Init(List<Vector2> points, RectangleF plotBounds)
    {
        sites = new SiteList();
        sitesIndexedByLocation = new Dictionary<Vector2, Site>();
        AddSites(points);
        this.plotBounds = plotBounds;
        triangles = new List<Triangle>();
        edges = new List<Edge>();

        FortunesAlgorithm();
    }

    private void AddSites(List<Vector2> points)
    {
        for (var i = 0; i < points.Count; i++)
        {
            AddSite(points[i], i);
        }
    }

    private void AddSite(Vector2 p, int index)
    {
        var weigth = (float)weigthDistributor.NextDouble() * 100;
        var site = Site.Create(p, index, weigth);
        sites.Add(site);
        sitesIndexedByLocation[p] = site;
    }

    /*
    public List<LineSegment> DelaunayLinesForSite(Vector2 coord) {
        return DelaunayLinesForEdges(Edge.SelectEdgesForSitePoint(coord, edges));
    }*/
    /*
    public List<LineSegment> Hull() {
        return DelaunayLinesForEdges(HullEdges());
    }*/

    private void FortunesAlgorithm()
    {
        Site newSite, bottomSite, topSite, tempSite;
        Vertex v, vertex;
        Vector2 newIntStar = Vector2.Zero;
        LR leftRight;
        Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
        Edge edge;

        var dataBounds = sites.GetSitesBounds();

        var sqrtSitesNb = (int)Math.Sqrt(sites.Count() + 4);
        var heap = new HalfedgePriorityQueue(dataBounds.Y, dataBounds.Height, sqrtSitesNb);
        var edgeList = new EdgeList(dataBounds.X, dataBounds.Width, sqrtSitesNb);
        var halfEdges = new List<Halfedge>();
        var vertices = new List<Vertex>();

        var bottomMostSite = sites.Next();
        newSite = sites.Next();

        while (true)
        {
            if (!heap.Empty())
            {
                newIntStar = heap.Min();
            }

            if (newSite != null &&
                (heap.Empty() || CompareByYThenX(newSite, newIntStar) < 0))
            {
                // New site is smallest
                //Debug.Log("smallest: new site " + newSite);

                // Step 8:
                lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord);    // The halfedge just to the left of newSite
                                                                        //UnityEngine.Debug.Log("lbnd: " + lbnd);
                rbnd = lbnd.EdgeListRightNeighbor;      // The halfedge just to the right
                                                        //UnityEngine.Debug.Log("rbnd: " + rbnd);
                bottomSite = RightRegion(lbnd, bottomMostSite);         // This is the same as leftRegion(rbnd)
                                                                        // This Site determines the region containing the new site
                                                                        //UnityEngine.Debug.Log("new Site is in region of existing site: " + bottomSite);

                // Step 9
                edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                //UnityEngine.Debug.Log("new edge: " + edge);
                edges.Add(edge);

                bisector = Halfedge.Create(edge, LR.LEFT);
                halfEdges.Add(bisector);
                // Inserting two halfedges into edgelist constitutes Step 10:
                // Insert bisector to the right of lbnd:
                edgeList.Insert(lbnd, bisector);

                // First half of Step 11:
                if ((vertex = Vertex.Intersect(lbnd, bisector)) != null)
                {
                    vertices.Add(vertex);
                    heap.Remove(lbnd);
                    lbnd.Vertex = vertex;
                    lbnd.YStar = vertex.y + newSite.Dist(vertex);
                    heap.Insert(lbnd);
                }

                lbnd = bisector;
                bisector = Halfedge.Create(edge, LR.RIGHT);
                halfEdges.Add(bisector);
                // Second halfedge for Step 10::
                // Insert bisector to the right of lbnd:
                edgeList.Insert(lbnd, bisector);

                // Second half of Step 11:
                if ((vertex = Vertex.Intersect(bisector, rbnd)) != null)
                {
                    vertices.Add(vertex);
                    bisector.Vertex = vertex;
                    bisector.YStar = vertex.y + newSite.Dist(vertex);
                    heap.Insert(bisector);
                }

                newSite = sites.Next();
            }
            else if (!heap.Empty())
            {
                // Intersection is smallest
                lbnd = heap.ExtractMin();
                llbnd = lbnd.EdgeListLeftNeighbor;
                rbnd = lbnd.EdgeListRightNeighbor;
                rrbnd = rbnd.EdgeListRightNeighbor;
                bottomSite = LeftRegion(lbnd, bottomMostSite);
                topSite = RightRegion(rbnd, bottomMostSite);
                // These three sites define a Delaunay triangle
                // (not actually using these for anything...)
                // triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

                v = lbnd.Vertex;
                v.SetIndex();
                lbnd.Edge.SetVertex(lbnd.LeftRight, v);
                rbnd.Edge.SetVertex(rbnd.LeftRight, v);
                edgeList.Remove(lbnd);
                heap.Remove(rbnd);
                edgeList.Remove(rbnd);
                leftRight = LR.LEFT;
                if (bottomSite.y > topSite.y)
                {
                    tempSite = bottomSite;
                    bottomSite = topSite;
                    topSite = tempSite;
                    leftRight = LR.RIGHT;
                }
                edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                edges.Add(edge);
                bisector = Halfedge.Create(edge, leftRight);
                halfEdges.Add(bisector);
                edgeList.Insert(llbnd, bisector);
                edge.SetVertex(LR.Other(leftRight), v);
                if ((vertex = Vertex.Intersect(llbnd, bisector)) != null)
                {
                    vertices.Add(vertex);
                    heap.Remove(llbnd);
                    llbnd.Vertex = vertex;
                    llbnd.YStar = vertex.y + bottomSite.Dist(vertex);
                    heap.Insert(llbnd);
                }
                if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null)
                {
                    vertices.Add(vertex);
                    bisector.Vertex = vertex;
                    bisector.YStar = vertex.y + bottomSite.Dist(vertex);
                    heap.Insert(bisector);
                }
            }
            else
            {
                break;
            }
        }

        // Heap should be empty now
        heap.Dispose();
        edgeList.Dispose();

        foreach (var halfedge in halfEdges)
        {
            halfedge.ReallyDispose();
        }
        halfEdges.Clear();

        // we need the vertices to clip the edges
        foreach (var e in edges)
        {
            e.ClipVertices(plotBounds);
        }
        // But we don't actually ever use them again!
        foreach (var ve in vertices)
        {
            ve.Dispose();
        }
        vertices.Clear();
    }

    private Site LeftRegion(Halfedge he, Site bottomMostSite)
    {
        var edge = he.Edge;
        if (edge == null)
        {
            return bottomMostSite;
        }
        return edge.Site(he.LeftRight);
    }

    private Site RightRegion(Halfedge he, Site bottomMostSite)
    {
        var edge = he.Edge;
        if (edge == null)
        {
            return bottomMostSite;
        }
        return edge.Site(LR.Other(he.LeftRight));
    }
}
