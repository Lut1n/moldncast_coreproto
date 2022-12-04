using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DebugReport
{
    public bool bug = false;
    public List<Vector2> intersectionPoints = new List<Vector2>();
    public List<MultiPolygon2i> originals = new List<MultiPolygon2i>();
    public List<MultiPolygon2i> subdivideds = new List<MultiPolygon2i>();
    public List<RingInt> traversals = new List<RingInt>();
    public List<PointInfo> pointInfos = new List<PointInfo>();
    public List<RingInt> indexed1 = new List<RingInt>();
    public List<RingInt> indexed2 = new List<RingInt>();
    public List<RingInt> indexedResult = new List<RingInt>();
}
