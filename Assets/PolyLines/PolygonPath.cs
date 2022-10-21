using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// on midpoint @see http://apodeline.free.fr/FAQ/CGAFAQ/CGAFAQ-3.html
//                  https://mathoverflow.net/questions/56655/get-a-point-inside-a-polygon

namespace PolyLine
{
    public enum Side
    {
        None,
        In,
        Out,
        Cross,
        Edge
    }

    public enum Orientation
    {
        Unknown,
        CCW,
        CW
    }

    [System.Serializable]
    public class PolygonPathVec2
    {
        public delegate void SegmentHandler(Vector2 n1, Vector2 n2);
        public delegate void NodeHandler(Vector2 node);
        public delegate void IndexHandler(int i);
//
        /*protected*/ public List<Vector2> nodes = new List<Vector2>();
        /*protected*/ public Orientation orientation = Orientation.Unknown;

         public void ForEachNode(NodeHandler handler)
        {
            foreach (var node in nodes)
                handler(node);
        }
        public void ForEachIndex(IndexHandler handler)
        {
            for (int i = 0; i < nodes.Count; ++i)
                handler(i);
        }
//
        public void ForEachSegment(SegmentHandler handler)
        {
            if (nodes.Count == 0)
                return;
//
            Vector2 prev = nodes[nodes.Count - 1];
            ForEachNode(node => {
                handler(prev, node);
                prev = node;
            });
        }
//
        public void PushNode(Vector2 node)
        {
            nodes.Add(node);
            orientation = Orientation.Unknown;
        }
//
        public Vector2 At(int i)
        {
            while (i >= nodes.Count)
                i -= nodes.Count;
            while (i < 0)
                i += nodes.Count;
            return nodes[i];
        }
//
        public void Set(int i, Vector2 v)
        {
            nodes[i] = v;
        }
//
        public int Count()
        {
            return nodes.Count;
        }
//
        public int IndexOf(Vector2 node)
        {
            return nodes.IndexOf(node);
        }

        public static PolygonPathVec2 FromRect(Rect rect)
        {
            PolygonPathVec2 ret = new PolygonPathVec2();
            ret.PushNode(rect.min);
            ret.PushNode(new Vector2(rect.max.x, rect.min.y));
            ret.PushNode(rect.max);
            ret.PushNode(new Vector2(rect.min.x, rect.max.y));
            return ret;
        }

        public Orientation ComputeOrientation()
        {
            if (orientation == Orientation.Unknown && nodes.Count > 0)
            {
                // find smallest x/y node (garanteed to be a vertex of the convex hull of polygon)
                int index = 0;
                ForEachIndex(i => {
                    if (nodes[i].x < nodes[index].x || (nodes[i].x == nodes[index].x && nodes[i].y < nodes[index].y))
                        index = i;
                });

                // compute cross product
                Vector2 curr = At(index);
                Vector2 prev = At(index - 1);
                Vector2 next = At(index + 1);
                float result = Vector3.Cross(next - curr, prev - curr).z;

                orientation = result > 0.0f ? Orientation.CCW : Orientation.CW;
            }
            return orientation;
        }
        public Vector2 GetInsidePoint()
        {
            // find smallest x/y node (garanteed to be a vertex of the convex hull of polygon)
            int index = 0;
            ForEachIndex(i => {
                if (nodes[i].x < nodes[index].x || (nodes[i].x == nodes[index].x && nodes[i].y < nodes[index].y))
                    index = i;
            });

            Vector2 curr = At(index);
            Vector2 prev = At(index - 1);
            Vector2 next = At(index + 1);

            PolygonPathVec2 tri = new PolygonPathVec2();
            tri.PushNode(curr); tri.PushNode(next); tri.PushNode(prev);

            int index2 = -1;
            float mind = 1000.0f;
            ForEachIndex(i => {
                if (tri.GetSide(nodes[i]) == Side.In)
                {
                    float d = Vector2.Distance(nodes[i], curr);
                    if (d < mind)
                    {
                        mind = d;
                        index2 = i;
                    }
                }
            });

            if (index2 == -1)
            {
                // midpoint of adjacent points
                if (nodes.Count > 3)
                    return (prev + next) * 0.5f;
                else
                    return (curr + prev + next) * 0.333333f;
            }
            else
            {
                // midpoint of curr and index2
                return (curr + nodes[index2]) * 0.5f;
            }
        }

        public bool IsCCW()
        {
            return ComputeOrientation() == Orientation.CCW;
        }

        public bool IsCW()
        {
            return ComputeOrientation() == Orientation.CW;
        }

        public Rect Bounds()
        {
            Rect bounds = new Rect(0, 0, 0, 0);
            if (nodes.Count > 0)
                bounds = new Rect(nodes[0], new Vector2(0.0f, 0.0f));
            ForEachNode(p => {
                bounds.max = Vector2.Max(bounds.max, p);
                bounds.min = Vector2.Min(bounds.min, p);
            });
            return bounds;
        }

        public bool Composes(Vector2 node)
        {
            return nodes.Contains(node);
        }
        
        public int RayCast(Vector2 o, Vector2 node)
        {
            if (nodes.Contains(node))
                return -1;

            Vector2 ignored = new Vector2();

            int ic = 0;
            ForEachSegment((p1, p2) => {
                if (GeometryMath.SegToSeg(o, node, p1, p2, ref ignored) == ISeg.Yes)
                    ic++;
            });
            return ic;
        }

        public int RayCast(Vector2 node)
        {
            // Vector2 ext = Bounds().min - new Vector2(-7.49215f, -3.345612f);
            Vector2 ext = GetOutsidePointForRayCast(node.x);
            return RayCast(ext, node);
        }

        public Vector2 GetOutsidePointForRayCast(float xref)
        {
            float x = xref;
            bool ok = true;

            int tryCount = 10;
            do
            {
                ok = true;
                ForEachNode(n => {
                    if (Mathf.Abs(n.x - x) < GeometryMath.Epsilon)
                    {
                        x += 0.01f;
                        ok = false;
                    }
                });
                if (tryCount-- == 0)
                {
                    Debug.LogWarning("Path: Failed to find a good outside point");
                    break;
                }
            }
            while (!ok);

            return new Vector2(x, Bounds().min.y - 5.0f);
        }

        public Side GetSide(Vector2 p)
        {
            var ray = RayCast(p);
            if (ray == -1)
                return Side.Edge;
            else if (ray % 2 == 0)
                return Side.Out;
            else
                return Side.In;
        }

        public Side GetSide(PolygonPathVec2 other)
        {
            int onEdge = 0;
            int inner = 0;
            int outer = 0;

            other.ForEachNode(p => {
                var side = GetSide(p);
                if (side == Side.Edge)
                    onEdge++;
                else if (side == Side.Out)
                    outer++;
                else
                    inner++;
            });

            if (outer > 0 && inner > 0)
                return Side.Cross;
            else if (outer == 0 && inner > 0)
                return Side.In;
            else if (outer > 0 && inner == 0)
                return Side.Out;
            else if (onEdge < Count())
                return GetSide(other.GetInsidePoint());
            else if (onEdge == Count())
                return Side.Edge;
            else
                return Side.None;
        }

        public bool Equivalents(PolygonPathVec2 other)
        {
            if (nodes.Count == 0)
                return other.nodes.Count == 0;

            int idx2 = other.nodes.IndexOf(nodes[0]);
            if (idx2 == -1)
                return false;

            for (int i = 0; i < nodes.Count; ++i)
                if (nodes[i] != other.At(idx2 + i))
                    return false;

            return true;
        }

        public Vector2 Center()
        {
            Vector2 center = new Vector2();
            ForEachNode(p => center += p);
            return center / nodes.Count;
        }

        public PolygonPathVec2 Offseted(Vector2 oft)
        {
            PolygonPathVec2 ret = new PolygonPathVec2();
            ForEachNode(n => ret.PushNode(n + oft));
            return ret;
        }

        public List<Vector2> GetNodes()
        {
            return nodes;
        }
//
        public PolygonPathVec2 Copy()
        {
            PolygonPathVec2 ret = new PolygonPathVec2();
            foreach(var n in nodes)
                ret.nodes.Add(n);
            return ret;
        }
    }

    [System.Serializable]
    public class PolygonPathSet
    {
        public delegate void PathHandler(PolygonPathVec2 path);

        public List<PolygonPathVec2> paths = new List<PolygonPathVec2> ();

        public void ForEachPath(PathHandler handler)
        {
            foreach(var path in paths)
                handler(path);
        }

        public Rect Bounds()
        {
            Rect bounds = new Rect(0, 0, 0, 0);
            bool started = false;
            foreach(var path in paths)
            {
                var bnds = path.Bounds();
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

        public void AddPath(PolygonPathVec2 path)
        {
            path.ComputeOrientation();
            paths.Add(path);
        }

        public void RemovePath(PolygonPathVec2 path)
        {
            paths.Remove(path);
        }

        public int PathCount()
        {
            return paths.Count;
        }

        public PolygonPathVec2 GetPath(int index)
        {
            return paths[index];
        }

        public bool Composes(Vector2 node)
        {
            foreach (var path in paths)
                if (path.Composes(node))
                    return true;
            return false;
        }

        public bool Composes(PolygonPathVec2 path)
        {
            if (paths.Contains(path))
                return true;

            foreach (var path2 in paths)
                if (path2.Equivalents(path))
                    return true;

            return false;
        }

        public Side GetSide(PolygonPathVec2 other)
        {
            int e = 0;
            int i = 0;
            int o = 0;
            other.ForEachNode(p => {
                Side s = GetSide(p);
                if (s == Side.Edge) e++;
                if (s == Side.In) i++;
                if (s == Side.Out) o++;
            });

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
        public Side GetSide(Vector2 node)
        {
            if (Composes(node))
                return Side.Edge;

            var ipoints = RayCastOnPaths(node);
            return ipoints.Count % 2 == 0 ? Side.Out : Side.In;
        }

        public Vector2 GetOutsidePointForRayCast(float xref)
        {
            float x = xref;
            bool ok = true;

            int tryCount = 10;
            do
            {
                ok = true;
                ForEachPath(p => p.ForEachNode(n => {
                    if (Mathf.Abs(n.x - x) < GeometryMath.Epsilon)
                    {
                        x += 0.01f;
                        ok = false;
                    }
                }));
                if (tryCount-- == 0)
                {
                    Debug.LogWarning("Polygon: Failed to find a good outside point");
                    break;
                }
            }
            while (!ok);

            return new Vector2(x, Bounds().min.y - 5.0f);
        }

        public List<KeyValuePair<Vector2, PolygonPathVec2>> RayCastOnPaths(Vector2 node)
        {
            List<KeyValuePair<Vector2, PolygonPathVec2>> ret = new List<KeyValuePair<Vector2, PolygonPathVec2>>();

            // Vector2 ext = Bounds().min - new Vector2(-7.49215f, -3.345612f);
            Vector2 ext = GetOutsidePointForRayCast(node.x);
            Vector2 ipt = new Vector2();

            foreach (var path in paths)
            {
                if (path.Composes(node))
                {
                    ret.Add(new KeyValuePair<Vector2, PolygonPathVec2>(node, path));
                    continue;
                }

                path.ForEachSegment((p1, p2) => {
                    if (GeometryMath.SegToSeg(ext, node, p1, p2, ref ipt) == ISeg.Yes)
                        ret.Add(new KeyValuePair<Vector2, PolygonPathVec2>(ipt, path));
                });
            }

            ret.Sort((a, b) => {
                return Vector2.Distance(node, a.Key) < Vector2.Distance(node, b.Key) ? -1 : 1;
            });

            return ret;
        }

        public PolygonPathSet Offseted(Vector2 oft)
        {
            PolygonPathSet ret = new PolygonPathSet();

            ForEachPath(path => ret.AddPath(path.Offseted(oft)));

            return ret;
        }
        public PolygonPathSet Inversed(PolygonPathVec2 dom = null)
        {
            PolygonPathSet ret = new PolygonPathSet();
            ForEachPath(p => {
                PolygonPathVec2 path = new PolygonPathVec2();
                p.ForEachNode(node => path.GetNodes().Insert(0, node));
                ret.AddPath(path);
            });

            if (dom != null)
                ret.AddPath(dom);

            return ret;
        }

        public PolygonPathSet GetExternalHull()
        {
            PolygonPathSet ret = new PolygonPathSet();
            ForEachPath(p => {
                if (p.IsCW())
                    return;

                bool outside = true;
                ForEachPath(p2 =>
                {
                    if (!outside)
                        return;

                    if (p == p2)
                        return;

                    if (p2.GetSide(p) == Side.In)
                        outside = false;
                });
                if (outside)
                    ret.AddPath(p);
            });

            return ret;
        }
    }
}