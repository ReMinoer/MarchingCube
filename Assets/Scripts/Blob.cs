using UnityEngine;
using System;

public class Blob : MonoBehaviour
{
	public Func<float,float> WeightFunction { get; set; }
    private Vector3 _lastPosition;

    public bool Dirty { get; private set; }

    public Blob()
    {
        //WeightFunction = x => -Mathf.Log10(x/10);
        WeightFunction = x => -x + 3;
    }

    void Update()
    {
        Dirty = _lastPosition != transform.position;
        _lastPosition = transform.position;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
