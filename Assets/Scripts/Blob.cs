using UnityEngine;
using System;

public class Blob : MonoBehaviour
{
	public Func<float,float> WeightFunction { get; set; }

    public Blob()
    {
        WeightFunction = x => 8 - 2 * x;
    }
}
