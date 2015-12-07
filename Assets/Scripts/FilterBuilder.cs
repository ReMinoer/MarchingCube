using System;
using System.Collections.Generic;
using UnityEngine;

public interface IFilterBuilder
{
    Stack<NeighborhoodRule> GenerateRulesStack();
    void Draw(MeshData meshData, Vector3 cubeOrigin, int[] solution, bool reverseNormal);
}

public struct NeighborhoodRule
{
    public int PointId { get; set; }
    public MustBe MustBe { get; set; }
}

public struct TriangleDescriptor
{
    public int P0A { get; set; }
    public int P0B { get; set; }
    public int P1A { get; set; }
    public int P1B { get; set; }
    public int P2A { get; set; }
    public int P2B { get; set; }
}

public enum MustBe
{
    Equal,
    NotEqual
}

public abstract class FilterBuilder : IFilterBuilder
{
    private readonly List<NeighborhoodRule> _rules;
    private readonly List<TriangleDescriptor> _trianglesDescriptors;

    protected FilterBuilder()
    {
        _rules = new List<NeighborhoodRule>();
        _trianglesDescriptors = new List<TriangleDescriptor>();
    }

    protected int NeighborOf(int pointId, MustBe mustBe)
    {
        var rule = new NeighborhoodRule {
            PointId = pointId,
            MustBe = mustBe
        };
        _rules.Add(rule);

        return _rules.Count;
    }

    protected void AddTriangle(int p0A, int p0B, int p1A, int p1B, int p2A, int p2B)
    {
        var triangle = new TriangleDescriptor {
            P0A = p0A,
            P0B = p0B,
            P1A = p1A,
            P1B = p1B,
            P2A = p2A,
            P2B = p2B
        };

        _trianglesDescriptors.Add(triangle);
    }

    protected void AddQuad(int a0, int a1, int b0, int b1, int c0, int c1, int d0, int d1)
    {
        AddTriangle(a0, a1, b0, b1, c0, c1);
        AddTriangle(a0, a1, c0, c1, d0, d1);
    }

    public Stack<NeighborhoodRule> GenerateRulesStack()
    {
        _rules.Reverse();
        var stack = new Stack<NeighborhoodRule>(_rules);
        _rules.Reverse();

        return stack;
    }

    public void Draw(MeshData meshData, Vector3 cubeOrigin, int[] solution, bool reverseNormal)
    {
        var edges = new List<int>();
        foreach (TriangleDescriptor tri in _trianglesDescriptors)
        {
            int e0 = LookAtTable.Edges[solution[tri.P0A], solution[tri.P0B]];
            int e1 = LookAtTable.Edges[solution[tri.P1A], solution[tri.P1B]];
            int e2 = LookAtTable.Edges[solution[tri.P2A], solution[tri.P2B]];

            if (!edges.Contains(e0))
                edges.Add(e0);
            if (!edges.Contains(e1))
                edges.Add(e1);
            if (!edges.Contains(e2))
                edges.Add(e2);

            if (e0 == -1 || e1 == -1 || e2 == -1)
                throw new IndexOutOfRangeException();

            if (reverseNormal)
                meshData.AddTriangle(edges.IndexOf(e0), edges.IndexOf(e2), edges.IndexOf(e1));
            else
                meshData.AddTriangle(edges.IndexOf(e0), edges.IndexOf(e1), edges.IndexOf(e2));
        }

        foreach (var edge in edges)
        {
            try
            {
                meshData.Vertices.Add(cubeOrigin + LookAtTable.EdgeCenters[edge]);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}