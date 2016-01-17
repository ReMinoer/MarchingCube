using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PointState
{
    public int PointIndex { get; set; }
    public bool Value { get; set; }
}

public class LocalCube : IEnumerable<PointState>
{
    private readonly bool[][][] _grid;
    private readonly int _i;
    private readonly int _j;
    private readonly int _k;

    private readonly List<PointState> _pointStates;
    private readonly List<int> _ingoredPoints;

    public LocalCube(bool[][][] grid, int i, int j, int k)
    {
        _grid = grid;
        _i = i;
        _j = j;
        _k = k;
        _pointStates = new List<PointState>();
        _ingoredPoints = new List<int>();

        Add(0, grid[i][j][k]);
        Add(1, grid[i][j][k + 1]);
        Add(2, grid[i][j + 1][k]);
        Add(3, grid[i][j + 1][k + 1]);
        Add(4, grid[i + 1][j][k]);
        Add(5, grid[i + 1][j][k + 1]);
        Add(6, grid[i + 1][j + 1][k]);
        Add(7, grid[i + 1][j + 1][k + 1]);

        _pointStates.Sort((x, y) => y.PointIndex.CompareTo(x.PointIndex));
    }

    public bool this[int pointIndex]
    {
        get { return _grid[_i + pointIndex / 4][_j + (pointIndex / 2) % 2][_k + pointIndex % 2]; }
    }

    public bool ApplyFilter(IFilterBuilder filter, out IEnumerable<int> result, out bool state)
    {
        Stack<NeighborhoodRule> rules = filter.GenerateRulesStack();

        var resultQueue = new Stack<int>();
        foreach (PointState pointState in this.ToArray())
        {
            resultQueue.Push(pointState.PointIndex);
            Ignore(pointState.PointIndex);

            if (ApplyFilter(rules, resultQueue))
            {
                result = resultQueue;
                state = pointState.Value;
                return true;
            }

            StopIgnoring(pointState.PointIndex);
            resultQueue.Pop();
        }

        result = null;
        state = false;
        return false;
    }

    public void Ignore(int pointIndex)
    {
        _ingoredPoints.Add(pointIndex);
    }

    public void StopIgnoring(int pointIndex)
    {
        _ingoredPoints.Remove(pointIndex);
    }

    public IEnumerator<PointState> GetEnumerator()
    {
        return _pointStates.Where(x => !_ingoredPoints.Contains(x.PointIndex)).GetEnumerator();
    }

    private void Add(int pointIndex, bool value)
    {
        _pointStates.Add(new PointState
        {
            PointIndex = pointIndex,
            Value = value
        });
    }

    private bool ApplyFilter(Stack<NeighborhoodRule> rules, Stack<int> result)
    {
        if (!rules.Any())
            return true;

        NeighborhoodRule rule = rules.Pop();

        int resultPoint = result.ElementAt(result.Count - rule.PointId - 1);
        int[] neighbors = FilterBasedLookAtTable.Neighborhood[resultPoint];
        IEnumerable<PointState> localNeighbors = this.Where(x => neighbors.Contains(x.PointIndex)).ToArray();

        foreach (PointState point in localNeighbors)
        {
            if ((rule.MustBe == MustBe.Equal && point.Value == this[rule.PointId])
                || (rule.MustBe == MustBe.NotEqual && point.Value != this[rule.PointId]))
            {
                result.Push(point.PointIndex);
                Ignore(point.PointIndex);

                if (ApplyFilter(rules, result))
                    return true;

                StopIgnoring(point.PointIndex);
                result.Pop();
            }
        }

        rules.Push(rule);
        return false;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}