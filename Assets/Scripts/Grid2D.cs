using UnityEngine;
using System.Collections;

public class Grid2D
{
    public float Step { get; set; }
    public Rect Bounds { get; set; }

    public Grid2D()
    {
        Step = 1f;
        Bounds = new Rect(-5, -5, 10, 10);
    }
}
