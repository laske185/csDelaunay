using csDelaunay.Geometries;

namespace csDelaunay.Delaunay;

public class SiteList
{
    private readonly List<Site> sites = new();
    private int currentIndex;

    private bool sorted = false;

    public void Dispose()
    {
        sites.Clear();
    }

    public int Add(Site site)
    {
        sorted = false;
        sites.Add(site);
        return sites.Count;
    }

    public int Count()
    {
        return sites.Count;
    }

    public Site? Next()
    {
        if (!sorted)
        {
            throw new Exception("SiteList.Next(): sites have not been sorted");
        }
        if (currentIndex < sites.Count)
        {
            return sites[currentIndex++];
        }
        else
        {
            return null;
        }
    }

    public RectangleF GetSitesBounds()
    {
        if (!sorted)
        {
            SortList();
            ResetListIndex();
        }
        float xmin, xmax, ymin, ymax;
        if (sites.Count == 0)
        {
            return RectangleF.Empty;
        }
        xmin = float.MaxValue;
        xmax = float.MinValue;
        foreach (var site in sites)
        {
            if (site.x < xmin) xmin = site.x;
            if (site.x > xmax) xmax = site.x;
        }
        // here's where we assume that the sites have been sorted on y:
        ymin = sites[0].y;
        ymax = sites[sites.Count - 1].y;

        return new RectangleF(xmin, ymin, xmax - xmin, ymax - ymin);
    }

    public List<Vector2> SiteCoords()
    {
        var coords = new List<Vector2>();
        foreach (var site in sites)
        {
            coords.Add(site.Coord);
        }

        return coords;
    }

    /*
     *
     * @return the largest circle centered at each site that fits in its region;
     * if the region is infinite, return a circle of radius 0.
     */

    public List<Circle> Circles()
    {
        var circles = new List<Circle>();
        foreach (var site in sites)
        {
            float radius = 0;
            var nearestEdge = site.NearestEdge();

            if (!nearestEdge.IsPartOfConvexHull()) radius = nearestEdge.SitesDistance() * 0.5f;
            circles.Add(new Circle(site.x, site.y, radius));
        }
        return circles;
    }

    public List<List<Vector2>> Regions(RectangleF plotBounds)
    {
        var regions = new List<List<Vector2>>();
        foreach (var site in sites)
        {
            regions.Add(site.Region(plotBounds));
        }
        return regions;
    }

    public void ResetListIndex()
    {
        currentIndex = 0;
    }

    public void SortList()
    {
        Site.SortSites(sites);
        sorted = true;
    }
}
