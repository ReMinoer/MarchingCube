using UnityEngine;
using System.Collections.Generic;

public class MarchingSquareRenderer : MonoBehaviour
{
    public struct WeightGrid
    {
        public bool[][] Vertices { get; set; }
        public Grid2D Grid { get; set; }
    }

    public GameObject BlobsRootNode;
    public float Threshold = 10f;

    public WeightGrid ProcessWeight(IEnumerable<Blob> blobs, Grid2D grid)
    {
        var weightGrid = new WeightGrid
        {
            Grid = grid
        };

        int sizeX = (int)(grid.Bounds.width / grid.Step);
        int sizeY = (int)(grid.Bounds.height / grid.Step);

        weightGrid.Vertices = new bool[sizeY][];
        for (int i = 0; i < sizeY; i++)
            weightGrid.Vertices[i] = new bool[sizeX];

        for (int i = 0; i < sizeY; i++)
            for (int j = 0; j < sizeX; j++)
            {
                float y = grid.Bounds.y + i * grid.Step;
                float x = grid.Bounds.x + j * grid.Step;
                Vector2 pos = new Vector2(x, y);

                float weigth = 0;
                foreach (Blob blob in blobs)
                {
                    float distance = Vector2.Distance(pos, blob.transform.position);
                    weigth += blob.WeightFunction(distance);
                }

                weightGrid.Vertices[i][j] = weigth > Threshold;
            }

        return weightGrid;
    }
}
