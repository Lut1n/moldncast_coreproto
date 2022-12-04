using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LinearRing2i : LinearRing<Vector2Int>
{
    // public Vector2 insidePoint;
    // public Rect bounds;
    
    public LinearRing2i() : base()
    {
    }

    public LinearRing2i(List<Vector2Int> path) : base(path, RingOrientation.Unknown)
    {
        ComputeOrientation();
    }
    
    public LinearRing2i(LinearRing<Vector2Int> other) : base(other.nodes, other.orientation)
    {
    }
    

    public new LinearRing2i Reverse()
    {
        Debug.Assert(orientation != RingOrientation.Unknown);
        LinearRing2i ret = new LinearRing2i();
        ret.nodes = new List<Vector2Int>(nodes);
        ret.nodes.Reverse();
        ret.orientation = orientation == RingOrientation.Positive ? RingOrientation.Negative : RingOrientation.Positive;
        return ret;
    }

    public RingOrientation ComputeOrientation()
    {
        if (nodes.Count < 3)
        {
            orientation = RingOrientation.Unknown;
            return orientation;
        }

        // find smallest x/y node (garanteed to be a vertex of the convex hull of the ring)
        int index = 0;
        
        for(int i=0; i<nodes.Count; ++i)
        {
            if (nodes[i].x < nodes[index].x || (nodes[i].x == nodes[index].x && nodes[i].y < nodes[index].y))
                index = i;
        }

        // compute cross product
        Vector2 curr = At(index);
        Vector2 prev = At(index - 1);
        Vector2 next = At(index + 1);
        float result = Vector3.Cross(next - curr, prev - curr).z;

        orientation = result > 0.0f ? RingOrientation.Positive : RingOrientation.Negative;
        return orientation;
    }
    
    public bool Equivalents(LinearRing2i other)
    {
        if (nodes.Count == 0)
            return other.nodes.Count == 0;

        int idx2 = other.nodes.IndexOf(nodes[0]);
        if (idx2 == -1)
            return false;

        for (int i = 0; i < nodes.Count; ++i)
            if (nodes[i] != other.At(idx2 + i))
                return false;

        return true;
    }
}
