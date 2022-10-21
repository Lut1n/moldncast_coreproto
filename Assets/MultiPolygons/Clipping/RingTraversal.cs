using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToVisitCache
{
    public struct Item
    {
        public Vector2Int setpath;
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
        toVisit.RemoveAt(0);
        return ret;
    }

    public void AddPointToVisit(int set, int path, Vector2Int node)
    {
        Item item; item.setpath = new Vector2Int(set, path); item.node =node;
        if (!toVisit.Contains(item))
            toVisit.Add(item);
    }

    public void Remove(int set, int path, Vector2Int node)
    {
        Item item; item.setpath = new Vector2Int(set, path); item.node = node;
        if (toVisit.Contains(item))
            toVisit.Remove(item);
    }
}

public class RingTraversal
{
    public delegate bool PathFilterHandler(LinearRing2i path);

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
                        if (GeometryMath.SegToSeg(path1.At(n1), path1.At(n1 + 1), path2.At(n2), path2.At(n2 + 1), ref ipt) == ISeg.Yes)
                        {
                            ret.Add(new SegmentPair(pathIdx1, n1, pathIdx2, n2), ipt);
                        }
                    }
                }
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

    static public LinearRing2i PathsTraversal(MultiPolygon2i set1, MultiPolygon2i set2, int startSet, int startPath, Vector2Int node, IPointCache cache, DebugReport report, ToVisitCache toVisit)
    {
        MultiPolygon2i[] sets = new MultiPolygon2i[2] { set1, set2 };

        int startSeg = sets[startSet].Get(startPath).nodes.IndexOf(node);

        int currentSet = startSet;
        int currentPath = startPath;
        int currentSeg = startSeg;

        LinearRing2i ret = new LinearRing2i();

        int fuse = 1000;

        do
        {
            var p0 = sets[currentSet].Get(currentPath).At(currentSeg);
            ret.Add(p0);
            toVisit.Remove(currentSet, currentPath, p0);

            if (cache.Contains(p0))
            {
                SegmentPair pair = cache.GetPair(p0);
                if (currentSet == 0)
                {
                    currentSet = 1;
                    currentPath = pair.segRef2.x;
                }
                else if (currentSet == 1)
                {
                    currentSet = 0;
                    currentPath = pair.segRef1.x;
                }
                currentSeg = sets[currentSet].Get(currentPath).nodes.IndexOf(p0);
            }

            currentSeg++;
            if (currentSeg == sets[currentSet].Get(currentPath).Count())
                currentSeg = 0;

            if (fuse-- == 0)
            {
                Debug.LogError("Infinite loop on path traversal");
                report.bug = true;
                break;
            }
        }
        while (!(currentSet == startSet && currentPath == startPath && currentSeg == startSeg));

        report.traversals.Add(new LinearRing2i(ret));
        return ret;
    }

    static public ToVisitCache CreateToVisitCache(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        ToVisitCache toVisit = new ToVisitCache();
        for (int pathIdx = 0; pathIdx < set1.Count(); pathIdx++)
        {
            LinearRing2i path = set1.Get(pathIdx);
            foreach(var n in path.nodes)
                toVisit.AddPointToVisit(0, pathIdx, n);
        }
        for (int pathIdx = 0; pathIdx < set2.Count(); pathIdx++)
        {
            LinearRing2i path = set2.Get(pathIdx);
            foreach(var n in path.nodes)
                toVisit.AddPointToVisit(1, pathIdx, n);
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
