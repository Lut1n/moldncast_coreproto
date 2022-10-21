using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VecIntOperation
{
    public enum Result
    {
        Undefined,
        OnTheLine,
        Outside,
        Inside
    }

    public class Line
    {
        public Vector2Int a, b;

        public Line() {}
        public Line(Vector2Int a, Vector2Int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    public class IntPair
    {
        public int a, b;
        public IntPair(int a, int b)
        {
            this.a = a;
            this.b = b;
        }
    }

    static public Result ComputePointToLine(Vector2Int p, Vector2Int l1, Vector2Int l2, float linewidth, float capend, ref float d)
    {
        Vector2 l1f = l1;
        Vector2 l2f = l2;
        Vector2 pf = p;

        Vector2 dif = (l2f - l1f);
        Vector2 dir = (pf - l1f);

        Vector2 tan = dif.normalized;

        d = Mathf.Abs( Vector3.Cross(tan, dir).z );

        float oft = Vector2.Dot(dir, tan);
        if (oft < -capend || oft > dif.magnitude + capend) return Result.Outside;

        return d > linewidth ? Result.Outside : Result.OnTheLine;

        // return Result.Undefined;
    }

    static public List<Vector2Int> SubdivisePath(List<Vector2Int> path, int subdiv)
    {
        List<Vector2Int> ret = new List<Vector2Int>();
        if (path.Count < 3) return ret;

        Vector2Int last = path[path.Count - 1];
        foreach (var curr in path)
        {
            for(int i=0; i<subdiv; ++i)
            {
                float f = (i + 1.0f) / (subdiv + 1.0f);
                Vector2Int s = Vector2Int.RoundToInt(Vector2.Lerp(last, curr, f));
                ret.Add(s);
            }
            ret.Add(curr);
            last = curr;
        }

        return ret;
    }

    static public Result PolygonRayCast(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, int subdiv, ref List<Line> intersectLines, ref List<Vector2Int> intersectPoints)
    {
        List<Vector2Int> subdivided = SubdivisePath(polygon, subdiv);

        int intersectionCount = 0;

        HashSet<int> pointIndex = new HashSet<int>();

        Vector2Int last = subdivided[subdivided.Count - 1];
        for(int i=0; i<subdivided.Count; ++i)
        {
            var curr = subdivided[i];

            Vector2Int ipt = new Vector2Int();
            var iRes = GeometryMath.SegToSeg(last, curr, o, p, ref ipt);
            if (iRes == ISeg.Yes)
            {
                intersectLines.Add(new Line(last, curr));
                intersectionCount += 1;
            }
            else if (iRes == ISeg.Edge)
            {
                pointIndex.Add(i);
                intersectPoints.Add(ipt);
            }
            last = curr;
        }

        intersectionCount += CountIPoint(o, p, subdivided, pointIndex);

        return (intersectionCount % 2) == 0 ? Result.Outside : Result.Inside;
        // return Result.Undefined;
    }

    static public int Loop(int i, int n)
    {
        while(i >= n) i -= n;
        while(i < 0) i += n;
        return i;
    }

    static public int CountIPoint(Vector2Int o, Vector2Int p, List<Vector2Int> path, HashSet<int> ipoints)
    {
        Dictionary<int, IntPair> adjacents = new Dictionary<int, IntPair>();

        foreach(var i in ipoints)
        {
            adjacents[i] = new IntPair(Loop(i-1, path.Count), Loop(i+1, path.Count));
        }

        return 0;
    }
}
