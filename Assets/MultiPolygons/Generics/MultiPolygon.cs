using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiPolygon<T>
{
    public delegate void BoundaryHandler(LinearRing<T> boundary);

    public List<LinearRing<T>> boundaries;

    public MultiPolygon()
    {
        boundaries = new List<LinearRing<T>>();
    }

    public MultiPolygon(List<LinearRing<T>> boundaries)
    {
        this.boundaries = boundaries;
    }

    public int Count()
    {
        return boundaries.Count;
    }

    public void Add(LinearRing<T> boundary)
    {
        Debug.Assert(boundary.orientation != RingOrientation.Unknown);
        boundaries.Add(boundary);
    }

    public void Remove(LinearRing<T> boundary)
    {
        boundaries.Remove(boundary);
    }
    
    public void ForEachBoundary(BoundaryHandler handler)
    {
        foreach(var boundary in boundaries)
            handler(boundary);
    }

    public LinearRing<T> Get(int index)
    {
        return boundaries[index];
    }
}
