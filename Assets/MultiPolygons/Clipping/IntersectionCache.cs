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
    public SegmentPair(Vector2Int indexes1, Vector2Int indexes2)
    {
        segRef1 = indexes1;
        segRef2 = indexes2;
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
            if (!points.Contains(s.Value)) points.Add(s.Value);
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

[System.Serializable]
public class PointInfo
{
    public Vector2Int value;
    public Vector2Int indexes1 = new Vector2Int(-1,-1); // path and node indexes on polygon 1
    public Vector2Int indexes2 = new Vector2Int(-1,-1); // path and node indexes on polygon 2
    public List<int> inRings = new List<int>();
    public List<int> outRings = new List<int>();
    public List<int> edgeRings = new List<int>();

    public PointInfo(Vector2Int value)
    {
        this.value = value;
    }

    public void Update(int polygonId, int pathId, int nodeId)
    {
        if (polygonId == 0)
            this.indexes1 = new Vector2Int(pathId, nodeId);
        else
            this.indexes2 = new Vector2Int(pathId, nodeId);
    }
}

[System.Serializable]
public class PointsInfo
{
    // give indexes and side of all points for a polygon
    public List<PointInfo> infos = new List<PointInfo>();

    // give polygon id and ring indexes
    public List<Vector2Int> rings = new List<Vector2Int>();

    public void Reset()
    {
        infos = new List<PointInfo>();
    }
    
    public void Add(Vector2Int p, int polygonId, int pathId, int nodeId)
    {
        if (Index(p) == -1) infos.Add(new PointInfo(p));
        Get(p).Update(polygonId, pathId, nodeId);
    }

    public void AddRing(int polygonId, int pathId)
    {
        var indexes = new Vector2Int(polygonId, pathId);
        if (RingIndex(indexes) == -1) rings.Add(indexes);
    }
    
    public int RingIndex(Vector2Int indexes)
    {
        return rings.IndexOf(indexes);
    }

    public int Index(Vector2Int p)
    {
        for(int i=0; i<infos.Count; ++i)
        {
            if (infos[i].value == p)
                return i;
        }
        return -1;
    }

    public PointInfo Get(int i)
    {
        return infos[i];
    }

    public PointInfo Get(Vector2Int p)
    {
        int id = Index(p);
        Debug.Assert(id != -1);
        return infos[id];
    }
    

    public Vector2Int GetRing(int i)
    {
        return rings[i];
    }
}