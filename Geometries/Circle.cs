namespace csDelaunay.Geometries;

/// <summary>
/// A immutable circle.
/// </summary>
public class Circle
{
    public Circle(float centerX, float centerY, float radius)
    {
        Center = new Vector2(centerX, centerY);
        Radius = radius;
    }

    /// <summary>
    /// Gets the radius of the circle.
    /// </summary>
    /// <value>
    /// The radius.
    /// </value>
    public float Radius { get; }

    /// <summary>
    /// Gets the center of the circle.
    /// </summary>
    /// <value>
    /// The center.
    /// </value>
    public Vector2 Center { get; }

    public override string ToString()
    {
        return $"Circle (center: {Center}; radius: {Radius}";
    }
}
