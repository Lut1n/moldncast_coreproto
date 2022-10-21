using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SegmentPair
{
    public Vector2Int segRef1 = new Vector2Int();
    public Vector2Int segRef2 = new Vector2Int();

    public SegmentPair()
    {
    }
    public SegmentPair(int p1, int s1, int p2, int s2)
    {
        segRef1 = new Vector2Int(p1, s1);
        segRef2 = new Vector2Int(p2, s2);
    }
    public override bool Equals(object obj) => this.Equals(obj as SegmentPair);
    public bool Equals(SegmentPair other)
    {
        if (other is null)
            return false;
        return (segRef1 == other.segRef1 && segRef2 == other.segRef2);
            // || (segRef1 == other.segRef2 && segRef2 == other.segRef1);
    }
    public override int GetHashCode() => (segRef1, segRef2).GetHashCode();
    public static bool operator ==(SegmentPair sp1, SegmentPair sp2)
    {
        if (sp1 is null)
            return sp2 is null;
        return sp1.Equals(sp2);
    }
    public static bool operator !=(SegmentPair sp1, SegmentPair sp2) => !(sp1 == sp2);
}

[System.Serializable]
public class IPointCache
{
    public Dictionary<SegmentPair, Vector2Int> ipoints2 = new Dictionary<SegmentPair, Vector2Int>();

    public List<Vector2Int> GetPoints()
    {
        List<Vector2Int> points = new List<Vector2Int>();
        foreach (var s in ipoints2)
            points.Add(s.Value);
        return points;
    }

    public Vector2Int Add(SegmentPair pair, Vector2Int p)
    {
        if (!ipoints2.ContainsKey(pair))
            ipoints2.Add(pair, p);
        return ipoints2[pair];
    }
    public bool Contains(SegmentPair pair)
    {
        return ipoints2.ContainsKey(pair);
    }

    public bool Contains(Vector2Int p)
    {
        return ipoints2.ContainsValue(p);
    }

    public Vector2Int GetPoint(SegmentPair pair)
    {
        return ipoints2[pair];
    }

    public SegmentPair GetPair(Vector2Int p)
    {
        foreach (var item in ipoints2)
        {
            if (item.Value.Equals(p))
                return item.Key;
        }
        return null;
    }

    public List<Vector2Int> GetIPointPolygon1(int p1, int s1)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        foreach(var item in ipoints2)
        {
            Vector2 ref1 = item.Key.segRef1;
            if (ref1 == new Vector2(p1,s1))
                ret.Add(item.Value);
        }
        return ret;
    }
    public List<Vector2Int> GetIPointPolygon2(int p2, int s2)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        foreach (var item in ipoints2)
        {
            Vector2 ref2 = item.Key.segRef2;
            if (ref2 == new Vector2(p2, s2))
                ret.Add(item.Value);
        }
        return ret;
    }
}