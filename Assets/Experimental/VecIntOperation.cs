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

    static public int Loop(int i, int n)
    {
        while(i >= n) i -= n;
        while(i < 0) i += n;
        return i;
    }

    public class PolygonRayCastInfo
    {
        public List<Line> intersectLines;
        public List<Vector2Int> intersectPoints;

        public void Reset()
        {
            intersectLines = new List<Line>();
            intersectPoints = new List<Vector2Int>();
        }
    }

    static public Vector2Int OutsidePoint(List<Vector2Int> polygon, Vector2Int refP)
    {
        Vector2Int min = polygon[0];

        foreach(var p in polygon)
            min = Vector2Int.Min(p, min);

        min -= new Vector2Int(20,20);
        min.y = refP.y;
        return min;
    }

    static public int SegmentWindingNumber(Vector2Int s1, Vector2Int s2)
    {
        int w2 = 0;

        if (s1.y*s2.y < 0) // s1s2 crosses the x-axis
        {
            // R is the x-coordinate of the intersection of s1s2 and the x-axis
            float R = s1.x + s1.y * (s2.x - s1.x ) / (float)( s1.y - s2.y );
            // if R > 0 -> positive x-axis, else negative x-axis

            // Debug.Log("R = " + R + "; s1.y = " + s1.y);

            if(s1.y * R < 0.0f)
                // crossing from below of positive x-axis
                // or above negative x-axis
                w2 = 2;
            else // crossing from above of positive x-axis
            // or below negative x-axis
                w2 = -2;
        }
        else if (s1.y == 0 && s2.y == 0) // points are on the boundary
        {
            // unchanged
            w2 = 0;
        }
        else if(s1.y == 0) // V1 on x-axis
        {
            if(s2.y * s1.x > 0)
                // s1 is on positive x-axis and s2 above,
                // or s1 is on negative x-axis and s2 below
                w2 = 1;
            else
                // s1 is on negative x-axis and s2 above,
                // or s1 is on positive x-axis and s2 below
                w2 = -1;
        }
        else if(s2.y == 0) // s2 is on x-axis
        {
            if(s1.y * s2.x < 0)
                // s2 is on negative x-axis and s1 above,
                // or s2 is on positive x-axis and s1 below
                w2 = 1;
            else
                // s2 is on positive x-axis and s1 above,
                // or s2 is on negative x-axis and s1 below
                w2 = -1;
        }

        return w2;
    }

    static public int PolygonWindingNumber(Vector2Int p, List<Vector2Int> polygon)
    {
        int w2 = 0;

        Vector2Int last = polygon[polygon.Count - 1];
        foreach(var curr in polygon)
        {
            int r2 = SegmentWindingNumber(last - p, curr - p);
            w2 += r2;
            last = curr;
        }
        
        return w2 / 2;
    }
    
    static public List<Vector2> PolygonRayCastExt2(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, ref PolygonRayCastInfo outInfo)
    {
        List<Vector2> ret = new List<Vector2>();

        outInfo.Reset();
        
        // find intersected segments
        int j = polygon.Count - 1;
        Vector2Int last = polygon[j];
        for(int i=0; i<polygon.Count; ++i)
        {
            var curr = polygon[i];
            // exclude segments with point on ray
            if (curr.y == o.y && curr.x >= o.x && curr.x <= p.x)
            {
                outInfo.intersectPoints.Add(curr);

                if (last.y == o.y)
                {
                    // ignore
                }
                else
                {
                    outInfo.intersectLines.Add(new Line(last, curr));
                    ret.Add(curr);
                }
            }
            else
            {
                Vector2 ipt = new Vector2();
                var iRes = GeometryMath.SegToSegf(last, curr, o, p, ref ipt);
                if (iRes == ISeg.Yes)
                {
                    outInfo.intersectLines.Add(new Line(last, curr));
                    ret.Add(ipt);
                }
            }
            j = i;
            last = curr;
        }

        return ret;
    }

    static public Result PolygonPointSide(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, ref PolygonRayCastInfo outInfo)
    {
        outInfo.Reset();
        int iCount = PolygonWindingNumber(p, polygon);
        return iCount > 0 ? Result.Inside : Result.Outside;
    }

    static public float CurveAngle(Vector2Int prev, Vector2Int mid, Vector2Int next)
    {
        Vector2 v1 = (Vector2)mid - (Vector2)prev;
        Vector2 v2 = (Vector2)next - (Vector2)mid;
        
        return Vector2.SignedAngle(v1, v2);
    }
}
