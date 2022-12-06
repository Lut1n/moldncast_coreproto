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
    
    static public float SidePointLine(Vector2Int p, Vector2Int l1, Vector2Int l2)
    {
        Vector2 l1f = l1;
        Vector2 l2f = l2;
        Vector2 pf = p;

        Vector2 dif = (l2f - l1f);
        Vector2 dir = (pf - l1f);

        return Mathf.Sign( Vector3.Cross(dif, dir).z );
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

    static public int CountIPoint(Vector2Int o, Vector2Int p, List<Vector2Int> path, HashSet<int> ipoints)
    {
        Dictionary<int, IntPair> adjacents = new Dictionary<int, IntPair>();

        foreach(var i in ipoints)
        {
            adjacents[i] = new IntPair(Loop(i-1, path.Count), Loop(i+1, path.Count));
        }

        return 0;
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
    
    static public int PolygonRayCast(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, ref PolygonRayCastInfo outInfo)
    {
        int iCount = 0;

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
                    iCount += 1;
                }
            }
            else
            {
                Vector2Int ipt = new Vector2Int();
                var iRes = GeometryMath.SegToSegi(last, curr, o, p, ref ipt);
                if (iRes == ISeg.Yes)
                {
                    outInfo.intersectLines.Add(new Line(last, curr));
                    iCount += 1;
                }
            }
            j = i;
            last = curr;
        }

        return iCount;
    }

    static public int SegmentWindingNumber(Vector2Int s1, Vector2Int s2)
    {
        int w2 = 0;

        if (s1.y*s2.y < 0) // ViVi1 crosses the x-axis
        {
            float R = s1.x + s1.y * (s2.x - s1.x ) / (float)( s1.y - s2.y ); // R is the x-coordinate of the intersection of ViVi1 and the x-axis
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
                // Vi is on positive x-axis and Vi1 above,
                // or Vi is on negative x-axis and Vi1 below
                w2 = 1;
            else
                // Vi is on negative x-axis and Vi1 above,
                // or Vi is on positive x-axis and Vi1 below
                w2 = -1;
        }
        else if(s2.y == 0) // Vi1 is on x-axis
        {
            if(s1.y * s2.x < 0)
                // Vi1 is on negative x-axis and Vi above,
                // or Vi1 is on positive x-axis and Vi below
                w2 = 1;
            else
                // Vi1 is on positive x-axis and Vi above,
                // or Vi1 is on negative x-axis and Vi below
                w2 = -1;
        }

        return w2;
    }

    static public int PolygonWindingNumber(Vector2Int p, List<Vector2Int> polygon)
    {
        int w2 = 0;
        // int it = 0;

        Vector2Int last = polygon[polygon.Count - 1];
        foreach(var curr in polygon)
        {
            int r2 = SegmentWindingNumber(last - p, curr - p);
            // Debug.Log("it " + it + " r=" + r2);
            w2 += r2;
            last = curr;
            // it++;
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

    static public List<Vector2Int> PolygonRayCastExt2i(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, ref PolygonRayCastInfo outInfo)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        outInfo.Reset();
        
        // find intersected segments
        int j = polygon.Count - 1;
        Vector2Int last = polygon[j];
        for(int i=0; i<polygon.Count; ++i)
        {
            var curr = polygon[i];
            // I should:
            // - exclude segments with 2 points on p.y
            // - detect curr and last on ray

            // - if curr on ray:
            //    - if last.y < curr.y : N -= 1
            //    - if last.y > curr.y : N += 1
            // - else if last on ray:
            //    - if curr.y > last.y : N += 1
            //    - if curr.y < last.y : N -= 1
            //
            // @see point_in_polygon.pdf

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
                Vector2Int ipt = new Vector2Int();
                var iRes = GeometryMath.SegToSegi(last, curr, o, p, ref ipt);
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

    static public List<Vector2Int> PolygonRayCastExt(Vector2Int o, Vector2Int p, List<Vector2Int> polygon, ref PolygonRayCastInfo outInfo)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        outInfo.Reset();
        List<int> iPointIdx = new List<int>();

        // find points on the ray
        Vector2Int last = polygon[polygon.Count - 1];
        for(int i=0; i<polygon.Count; ++i)
        {
            var curr = polygon[i];
            float ignored = 0.0f;
            if (ComputePointToLine(curr, o, p, 0.5f, 0.5f, ref ignored) == Result.OnTheLine)
            {
                outInfo.intersectPoints.Add(curr);
                iPointIdx.Add(i);
            }
            last = curr;
        }
        foreach(var retp in TanIntersectExt(polygon, iPointIdx, o, p))
        {
            ret.Add(retp);
        }

        // find intersected segments
        int j = polygon.Count - 1;
        last = polygon[j];
        for(int i=0; i<polygon.Count; ++i)
        {
            var curr = polygon[i];
            // exclude segments with point on ray
            if (!iPointIdx.Contains(i) && !iPointIdx.Contains(j))
            {
                Vector2Int ipt = new Vector2Int();
                var iRes = GeometryMath.SegToSegi(last, curr, o, p, ref ipt);
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
        // Debug.Log("wn = " + iCount);
        return iCount > 0 ? Result.Inside : Result.Outside;
    }

    static public void UpdateGroup(List<int> groups, int old , int newV)
    {
        int name = old < newV ? old : newV;

        for(int i=0; i<groups.Count; ++i)
            if (groups[i] == old || groups[i] == newV) groups[i]= name;
    }

    static public int TanIntersect(List<Vector2Int> polygon, List<int> iPoints, Vector2Int o, Vector2Int p)
    {
        int pathc = polygon.Count;
        int c = iPoints.Count;
        
        // group adjacent points
        List<int> groups  = new List<int>();
        for(int i=0; i<c;++i) groups.Add(i);
        for(int i=0; i<c; ++i)
        {
            int index = iPoints.FindIndex(v => v==Loop(iPoints[i]-1, pathc));
            if (index != -1) UpdateGroup(groups, groups[index], groups[i]);
            index = iPoints.FindIndex(v => v == Loop(iPoints[i]+1, pathc));
            if (index != -1) UpdateGroup(groups, groups[index], groups[i]);
        }

        HashSet<int> uniqueGroupIds = new HashSet<int>();
        for(int i=0; i<c; ++i)
            uniqueGroupIds.Add(groups[i]);

        // count tangential intersection
        int ret = 0;

        // reduce equivalent for intersection
        foreach(var id in uniqueGroupIds)
        {
            // get group
            List<int> group = new List<int>();
            for(int g=0; g<c; ++g)
                if (groups[g] == id) group.Add(iPoints[g]);
            
            // exclude intermediate point ?
            // or find 2 end points for each group
            List<Vector2Int> ep = new List<Vector2Int>();
            // TODO, find ep1 and ep2
            foreach(var g in group)
            {
                int index = iPoints.FindIndex(v => v == Loop(g-1, pathc));
                if ( index == -1 ) ep.Add(polygon[Loop(g-1, pathc)]);
                int index2 = iPoints.FindIndex(v => v == Loop(g+1, pathc));
                if ( index2 == -1 ) ep.Add(polygon[Loop(g+1, pathc)]);
            }

            var msg = "points:";
            foreach(var db in group)
            {
                msg += " " + polygon[db];
            }
            Debug.Assert(ep.Count == 2, "group count = " + msg);

            // compute side of end points
            if ( ep.Count >= 2 && ( SidePointLine(ep[0], o, p) != SidePointLine(ep[1], o, p)))
            {
                ret++;
            }
        }
        
        return ret;
    }

    static public List<Vector2Int> TanIntersectExt(List<Vector2Int> polygon, List<int> iPoints, Vector2Int o, Vector2Int p)
    {
        int pathc = polygon.Count;
        int c = iPoints.Count;
        
        // group adjacent points
        List<int> groups  = new List<int>();
        for(int i=0; i<c;++i) groups.Add(i);
        for(int i=0; i<c; ++i)
        {
            int index = iPoints.FindIndex(v => v==Loop(iPoints[i]-1, pathc));
            if (index != -1) UpdateGroup(groups, groups[index], groups[i]);
            index = iPoints.FindIndex(v => v == Loop(iPoints[i]+1, pathc));
            if (index != -1) UpdateGroup(groups, groups[index], groups[i]);
        }

        HashSet<int> uniqueGroupIds = new HashSet<int>();
        for(int i=0; i<c; ++i)
            uniqueGroupIds.Add(groups[i]);

        // count tangential intersection
        List<Vector2Int> ret = new List<Vector2Int>();

        // reduce equivalent for intersection
        foreach(var id in uniqueGroupIds)
        {
            // get group
            List<int> group = new List<int>();
            for(int g=0; g<c; ++g)
                if (groups[g] == id) group.Add(iPoints[g]);
            
            // exclude intermediate point ?
            // or find 2 end points for each group
            List<Vector2Int> ep = new List<Vector2Int>();
            // TODO, find ep1 and ep2
            foreach(var g in group)
            {
                int index = iPoints.FindIndex(v => v == Loop(g-1, pathc));
                if ( index == -1 ) ep.Add(polygon[Loop(g-1, pathc)]);
                int index2 = iPoints.FindIndex(v => v == Loop(g+1, pathc));
                if ( index2 == -1 ) ep.Add(polygon[Loop(g+1, pathc)]);
            }

            //Debug.Assert(ep.Count == 2);

            // compute side of end points
            if ( ep.Count >= 2 && ( SidePointLine(ep[0], o, p) != SidePointLine(ep[1], o, p)))
            {
                ret.Add( (ep[0] + ep[1]) / 2 );
            }
        }
        
        return ret;
    }

    static public float CurveAngle(Vector2Int prev, Vector2Int mid, Vector2Int next)
    {
        Vector2 v1 = (Vector2)mid - (Vector2)prev;
        Vector2 v2 = (Vector2)next - (Vector2)mid;
        
        return Vector2.SignedAngle(v1, v2);
    }
}
