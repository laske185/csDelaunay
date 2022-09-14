namespace csDelaunay.Delaunay;

public class LR
{
    public static readonly LR LEFT = new("left");
    public static readonly LR RIGHT = new("right");

    private readonly string name;

    private LR(string name)
    {
        this.name = name;
    }

    public static LR Other(LR leftRight)
    {
        return leftRight == LEFT ? RIGHT : LEFT;
    }

    public override string ToString()
    {
        return name;
    }
}
