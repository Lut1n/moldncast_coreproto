using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PolyLine
{
    public class PolygonOperation
    {
        [System.Serializable]
        public class SerializableDict<K,V>
        {
            [System.Serializable]
            public class Pair
            {
                public K key;
                public V val;

                public Pair(K k, V v)
                {
                    key = k;
                    val = v;
                }
            }

            public List<Pair> pairs;

            public SerializableDict()
            {
                pairs = new List<Pair>();
            }

            public SerializableDict(Dictionary<K, V> dict)
            {
                pairs = new List<Pair>();
                foreach (var s in dict)
                    pairs.Add(new Pair(s.Key, s.Value));
            }
        }

        [System.Serializable]
        public class Report
        {
            public bool bug = false;
            public SerializableDict<SegmentPair, Vector2> intersectionPoints = new SerializableDict<SegmentPair, Vector2>();
            public List<PolygonPathSet> originals = new List<PolygonPathSet>();
            public List<PolygonPathSet> subdivideds = new List<PolygonPathSet>();
            public List<PolygonPathVec2> traversals = new List<PolygonPathVec2>();
        }

        [System.Serializable]
        public class SegmentPair
        {
            public Vector2Int segRef1 = new Vector2Int();
            public Vector2Int segRef2 = new Vector2Int();

            public SegmentPair()
            {
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
            public Dictionary<SegmentPair, Vector2> ipoints2 = new Dictionary<SegmentPair, Vector2>();

            public List<Vector2> GetPoints()
            {
                List<Vector2> points = new List<Vector2>();
                foreach (var s in ipoints2)
                    points.Add(s.Value);
                return points;
            }

            public Vector2 Add(SegmentPair pair, Vector2 p)
            {
                if (!ipoints2.ContainsKey(pair))
                    ipoints2.Add(pair, p);
                return ipoints2[pair];
            }
            public bool Contains(SegmentPair pair)
            {
                return ipoints2.ContainsKey(pair);
            }

            public bool Contains(Vector2 p)
            {
                return ipoints2.ContainsValue(p);
            }

            public Vector2 GetPoint(SegmentPair pair)
            {
                return ipoints2[pair];
            }

            public SegmentPair GetPair(Vector2 p)
            {
                foreach (var item in ipoints2)
                {
                    if (item.Value.Equals(p))
                        return item.Key;
                }
                return null;
            }

            public List<Vector2> GetIPointPolygon1(int p1, int s1)
            {
                List<Vector2> ret = new List<Vector2>();
                foreach(var item in ipoints2)
                {
                    Vector2 ref1 = item.Key.segRef1;
                    if (ref1 == new Vector2(p1,s1))
                        ret.Add(item.Value);
                }
                return ret;
            }
            public List<Vector2> GetIPointPolygon2(int p2, int s2)
            {
                List<Vector2> ret = new List<Vector2>();
                foreach (var item in ipoints2)
                {
                    Vector2 ref2 = item.Key.segRef2;
                    if (ref2 == new Vector2(p2, s2))
                        ret.Add(item.Value);
                }
                return ret;
            }
        }

        public IPointCache ComputeIPoints(PolygonPathSet polygons1, PolygonPathSet polygons2)
        {
            IPointCache ret = new IPointCache();
            Vector2 ipt = new Vector2();

            for (int pathIdx1 = 0; pathIdx1 < polygons1.PathCount(); pathIdx1++)
            {
                PolygonPathVec2 path1 = polygons1.GetPath(pathIdx1);
                path1.ForEachIndex(n1 => {

                    for (int pathIdx2 = 0; pathIdx2 < polygons2.PathCount(); pathIdx2++)
                    {
                        PolygonPathVec2 path2 = polygons2.GetPath(pathIdx2);
                        if (path1.Equivalents(path2))
                            continue;
                        path2.ForEachIndex(n2 =>
                        {
                            if (GeometryMath.SegToSeg(path1.At(n1), path1.At(n1 + 1), path2.At(n2), path2.At(n2 + 1), ref ipt) == ISeg.Yes)
                                ret.Add(new SegmentPair(pathIdx1, n1, pathIdx2, n2), ipt);
                        });
                    }
                });
            }
            return ret;
        }

        public PolygonPathSet SubdivideSegments(PolygonPathSet polygons, IPointCache cache, bool firstPolys = true)
        {
            // var pn = firstPolys ? "A" : "B";
            PolygonPathSet ret = new PolygonPathSet();

            for (int pathIdx = 0; pathIdx < polygons.PathCount(); pathIdx++)
            {
                PolygonPathVec2 path = polygons.GetPath(pathIdx);
                PolygonPathVec2 newPath = new PolygonPathVec2();
                path.ForEachIndex(n =>
                {
                    List<Vector2> toInsert = firstPolys ? cache.GetIPointPolygon1(pathIdx, n) : cache.GetIPointPolygon2(pathIdx, n);
                    // Debug.Log(pn + " path #" + pathIdx + " at " + n + " toInsert size = " + toInsert.Count);
                    Vector2 p0 = path.At(n);
                    Vector2 p3 = path.At(n + 1);
                    toInsert.Sort((p1, p2) => {
                        return Vector2.Distance(p0, p1) < Vector2.Distance(p0, p2) ? -1 : 1;
                    });
                    newPath.PushNode(p0);
                    foreach (var p in toInsert)
                    {
                        // if (toInsert[i] != p0 && toInsert[i] != p3)
                        {
                            newPath.PushNode(p);
                        }
                    }
                });
                ret.AddPath(newPath);
            }

            return ret;
        }

        IPointCache cache;
        PolygonPathSet subdivided1, subdivided2;


        public int DebugNodeCount(PolygonPathSet set)
        {
            int c = 0;
            set.ForEachPath(p => c += p.Count());
            return c;
        }

        public void Initialize(PolygonPathSet polygons1, PolygonPathSet polygons2)
        {
            report = new Report();

            // normalize positions (fixed precision to 0.01)
            // polygons1.ForEachPath(p => p.ForEachIndex(i => p.Set(i, p.At(i))));
            // polygons2.ForEachPath(p => p.ForEachIndex(i => p.Set(i, p.At(i))));
            report.originals.Add(polygons1);
            report.originals.Add(polygons2);

            cache = ComputeIPoints(polygons1, polygons2);
            report.intersectionPoints = new SerializableDict<SegmentPair, Vector2>(cache.ipoints2);

            Debug.Log("cache size : " + cache.ipoints2.Count);
            subdivided1 = SubdivideSegments(polygons1, cache, true);
            subdivided2 = SubdivideSegments(polygons2, cache, false);
            report.subdivideds.Add(subdivided1);
            report.subdivideds.Add(subdivided2);

            CreateToVisitCache(subdivided1, subdivided2);
        }

        public class ToVisitCache
        {
            public struct Item
            {
                public Vector2Int setpath;
                public Vector2 node;
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

            public void AddPointToVisit(int set, int path, Vector2 node)
            {
                Item item; item.setpath = new Vector2Int(set, path); item.node =node;
                if (!toVisit.Contains(item))
                    toVisit.Add(item);
            }

            public void Remove(int set, int path, Vector2 node)
            {
                Item item; item.setpath = new Vector2Int(set, path); item.node = node;
                if (toVisit.Contains(item))
                    toVisit.Remove(item);
            }
        }

        public PolygonPathVec2 PathsTraversal(PolygonPathSet set1, PolygonPathSet set2, int startSet, int startPath, Vector2 node)
        {
            PolygonPathSet[] sets = new PolygonPathSet[2] { set1, set2 };

            int startSeg = sets[startSet].GetPath(startPath).IndexOf(node);

            int currentSet = startSet;
            int currentPath = startPath;
            int currentSeg = startSeg;

            PolygonPathVec2 ret = new PolygonPathVec2();

            int fuse = 1000;

            do
            {
                var p0 = sets[currentSet].GetPath(currentPath).At(currentSeg);
                ret.PushNode(p0);
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
                    currentSeg = sets[currentSet].GetPath(currentPath).IndexOf(p0);
                }

                currentSeg++;
                if (currentSeg == sets[currentSet].GetPath(currentPath).Count())
                    currentSeg = 0;

                if (fuse-- == 0)
                {
                    Debug.LogError("Infinite loop on path traversal");
                    report.bug = true;
                    break;
                }
            }
            while (!(currentSet == startSet && currentPath == startPath && currentSeg == startSeg));

            report.traversals.Add(ret.Copy() as PolygonPathVec2);
            return ret;
        }

        ToVisitCache toVisit;
        Report report;

        public Report GetLastReport()
        {
            return report;
        }

        public void CreateToVisitCache(PolygonPathSet set1, PolygonPathSet set2)
        {
            toVisit = new ToVisitCache();
            for (int pathIdx = 0; pathIdx < set1.PathCount(); pathIdx++)
            {
                PolygonPathVec2 path = set1.GetPath(pathIdx);
                path.ForEachNode(n => toVisit.AddPointToVisit(0, pathIdx, n));
            }
            for (int pathIdx = 0; pathIdx < set2.PathCount(); pathIdx++)
            {
                PolygonPathVec2 path = set2.GetPath(pathIdx);
                path.ForEachNode(n => toVisit.AddPointToVisit(1, pathIdx, n));
            }
        }


        public delegate bool PathFilterHandler(PolygonPathVec2 path);

        public void FilterPaths(PolygonPathSet set, PathFilterHandler handler)
        {
            List<PolygonPathVec2> toRemove = new List<PolygonPathVec2>();
            set.ForEachPath(path => { if (handler(path)) toRemove.Add(path); });
            foreach (var path in toRemove) set.RemovePath(path);
        }

        public void Bake(PolygonPathSet set)
        {
            FilterPaths(set, path => {
                bool outside = true;
                bool remove = false;
                Vector2 p = path.GetInsidePoint();
                var ray = set.RayCastOnPaths(p);

                for (int i = 0; i < ray.Count; i++)
                {
                    if (ray[i].Value == path)
                        continue;

                    Side side = ray[i].Value.GetSide(path);
                    if (side == Side.In)
                    {
                        outside = false;
                        ray[i].Value.ComputeOrientation();
                        path.ComputeOrientation();
                        remove = ray[i].Value.IsCCW() == path.IsCCW();
                        break;
                    }
                }
                return remove || (outside && path.IsCW());
            });
        }

        public PolygonPathSet ApplyPolygonOperation(PolygonPathSet set1, PolygonPathSet set2, Side sideToFilter)
        {
            Initialize(set1, set2);

            // generate result
            int fuse = 1000;
            PolygonPathSet result = new PolygonPathSet();
            while (!toVisit.Empty())
            {
                var next = toVisit.PickToVisit();
                PolygonPathVec2 path = PathsTraversal(subdivided1, subdivided2, next.setpath.x, next.setpath.y, next.node);
                result.AddPath(path);

                if (fuse-- == 0)
                {
                    Debug.LogError("infinite loop in ApplyPolygonOperation");
                    report.bug = true;
                    break;
                }
            }

            // filter paths depending on sideToFilrer
            FilterPaths(result, path =>
            {
                Side sideSet1 = subdivided1.GetSide(path);
                Side sideSet2 = subdivided2.GetSide(path);
                return sideSet1 == sideToFilter || sideSet2 == sideToFilter;
            });

            // remove plain in plain; hole in hole
            Bake(result);
            return result;
        }

        public Rect GetDom(PolygonPathSet set1, PolygonPathSet set2)
        {
            Rect r1 = set1.Bounds();
            Rect r2 = set2.Bounds();

            Rect r = r1;
            r.min = Vector2.Min(r1.min, r2.min);
            r.max = Vector2.Max(r1.max, r2.max);
            // r.size = r.size * 2.0f;
            var c = (r.min + r.max) * 0.5f;
            r.min = c + (r.min - c) * 2.0f;
            r.max = c + (r.max - c) * 2.0f;
            return r;
        }

        public PolygonPathSet Union2(PolygonPathSet set1, PolygonPathSet set2)
        {
            // A + B
            // intersect and remove path inside set1 or set2
            return ApplyPolygonOperation(set1, set2, Side.In);
        }

        public PolygonPathSet Intersection2(PolygonPathSet set1, PolygonPathSet set2)
        {
            // A * B
            // intersect and remove path outside set1 or set2
            return ApplyPolygonOperation(set1, set2, Side.Out);
        }

        public PolygonPathSet Substraction2(PolygonPathSet set1, PolygonPathSet set2)
        {
            // A * !B
            PolygonPathVec2 dom = PolygonPathVec2.FromRect(GetDom(set1,set2));
            return Intersection2(set1, set2.Inversed(dom));
        }

        public PolygonPathSet Exclusion2(PolygonPathSet set1, PolygonPathSet set2)
        {
            // (A + B) * !(A * B)
            PolygonPathSet u = Union2(set1, set2);
            PolygonPathVec2 dom = PolygonPathVec2.FromRect(GetDom(set1, set2));
            PolygonPathSet i = Intersection2(set1, set2).Inversed(dom);
            return Intersection2(u, i);
        }

        public PolygonPathSet Dom(PolygonPathSet set1, PolygonPathSet set2)
        {
            PolygonPathVec2 dom = PolygonPathVec2.FromRect(GetDom(set1, set2));
            PolygonPathSet set = new PolygonPathSet();
            set.AddPath(dom);
            return set;
        }
    }

}