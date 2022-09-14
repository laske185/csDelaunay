namespace csDelaunay.Delaunay;

public class Triangle
{
    private readonly List<Site> sites = new(3);

    public Triangle(Site a, Site b, Site c)
    {
        sites.Add(a);
        sites.Add(b);
        sites.Add(c);
    }

    public List<Site> Sites => sites;

    public void Dispose()
    {
        sites.Clear();
    }
}
