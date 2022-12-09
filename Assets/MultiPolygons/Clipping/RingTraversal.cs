using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Segment
{
    public Vector2Int p1;
    public Vector2Int p2;

    public Segment(Vector2Int p1, Vector2Int p2)
    {
        this.p1 = p1;
        this.p2 = p2;
    }
}

public class ToVisitCache
{
    public struct Item
    {
        public Vector3Int indexes;
        public Vector2Int node;
    }

    List<Item> toVisit = new List<Item>();

    public bool Empty()
    {
        return toVisit.Count == 0;
    }

    public Item PickToVisit()
    {
        Debug.Assert(!Empty(), "Error: cannot pick in an empty list");

        var ret = toVisit[0];
        RemoveAll(ret.node);
        return ret;
    }

    public void AddPointToVisit(int set, int path, int node, Vector2Int value)
    {
        Item item;
        item.indexes = new Vector3Int(set, path, node);
        item.node = value;
        if (!toVisit.Contains(item))
            toVisit.Add(item);
    }

    public void RemoveAll(Vector2Int node)
    {
        List<Item> toVisit2 = new List<Item>();
        foreach(var item in toVisit)
        {
            if (item.node != node)
                toVisit2.Add(item);
        }
        var removed = (toVisit.Count - toVisit2.Count);
        // Debug.Log("removed " + removed + " " + node + " - remaining " + toVisit2.Count);
        toVisit = toVisit2;
    }
}

public class RingTraversal
{
    public delegate bool PathFilterHandler(LinearRing2i path);

    static public ISeg SegToSeg(Vector2Int a1, Vector2Int a2, Vector2Int b1, Vector2Int b2, ref Vector2Int ipt)
    {
        // 3 cases have to be tested:
        // (1) - intersection of segments (lines). Returns Yes
        // (2) - if points are same. Returns Edge
        // (3) - if a point is on a line. Return Edge

        var res = GeometryMath.SegToSegi(a1, a2, b1, b2, ref ipt);
        if (res == ISeg.No)
        {
            float ignored = 0.0f;
            // (1) and (2) are false. Let's test (3)
            if (VecIntOperation.ComputePointToLine(a1, b1, b2, 0.5f, 0.5f, ref ignored) == VecIntOperation.Result.OnTheLine)
            {
                ipt = a1;
                return ISeg.Edge;
            }
            if (VecIntOperation.ComputePointToLine(a2, b1, b2, 0.5f, 0.5f, ref ignored) == VecIntOperation.Result.OnTheLine)
            {
                ipt = a2;
                return ISeg.Edge;
            }
            if (VecIntOperation.ComputePointToLine(b1, a1, a2, 0.5f, 0.5f, ref ignored) == VecIntOperation.Result.OnTheLine)
            {
                ipt = b1;
                return ISeg.Edge;
            }
            if (VecIntOperation.ComputePointToLine(b2, a1, a2, 0.5f, 0.5f, ref ignored) == VecIntOperation.Result.OnTheLine)
            {
                ipt = b2;
                return ISeg.Edge;
            }
        }
        return res;
    }

    static public IPointCache ComputeIPoints(MultiPolygon2i polygons1, MultiPolygon2i polygons2)
    {
        IPointCache ret = new IPointCache();
        Vector2Int ipt = new Vector2Int();

        for (int pathIdx1 = 0; pathIdx1 < polygons1.Count(); pathIdx1++)
        {
            LinearRing2i path1 = polygons1.Get(pathIdx1);
            for(int n1=0; n1<path1.Count(); ++n1)
            {
                for (int pathIdx2 = 0; pathIdx2 < polygons2.Count(); pathIdx2++)
                {
                    LinearRing2i path2 = polygons2.Get(pathIdx2);
                    if (path1.Equivalents(path2))
                        continue;
                    for(int n2=0; n2<path2.Count(); ++n2)
                    {
                        if (SegToSeg(path1.At(n1), path1.At(n1 + 1), path2.At(n2), path2.At(n2 + 1), ref ipt) != ISeg.No)
                        {
                            int refn1 = n1;
                            int refn2 = n2;

                            if (ipt == path1.At(refn1 + 1))
                                refn1 = path1.LoopIdx(refn1 + 1);
                            if (ipt == path2.At(refn2 + 1))
                                refn2 = path1.LoopIdx(refn2 + 1);
                            ret.Add(new SegmentPair(pathIdx1, refn1, pathIdx2, refn2), ipt);
                        }
                    }
                }
            }
        }
        return ret;
    }

    static public void RegisterPoints(MultiPolygon2i polygons, int polygonId, PointsInfo info)
    {
        for (int pathId = 0; pathId < polygons.Count(); pathId++)
        {
            LinearRing2i path = polygons.Get(pathId);
            info.AddRing(polygonId, pathId);
            for(int n1=0; n1<path.Count(); ++n1)
            {
                info.Add(path.At(n1), polygonId, pathId, n1);
            }
        }
    }

    static public PointsInfo ComputeInfos(MultiPolygon2i polygons1, MultiPolygon2i polygons2)
    {
        PointsInfo ret = new PointsInfo();
        RegisterPoints(polygons1, 0, ret);
        RegisterPoints(polygons2, 1, ret);

        foreach(var info in ret.infos)
        {
            for(int ringid = 0; ringid < ret.rings.Count; ringid++)
            {
                var ring = ret.rings[ringid];
                LinearRing2i path;
                if (ring.x == 0)
                    path = polygons1.Get(ring.y);
                else
                    path = polygons2.Get(ring.y);
                Side side = MultiPolygon2iOperation.GetSide(path, info.value);
                if (side == Side.In)
                    info.inRings.Add(ringid);
                if (side == Side.Out)
                    info.outRings.Add(ringid);
                if (side == Side.Edge)
                    info.edgeRings.Add(ringid);
            }
        }

        return ret;
    }

    static public MultiPolygon2i SubdivideSegments(MultiPolygon2i polygons, IPointCache cache, bool firstPolys = true)
    {
        MultiPolygon2i ret = new MultiPolygon2i();

        for (int pathIdx = 0; pathIdx < polygons.Count(); pathIdx++)
        {
            LinearRing2i path = polygons.Get(pathIdx);
            LinearRing2i newPath = new LinearRing2i();
            for(int n = 0; n<path.Count(); ++n)
            {
                List<Vector2Int> toInsert = firstPolys ? cache.GetIPointPolygon1(pathIdx, n) : cache.GetIPointPolygon2(pathIdx, n);
                Vector2Int p0 = path.At(n);
                Vector2Int p3 = path.At(n + 1);
                toInsert.Sort((p1, p2) => {
                    return Vector2.Distance(p0, p1) < Vector2.Distance(p0, p2) ? -1 : 1;
                });
                newPath.Add(p0);
                foreach (var p in toInsert)
                    if (p != p0 && p != p3)
                        newPath.Add(p);
            }
            newPath.orientation = path.orientation;
            ret.Add(newPath);
        }

        return ret;
    }

    static public Dictionary<Vector2Int, List<Vector3Int>> GetConnectivities(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        // list of tuple(polyId, pathId, nodeId) per point
        Dictionary<Vector2Int, List<Vector3Int>> ret = new Dictionary<Vector2Int, List<Vector3Int>>();

        var sets = new MultiPolygon2i[] {set1, set2};
        for(int k=0; k<2; ++k)
        {
            for(int i=0; i<sets[k].Count(); ++i)
            {
                var ring = sets[k].Get(i);
                for(int j=0; j<ring.Count(); ++j)
                {
                    var node = ring.At(j);
                    if (!ret.ContainsKey(node))
                        ret.Add(node, new List<Vector3Int>());
                    var indexes = new Vector3Int(k, i, j);
                    if (!ret[node].Contains(indexes))
                        ret[node].Add(indexes);
                }
            }
        }

        return ret;
    }

    static public int Index(List<Vector2Int> ls, Vector2Int p)
    {
        for(int i=0; i<ls.Count; ++i)
        {
            if (ls[i] == p)
                return i;
        }
        return -1;
    }

    static public Vector2Int GetPoint(MultiPolygon2i[] sets, Vector3Int indexes, int oft = 0)
    {
        return sets[indexes[0]].Get(indexes[1]).At(indexes[2] + oft);
    }

    static public Vector3Int NextIndex(MultiPolygon2i[] sets, Vector3Int indexes)
    {
        var ret = indexes;
        ret[2] = sets[ret[0]].Get(ret[1]).LoopIdx(ret[2] + 1);
        return ret;
    }

    static public void PathsTraversal2(PointsInfo info, MultiPolygon2i set1, MultiPolygon2i set2, Vector3Int startIndexes, IPointCache cache, DebugReport report, ToVisitCache toVisit, bool outDir, MultiPolygon2i outResult, List<Segment> visited)
    {
        MultiPolygon2i[] sets = new MultiPolygon2i[2] { set1, set2 };

        // [todo] should be global (computed only one time for all)
        Dictionary<Vector2Int, List<Vector3Int>> connectivities = GetConnectivities(set1, set2);

        Vector3Int current = startIndexes;

        List<Vector2Int> stack = new List<Vector2Int>();

        var p0 = GetPoint(sets, current, -1);
        var p = GetPoint(sets, current);

        RingInt traversal = new RingInt();

        int debugit = 0;
        int debugadd = 0;

        bool foundNext = true;

        do
        {
            if (connectivities[p].Count > 1)
            {
                // compute angles for all potential next nodes
                List<float> angles = new List<float>();
                for(int i=0; i<connectivities[p].Count; ++i)
                {
                    var indexes = connectivities[p][i];
                    var p1 = GetPoint(sets, indexes, 1);
                    angles.Add(VecIntOperation.CurveAngle(p0, p, p1));
                }

                // find the best next node considering the orientation (out == Union == min, in == Intersection == max)
                int index = -1;
                for(int a=0; a<angles.Count; ++a)
                {
                    var indexes = connectivities[p][a];
                    var panext = GetPoint(sets, indexes, 1);
                    bool alreadyVisited2 = visited.Contains(new Segment(p, panext));
                    if (!alreadyVisited2 && panext != p0)
                    {
                        if (index == -1) index = a;
                        if ((outDir && angles[a] < angles[index]) || (!outDir && angles[a] > angles[index]))
                            index = a;
                    }
                }

                if (index != -1)
                {    
                    // update state
                    current = connectivities[p][index];
                }
            }

            if (stack.Contains(p))
            {
                // build ring and add it the the result
                LinearRing2i ret = new LinearRing2i();
                int firstIndex = Index(stack, p);
                for(int i=firstIndex; i<stack.Count; ++i)
                    ret.Add(stack[i]);

                // stack pop
                for(int i=0; i<ret.Count(); ++i)
                    stack.RemoveAt(stack.Count-1);
                
                ret.ComputeOrientation();
                debugadd++;

                if (ret.Count() > 2)
                    outResult.Add(new LinearRing2i(ret));
            }

            var nextIdx = NextIndex(sets, current);
            var next = GetPoint(sets, nextIdx);

            var seg = new Segment(p, next);
            foundNext = !visited.Contains(seg);
            if (foundNext)
            {
                // update state
                stack.Add(p);
                visited.Add(seg);
                toVisit.RemoveAll(p);
                traversal.nodes.Add(info.Index(p));

                // advance
                p0 = p;
                p = next;
                current = nextIdx;
            }

            debugit++;

            if (debugit > 1000)
            {
                Debug.Log("break infinite loop");
                break;
            }
        }
        while (foundNext);
        
        report.traversals.Add(traversal);
    }

    static public ToVisitCache CreateToVisitCache(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        ToVisitCache toVisit = new ToVisitCache();
        for (int pathIdx = 0; pathIdx < set1.Count(); pathIdx++)
        {
            LinearRing2i path = set1.Get(pathIdx);
            for(int n = 0; n < path.nodes.Count; ++n)
                toVisit.AddPointToVisit(0, pathIdx, n, path.nodes[n]);
        }
        for (int pathIdx = 0; pathIdx < set2.Count(); pathIdx++)
        {
            LinearRing2i path = set2.Get(pathIdx);
            for(int n = 0; n < path.nodes.Count; ++n)
                toVisit.AddPointToVisit(1, pathIdx, n, path.nodes[n]);
        }
        return toVisit;
    }

    static public void FilterPaths(MultiPolygon2i set, PathFilterHandler handler)
    {
        List<LinearRing2i> toRemove = new List<LinearRing2i>();
        set.ForEachBoundary(path => { if (handler((LinearRing2i)path)) toRemove.Add((LinearRing2i)path); });
        foreach (var path in toRemove) set.Remove(path);
    }
}
