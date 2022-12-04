using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RingInt
{
    public List<int> nodes = new List<int>();
}

public class CSGOperation
{
    IPointCache cache;
    MultiPolygon2i subdivided1, subdivided2;
    ToVisitCache toVisit;
    DebugReport report;
    PointsInfo info;

    public int DebugNodeCount(MultiPolygon2i set)
    {
        int c = 0;
        set.ForEachBoundary(p => c += p.Count());
        return c;
    }

    public DebugReport GetLastReport()
    {
        return report;
    }

    public MultiPolygon2i Validate(MultiPolygon2i poly)
    {
        MultiPolygon2i ret = new MultiPolygon2i();

        for(int i=0; i<poly.Count(); ++i)
        {
            List<Vector2Int> stack = new List<Vector2Int>();
            LinearRing2i ring = poly.Get(i);

            for(int n=0; n<ring.Count() + 1; ++n)
            {
                if (stack.Contains(ring.At(n)))
                {
                    var start = stack.IndexOf(ring.At(n));
                    var end = stack.Count - 1;
                    LinearRing2i ring2 = new LinearRing2i();
                    for(int n2=start; n2<=end; ++n2) ring2.Add(stack[n2]);
                    if (ring2.Count() > 2)
                    {
                        ring2.ComputeOrientation();
                        ret.Add(ring2);
                    }
                    for(int s=0; s<ring2.Count() - 1; ++s)
                        stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    stack.Add(ring.At(n));
                }
            }
        }

        return ret;
    }

    public void Initialize(MultiPolygon2i polygons1, MultiPolygon2i polygons2)
    {
        report = new DebugReport();

        // normalize positions (fixed precision to 0.01)
        // polygons1.ForEachBoundary(p => p.ForEachIndex(i => p.Set(i, p.At(i))));
        // polygons2.ForEachBoundary(p => p.ForEachIndex(i => p.Set(i, p.At(i))));
        report.originals.Add(polygons1);
        report.originals.Add(polygons2);

        cache = RingTraversal.ComputeIPoints(polygons1, polygons2);
        report.intersectionPoints = new List<Vector2>();
        foreach(var p in cache.GetPoints())
            report.intersectionPoints.Add(p);

        // Debug.Log("cache size : " + cache.ipoints2.Count);
        subdivided1 = Validate(RingTraversal.SubdivideSegments(polygons1, cache, true));
        subdivided2 = Validate(RingTraversal.SubdivideSegments(polygons2, cache, false));
        report.subdivideds.Add(subdivided1);
        report.subdivideds.Add(subdivided2);

        info = RingTraversal.ComputeInfos(subdivided1, subdivided2);
        report.pointInfos = info.infos;

        report.indexed1 = IndexedRingSet(subdivided1, info);
        report.indexed2 = IndexedRingSet(subdivided2, info);

        toVisit = RingTraversal.CreateToVisitCache(subdivided1, subdivided2);
    }

    public List<RingInt> IndexedRingSet(MultiPolygon2i polygons, PointsInfo info)
    {
        List<RingInt> ret = new List<RingInt>();

        for (int pathId = 0; pathId < polygons.Count(); pathId++)
        {
            RingInt ringInt = new RingInt();

            LinearRing2i path = polygons.Get(pathId);
            for(int n1=0; n1<path.Count(); ++n1)
                ringInt.nodes.Add(info.Index(path.At(n1)));
            
            ret.Add(ringInt);
        }

        return ret;
    }

    public void Bake(MultiPolygon2i set)
    {
        RingTraversal.FilterPaths(set, path => {
            bool outside = true;
            bool remove = false;
            Vector2Int p = MultiPolygon2iOperation.GetInsidePoint(path);
            var ray = MultiPolygon2iOperation.RayCastOnPaths(set, p);

            for (int i = 0; i < ray.Count; i++)
            {
                if (ray[i].Value == path)
                    continue;

                Side side = MultiPolygon2iOperation.GetSide(ray[i].Value, path);
                if (side == Side.In)
                {
                    outside = false;
                    remove = ray[i].Value.IsCCW() == path.IsCCW();
                    break;
                }
            }
            return remove || (outside && path.IsCW());
        });
    }

    public IPointCache GetEdgePoints(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        IPointCache edgePoints = new IPointCache();
        foreach(var i in info.infos)
        {
            if (set1.Composes(i.value) && set2.Composes(i.value))
            {
                // Debug.Log("add edge point " + i.value);
                edgePoints.Add(new SegmentPair(i.indexes1, i.indexes2), i.value);
            }
        }
        return edgePoints;
    }

    public MultiPolygon2i ApplyPolygonOperation(MultiPolygon2i set1, MultiPolygon2i set2, Side sideToFilter)
    {
        Initialize(set1, set2);
        IPointCache edgePoints = GetEdgePoints(subdivided1, subdivided2);

        // generate result
        int fuse = 100;
        MultiPolygon2i result = new MultiPolygon2i();
        List<KeyValuePair<Vector2Int,Vector2Int>> visited = new List<KeyValuePair<Vector2Int,Vector2Int>>();
        while (!toVisit.Empty())
        {
            var next = toVisit.PickToVisit();
            RingTraversal.PathsTraversal2(info, subdivided1, subdivided2, next.indexes, edgePoints, report, toVisit, sideToFilter == Side.In, result, visited);

            if (fuse-- == 0)
            {
                Debug.LogError("infinite loop in ApplyPolygonOperation");
                report.bug = true;
                break;
            }
        }

        result = Validate(result);

        // filter paths depending on sideToFilrer
        RingTraversal.FilterPaths(result, path =>
        {
            Side sideSet1 = MultiPolygon2iOperation.GetSide(subdivided1, path);
            Side sideSet2 = MultiPolygon2iOperation.GetSide(subdivided2, path);
            return sideSet1 == sideToFilter || sideSet2 == sideToFilter;
        });

        // remove plain in plain; hole in hole
        // Bake(result);

        report.indexedResult = IndexedRingSet(result, info);

        return result;
    }

    public Rect GetDom(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        Rect r1 = MultiPolygon2iOperation.Bounds(set1);
        Rect r2 = MultiPolygon2iOperation.Bounds(set2);

        Rect r = r1;
        r.min = Vector2.Min(r1.min, r2.min);
        r.max = Vector2.Max(r1.max, r2.max);
        // r.size = r.size * 2.0f;
        var c = (r.min + r.max) * 0.5f;
        r.min = c + (r.min - c) * 2.0f;
        r.max = c + (r.max - c) * 2.0f;
        return r;
    }

    public MultiPolygon2i Union2(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        // A + B
        // intersect and remove path inside set1 or set2
        return ApplyPolygonOperation(set1, set2, Side.In);
    }

    public MultiPolygon2i Intersection2(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        // A * B
        // intersect and remove path outside set1 or set2
        return ApplyPolygonOperation(set1, set2, Side.Out);
    }

    public LinearRing2i FromRect(Rect rect)
    {
        List<Vector2Int> nodes = new List<Vector2Int>();
        nodes.Add(Vector2Int.RoundToInt(rect.min));
        nodes.Add(new Vector2Int((int)rect.max.x, (int)rect.min.y));
        nodes.Add(Vector2Int.RoundToInt(rect.max));
        nodes.Add(new Vector2Int((int)rect.min.x, (int)rect.max.y));
        return new LinearRing2i(nodes);
    }

    public MultiPolygon2i Substraction2(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        // A * !B
        LinearRing2i dom = FromRect(GetDom(set1,set2));
        return Intersection2(set1, set2.Reverse(dom));
    }

    public MultiPolygon2i Exclusion2(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        // (A + B) * !(A * B)
        MultiPolygon2i u = Union2(set1, set2);
        LinearRing2i dom = FromRect(GetDom(set1, set2));
        MultiPolygon2i i = Intersection2(set1, set2).Reverse(dom);
        return Intersection2(u, i);
    }

    public MultiPolygon2i Dom(MultiPolygon2i set1, MultiPolygon2i set2)
    {
        LinearRing2i dom = FromRect(GetDom(set1, set2));
        MultiPolygon2i set = new MultiPolygon2i();
        set.Add(dom);
        return set;
    }
}
