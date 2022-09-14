using csDelaunay.Delaunay;

namespace csDelaunay.Geometries;

/// <summary>
/// A immutable line segment.
/// </summary>
public class LineSegment
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineSegment"/> class.
    /// </summary>
    /// <param name="p">The first point.</param>
    /// <param name="q">The second point.</param>
    public LineSegment(Vector2 p, Vector2 q)
    {
        P = p;
        Q = q;
    }

    /// <summary>
    /// Gets the starting point.
    /// </summary>
    /// <value>
    /// The first point.
    /// </value>
    public Vector2 P { get; }

    /// <summary>
    /// Gets the targeting point.
    /// </summary>
    /// <value>
    /// The second point.
    /// </value>
    public Vector2 Q { get; }

    public static List<LineSegment> VisibleLineSegments(List<Edge> edges)
    {
        var segments = new List<LineSegment>();

        foreach (var edge in edges)
        {
            if (edge.Visible())
            {
                var p1 = edge.ClippedEnds[LR.LEFT];
                var p2 = edge.ClippedEnds[LR.RIGHT];
                segments.Add(new LineSegment(p1, p2));
            }
        }

        return segments;
    }

    public static float CompareLengths_MAX(LineSegment segment0, LineSegment segment1)
    {
        float length0 = (segment0.P - segment0.Q).LengthSquared();
        float length1 = (segment1.P - segment1.Q).LengthSquared();
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

    public static float CompareLengths(LineSegment edge0, LineSegment edge1)
    {
        return -CompareLengths_MAX(edge0, edge1);
    }
}
