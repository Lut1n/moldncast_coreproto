using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RegularPolygon : MonoBehaviour
{
    public int sideCount = 3;
    public float size = 1.0f;
    public float rotation = 0.0f;

    public LinearRing2i GetPoints()
    {
        float a = rotation;
        float step = Mathf.PI * 2.0f / sideCount;

        LinearRing2i ret = new LinearRing2i();
        for(int i=0; i<sideCount; ++i)
        {
            Vector2 pt = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size;
            ret.Add(Vector2Int.RoundToInt(transform.TransformPoint(pt) * CSGController2.Unit));
            a += step;
        }
        ret.ComputeOrientation();
        return ret;
    }
}
