using UnityEngine;
using System;

public class Blob : MonoBehaviour
{
	public Func<float,float> WeightFunction { get; set; }

    public Blob()
    {
        WeightFunction = x => -Mathf.Log10(x/10);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
