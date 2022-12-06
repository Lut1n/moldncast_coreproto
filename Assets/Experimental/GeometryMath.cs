using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ISeg
{
    No,
    Yes,
    Edge
}

public class GeometryMath
{
    public static float Epsilon = 0.01f;


    public static ISeg SegToSegf(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, ref Vector2 ipt)
    {
        if (a1 == b1 || a1 == b2)
        {
            ipt = a1;
            return ISeg.Edge;
        }
        if (a2 == b1 || a2 == b2)
        {
            ipt = a2;
            return ISeg.Edge;
        }

        Vector2 p12 = a2 - a1;
        Vector2 a = b1 - a1;
        Vector2 b = b2 - a1;

        Vector2 tan = p12;
        tan.Normalize();

        Vector2 bitan = new Vector2(-tan.y, tan.x);

        float da = Vector3.Dot(a, bitan);
        float db = Vector3.Dot(b, bitan);
        if (Mathf.Sign(da) != Mathf.Sign(db)) // one on each side
        {
            float t = da / (da - db);
            ipt = Vector2.Lerp(a, b, t);

            float di = Vector3.Dot(ipt, tan);
            if (di > 0.0 && di < p12.magnitude)
            {
                ipt = a1 + ipt;
                if (ipt == a1 || ipt == a2 || ipt == b1 || ipt == b2)
                    return ISeg.Edge;
                else
                    return ISeg.Yes;
            }
        }

        return ISeg.No;
    }
    
    public static ISeg SegToSegi(Vector2Int a1i, Vector2Int a2i, Vector2Int b1i, Vector2Int b2i, ref Vector2Int ipt)
    {
        Vector2 a1 = a1i;
        Vector2 a2 = a2i;
        Vector2 b1 = b1i;
        Vector2 b2 = b2i;

        if (a1i == b1i || a1i == b2i)
        {
            ipt = a1i;
            return ISeg.Edge;
        }
        if (a2i == b1i || a2i == b2i)
        {
            ipt = a2i;
            return ISeg.Edge;
        }

        Vector2 p12 = a2 - a1;
        Vector2 a = b1 - a1;
        Vector2 b = b2 - a1;

        Vector2 tan = p12;
        tan.Normalize();

        Vector2 bitan = new Vector2(-tan.y, tan.x);

        float da = Vector3.Dot(a, bitan);
        float db = Vector3.Dot(b, bitan);
        if (Mathf.Sign(da) != Mathf.Sign(db)) // one on each side
        {
            float t = da / (da - db);
            Vector2 iptf = Vector2.Lerp(a, b, t);

            float di = Vector3.Dot(iptf, tan);
            if (di > 0.0 && di < p12.magnitude)
            {
                ipt = Vector2Int.RoundToInt(a1 + iptf);
                if (ipt == a1i || ipt == a2i || ipt == b1i || ipt == b2i)
                    return ISeg.Edge;
                else
                    return ISeg.Yes;
            }
        }

        return ISeg.No;
    }
}