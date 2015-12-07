using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> Vertices { get; private set; }
    public List<int> Triangles { get; private set; }

    public MeshData()
    {
        Vertices = new List<Vector3>();
        Triangles = new List<int>();
    }

    public void AddTriangle(int a, int b, int c)
    {
        Triangles.Add(Vertices.Count + a);
        Triangles.Add(Vertices.Count + b);
        Triangles.Add(Vertices.Count + c);

        Triangles.Add(Vertices.Count + a);
        Triangles.Add(Vertices.Count + c);
        Triangles.Add(Vertices.Count + b);
    }
}