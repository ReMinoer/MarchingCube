using UnityEngine;

static public class LookAtTable
{
    /*  Cube :
        * 
        *     3----7
        *   ./|   /|
        *   2----6 |
        *   | 1--|-5
        *   |/   |/
        *   0----4
        */

    static public readonly int[][] Neighborhood;
    static public readonly int[,] Edges;
    static public readonly Vector3[] EdgeCenters;

    static LookAtTable()
    {
        Neighborhood = new int[8][];

        Neighborhood[0] = new[] { 1, 2, 4 };
        Neighborhood[1] = new[] { 0, 3, 5 };
        Neighborhood[2] = new[] { 3, 0, 6 };
        Neighborhood[3] = new[] { 2, 1, 7 };
        Neighborhood[4] = new[] { 5, 6, 0 };
        Neighborhood[5] = new[] { 4, 7, 1 };
        Neighborhood[6] = new[] { 7, 4, 2 };
        Neighborhood[7] = new[] { 6, 5, 3 };

        Edges = new int[8, 8];
        for (int i = 0; i < Edges.Length; i++)
            Edges[i / 8, i % 8] = -1;

        Edges[0, 1] = 0;
        Edges[0, 2] = 1;
        Edges[3, 1] = 2;
        Edges[3, 2] = 3;
        Edges[4, 5] = 4;
        Edges[4, 6] = 5;
        Edges[7, 5] = 6;
        Edges[7, 6] = 7;
        Edges[0, 4] = 8;
        Edges[1, 5] = 9;
        Edges[2, 6] = 10;
        Edges[3, 7] = 11;

        Edges[1, 0] = 0;
        Edges[2, 0] = 1;
        Edges[1, 3] = 2;
        Edges[2, 3] = 3;
        Edges[5, 4] = 4;
        Edges[6, 4] = 5;
        Edges[5, 7] = 6;
        Edges[6, 7] = 7;
        Edges[4, 0] = 8;
        Edges[5, 1] = 9;
        Edges[6, 2] = 10;
        Edges[7, 3] = 11;

        EdgeCenters = new Vector3[12];

        EdgeCenters[0] = new Vector3(0.0f, 0.0f, 0.5f);
        EdgeCenters[1] = new Vector3(0.0f, 0.5f, 0.0f);
        EdgeCenters[2] = new Vector3(0.0f, 0.5f, 1.0f);
        EdgeCenters[3] = new Vector3(0.0f, 1.0f, 0.5f);
        EdgeCenters[4] = new Vector3(1.0f, 0.0f, 0.5f);
        EdgeCenters[5] = new Vector3(1.0f, 0.5f, 0.0f);
        EdgeCenters[6] = new Vector3(1.0f, 0.5f, 1.0f);
        EdgeCenters[7] = new Vector3(1.0f, 1.0f, 0.5f);
        EdgeCenters[8] = new Vector3(0.5f, 0.0f, 0.0f);
        EdgeCenters[9] = new Vector3(0.5f, 0.0f, 1.0f);
        EdgeCenters[10] = new Vector3(0.5f, 1.0f, 0.0f);
        EdgeCenters[11] = new Vector3(0.5f, 1.0f, 1.0f);
    }
}