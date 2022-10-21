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
    public List<LinearRing2i> traversals = new List<LinearRing2i>();
}
