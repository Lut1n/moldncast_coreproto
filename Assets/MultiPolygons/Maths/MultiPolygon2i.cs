using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MultiPolygon2i : MultiPolygon<Vector2Int>
{
    public bool Composes(Vector2Int node)
    {
        foreach(var boundary in boundaries)
            if (boundary.Composes(node))
                return true;
        return false;
    }

    public bool Composes(LinearRing2i boundary)
    {
        if (boundaries.Contains(boundary))
            return true;

        foreach (LinearRing2i b in boundaries)
            if (b.Equivalents(boundary))
                return true;

        return false;
    }

    public MultiPolygon2i Reverse(LinearRing2i dom = null)
    {
        MultiPolygon2i ret = new MultiPolygon2i();
        foreach(var b in boundaries)
            ret.Add(((LinearRing2i)b).Reverse());
        
        if (dom != null)
        {
            Debug.Assert(dom.orientation == RingOrientation.Positive);
            ret.Add(dom);
        }

        return ret;
    }

    public new LinearRing2i Get(int index)
    {
        return (LinearRing2i)(boundaries[index]);
    }
}