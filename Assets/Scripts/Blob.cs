using UnityEngine;
using System;
using System.Collections;

public class Blob : MonoBehaviour
{
	public Func<float,float> WeightFunction { get; set; }

    public Blob()
    {
        WeightFunction = x => -Mathf.Log10(x);
    }
}
