using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RegularPolygon : MonoBehaviour
{
    public int sideCount = 3;
    public float size = 1.0f;
    public float rotation = 0.0f;

    public CSGController2.PolygonPath GetPoints(bool scaled = false)
    {
        float a = rotation;
        float step = Mathf.PI * 2.0f / sideCount;

        CSGController2.PolygonPath ret = new CSGController2.PolygonPath();
        for(int i=0; i<sideCount; ++i)
        {
            Vector2 pt = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size;
            // pt = scaled ? pt * CSGController2.Unit : pt;
            ret.Add(transform.TransformPoint(pt));
            a += step;
        }
        return ret;
    }
}
