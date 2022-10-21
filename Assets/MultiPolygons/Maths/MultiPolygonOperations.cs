using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiPolygon2iOperation
{
    static public Side GetSide(LinearRing2i path, Vector2Int p)
    {
        var ray = RayCast(path, p);
        if (ray == -1)
            return Side.Edge;
        else if (ray % 2 == 0)
            return Side.Out;
        else
            return Side.In;
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
        foreach(var p in other.nodes)
        {
            Side s = GetSide(MultiPolygon2i, p);
            if (s == Side.Edge) e++;
            if (s == Side.In) i++;
            if (s == Side.Out) o++;
        }

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
    static public Side GetSide(MultiPolygon2i MultiPolygon2i, Vector2Int node)
    {
        if (MultiPolygon2i.Composes(node))
            return Side.Edge;

        var ipoints = RayCastOnPaths(MultiPolygon2i, node);
        return ipoints.Count % 2 == 0 ? Side.Out : Side.In;
    }

    static public int RayCast(LinearRing2i path, Vector2Int o, Vector2Int node)
    {
        if (path.nodes.Contains(node))
            return -1;

        Vector2 ignored = new Vector2();

        int ic = 0;
        path.ForEachSegment((p1, p2) => {
            if (GeometryMath.SegToSeg(o, node, p1, p2, ref ignored) == ISeg.Yes)
                ic++;
        });
        return ic;
    }

    static public int RayCast(LinearRing2i path, Vector2Int node)
    {
        // Vector2 ext = Bounds().min - new Vector2(-7.49215f, -3.345612f);
        Vector2Int ext = GetOutsidePointForRayCast(path, node.x);
        return RayCast(path, ext, node);
    }

    static public Vector2Int GetOutsidePointForRayCast(LinearRing2i path, float xref)
    {
        float x = xref;
        bool ok = true;

        int tryCount = 10;
        do
        {
            ok = true;
            foreach(var n in path.nodes)
            {
                if (Mathf.Abs(n.x - x) < GeometryMath.Epsilon)
                {
                    x += 0.01f;
                    ok = false;
                }
            }
            if (tryCount-- == 0)
            {
                Debug.LogWarning("Path: Failed to find a good outside point");
                break;
            }
        }
        while (!ok);

        return new Vector2Int((int)x, (int)(Bounds(path).min.y) - 5);
    }

    static public Vector2 GetOutsidePointForRayCast(MultiPolygon2i path, float xref)
    {
        float x = xref;
        bool ok = true;

        int tryCount = 10;
        do
        {
            ok = true;
            path.ForEachBoundary(p =>
            {
                foreach(var n in p.nodes)
                {
                    if (Mathf.Abs(n.x - x) < GeometryMath.Epsilon)
                    {
                        x += 0.01f;
                        ok = false;
                    }
                }
            });
            if (tryCount-- == 0)
            {
                Debug.LogWarning("Polygon: Failed to find a good outside point");
                break;
            }
        }
        while (!ok);

        return new Vector2(x, Bounds(path).min.y - 5.0f);
    }

    static public List<KeyValuePair<Vector2, LinearRing2i>> RayCastOnPaths(MultiPolygon2i MultiPolygon2i, Vector2Int node)
    {
        List<KeyValuePair<Vector2, LinearRing2i>> ret = new List<KeyValuePair<Vector2, LinearRing2i>>();

        // Vector2 ext = Bounds().min - new Vector2(-7.49215f, -3.345612f);
        Vector2 ext = GetOutsidePointForRayCast(MultiPolygon2i, node.x);
        Vector2 ipt = new Vector2();

        foreach (var path in MultiPolygon2i.boundaries)
        {
            if (path.Composes(node))
            {
                ret.Add(new KeyValuePair<Vector2, LinearRing2i>(node, (LinearRing2i)path));
                continue;
            }

            path.ForEachSegment((p1, p2) => {
                if (GeometryMath.SegToSeg(ext, node, p1, p2, ref ipt) == ISeg.Yes)
                    ret.Add(new KeyValuePair<Vector2, LinearRing2i>(ipt, (LinearRing2i)path));
            });
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
