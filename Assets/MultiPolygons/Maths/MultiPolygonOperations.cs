using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiPolygon2iOperation
{
    static public Side GetSide(LinearRing2i path, Vector2Int p)
    {
        if (path.nodes.Contains(p)) return Side.Edge;

        VecIntOperation.PolygonRayCastInfo info = new VecIntOperation.PolygonRayCastInfo();
        Vector2Int ext = VecIntOperation.OutsidePoint(path.nodes, p);
        var result = VecIntOperation.PolygonPointSide(ext, p, path.nodes, ref info);

        return result == VecIntOperation.Result.Outside ? Side.Out : Side.In;
    }

    static public int MultiPolygonWindingNumber(MultiPolygon2i set, Vector2Int p)
    {
        int w2 = 0;

        for(int i=0; i<set.Count(); ++i)
        {
            var polygon = set.Get(i).nodes;
            Vector2Int last = polygon[polygon.Count - 1];
            foreach(var curr in polygon)
            {
                int r2 = VecIntOperation.SegmentWindingNumber(last - p, curr - p);
                w2 += r2;
                last = curr;
            }
        }
        
        return w2 / 2;
    }
    
    static public Vector2Int GetInsidePoint(LinearRing2i path)
    {
        var nodes = path.nodes;

        // find smallest x/y node (garanteed to be a vertex of the convex hull of polygon)
        int index = 0;
        for(int i=0; i<path.Count(); ++i)
        {
            if (nodes[i].x < nodes[index].x || (nodes[i].x == nodes[index].x && nodes[i].y < nodes[index].y))
                index = i;
        }

        Vector2Int curr = path.At(index);
        Vector2Int prev = path.At(index - 1);
        Vector2Int next = path.At(index + 1);

        LinearRing2i tri = new LinearRing2i();
        tri.Add(curr); tri.Add(next); tri.Add(prev);

        int index2 = -1;
        float mind = 1e9f;
        for(int i = 0; i<path.Count(); ++i)
        {
            if (MultiPolygon2iOperation.GetSide(tri, nodes[i]) == Side.In)
            {
                float d = Vector2.Distance(nodes[i], curr);
                if (d < mind)
                {
                    mind = d;
                    index2 = i;
                }
            }
        }

        if (index2 == -1)
        {
            // midpoint of adjacent points
            if (nodes.Count > 3)
                return (prev + next) / 2;
            else
                return (curr + prev + next) / 3;
        }
        else
        {
            // midpoint of curr and index2
            return (curr + nodes[index2]) / 2;
        }
    }

    static public Side GetSide(LinearRing2i path, LinearRing2i other)
    {
        int onEdge = 0;
        int inner = 0;
        int outer = 0;

        foreach(var p in other.nodes)
        {
            var side = GetSide(path, p);
            if (side == Side.Edge)
                onEdge++;
            else if (side == Side.Out)
                outer++;
            else
                inner++;
        }

        if (outer > 0 && inner > 0)
            return Side.Cross;
        else if (outer == 0 && inner > 0)
            return Side.In;
        else if (outer > 0 && inner == 0)
            return Side.Out;
        else if (onEdge < path.Count())
        {
            Vector2Int px = GetInsidePoint(other);
            return GetSide(path, px);
        }
        else if (onEdge == path.Count())
            return Side.Edge;
        else
            return Side.None;
    }

    static public Side GetSide(MultiPolygon2i MultiPolygon2i, LinearRing2i other)
    {
        int e = 0;
        int i = 0;
        int o = 0;

        int it = 0;
        foreach(var p in other.nodes)
        {
            Side s = GetSide(MultiPolygon2i, p);
            if (s == Side.Edge) e++;
            if (s == Side.In) i++;
            if (s == Side.Out) o++;
            it++;
        }

        // Debug.Log("GetSide " + MultiPolygon2i + " vs " + other + "=> i=" + i + "; o=" + o + "; e=" + e);

        if (i > 0 && o > 0)
            return Side.Cross;
        if (i == 0 && o == 0 && e > 0)
            return Side.Edge;
        if (i > 0 && o == 0)
            return Side.In;
        if (i == 0 && o > 0)
            return Side.Out;

        return Side.None;
    }
    static public Side GetSide(MultiPolygon2i set, Vector2Int node)
    {
        if (set.Composes(node))
            return Side.Edge;

        var c = MultiPolygonWindingNumber(set, node); // RayCastOnPathsCount(MultiPolygon2i, node);
        // Debug.Log("raycast = " + c);
        return c > 0 ? Side.In : Side.Out;
    }

    static public Vector2Int GetOutsidePointForRayCast(MultiPolygon2i path, Vector2Int refp)
    {
        List<Vector2Int> outPoints = new List<Vector2Int>();
        path.ForEachBoundary(p => outPoints.Add(VecIntOperation.OutsidePoint(p.nodes, refp)) );

        Vector2Int outPoint = outPoints[0];
        foreach(var p in outPoints) outPoint = Vector2Int.Min(outPoint, p);
        return outPoint;
    }

    static public int RayCastOnPathsCount(MultiPolygon2i MultiPolygon2i, Vector2Int node)
    {
        HashSet<Vector2> set = new HashSet<Vector2>();

        Vector2Int ext = GetOutsidePointForRayCast(MultiPolygon2i, node);

        VecIntOperation.PolygonRayCastInfo info = new VecIntOperation.PolygonRayCastInfo();

        foreach (var path in MultiPolygon2i.boundaries)
        {
            if (path.Composes(node))
            {
                set.Add(node);
                continue;
            }

            foreach(var ipt in VecIntOperation.PolygonRayCastExt2( ext, node, path.nodes, ref info))
            {
                set.Add(ipt);
            }
        }

        return set.Count;
    }

    static public List<KeyValuePair<Vector2, LinearRing2i>> RayCastOnPaths(MultiPolygon2i MultiPolygon2i, Vector2Int node)
    {
        List<KeyValuePair<Vector2, LinearRing2i>> ret = new List<KeyValuePair<Vector2, LinearRing2i>>();

        Vector2Int ext = GetOutsidePointForRayCast(MultiPolygon2i, node);

        VecIntOperation.PolygonRayCastInfo info = new VecIntOperation.PolygonRayCastInfo();

        foreach (var path in MultiPolygon2i.boundaries)
        {
            if (path.Composes(node))
            {
                ret.Add(new KeyValuePair<Vector2, LinearRing2i>(node, (LinearRing2i)path));
                continue;
            }

            foreach(var ipt in VecIntOperation.PolygonRayCastExt2( ext, node, path.nodes, ref info))
            {
                ret.Add(new KeyValuePair<Vector2, LinearRing2i>(ipt, (LinearRing2i)path));
            }
        }

        ret.Sort((a, b) => {
            return Vector2.Distance(node, a.Key) < Vector2.Distance(node, b.Key) ? -1 : 1;
        });

        return ret;
    }

    static public Rect Bounds(LinearRing2i path)
    {
        Rect bounds = new Rect(0, 0, 0, 0);
        if (path.Count() > 0)
            bounds = new Rect(path.At(0), new Vector2(0.0f, 0.0f));
        foreach(var p in path.nodes)
        {
            bounds.max = Vector2.Max(bounds.max, p);
            bounds.min = Vector2.Min(bounds.min, p);
        }
        return bounds;
    }

    static public Rect Bounds(MultiPolygon2i MultiPolygon2i)
    {
        Rect bounds = new Rect(0, 0, 0, 0);
        bool started = false;
        foreach(var path in MultiPolygon2i.boundaries)
        {
            var bnds = Bounds((LinearRing2i)path);
            if (!started)
            {
                bounds = bnds;
                started = true;
            }
            else
            {
                bounds.min = Vector2.Min(bounds.min, bnds.min);
                bounds.max = Vector2.Max(bounds.max, bnds.max);
            }
        }
        return bounds;
    }
}
