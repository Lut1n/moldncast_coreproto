using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Define the orientation of a ring
public enum RingOrientation
{
    Unknown,
    Positive,   // CCW
    Negative    // CW
}

public enum Side
{
    None,
    In,
    Out,
    Cross,
    Edge
}

public class LinearRing<T>
{
    public delegate void SegmentHandler(T a, T b);

    public List<T> nodes;
    public RingOrientation orientation;

    public LinearRing()
    {
        nodes = new List<T>();
        orientation = RingOrientation.Unknown;
    }

    public LinearRing(List<T> nodes, RingOrientation orientation)
    {
        this.nodes = new List<T>(nodes);
        this.orientation = orientation;
    }
    
    public LinearRing(LinearRing<T> other) : this(other.nodes, other.orientation)
    {
    }
    
    public bool IsCCW()
    {
        Debug.Assert(orientation != RingOrientation.Unknown);
        return orientation == RingOrientation.Positive;
    }

    public bool IsCW()
    {
        Debug.Assert(orientation != RingOrientation.Unknown);
        return orientation == RingOrientation.Negative;
    }

    public void Add(T node)
    {
        nodes.Add(node);
        orientation = RingOrientation.Unknown;
    }

    public T At(int i)
    {
        while (i >= nodes.Count)
            i -= nodes.Count;
        while (i < 0)
            i += nodes.Count;
        return nodes[i];
    }

    public void ForEachSegment(SegmentHandler handler)
    {
        if (nodes.Count < 3)
            return;

        T prev = nodes[nodes.Count - 1];
        for(int i=0; i<nodes.Count; ++i)
        {
            handler(prev, nodes[i]);
            prev = nodes[i];
        }
    }

    public LinearRing<T> Reverse()
    {
        Debug.Assert(orientation != RingOrientation.Unknown);
        LinearRing<T> ret = new LinearRing<T>();
        ret.nodes = new List<T>(nodes);
        ret.nodes.Reverse();
        ret.orientation = orientation == RingOrientation.Positive ? RingOrientation.Negative : RingOrientation.Positive;
        return ret;
    }
    
    public bool Composes(T node)
    {
        return nodes.Contains(node);
    }

    public int Count()
    {
        return nodes.Count;
    }
}
