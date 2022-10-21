using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSGController2 : MonoBehaviour
{
    static public int Unit = 10;

    public class PolygonPath
    {
        public List<Vector2> points;

        public PolygonPath()
        {
            points = new List<Vector2>();
        }

        public PolygonPath(List<Vector2> path)
        {
            points = path;
        }

        public void Add(Vector2 point)
        {
            points.Add(point);
        }
    }

    public DebugReport report;

    private List<CSGController2> GetCSGChildren()
    {
        List<CSGController2> ret = new List<CSGController2>();
        for(int i=0; i<transform.childCount; ++i)
        {
            var ctrl = transform.GetChild(i).GetComponent<CSGController2>();
            if (ctrl) ret.Add(ctrl);
        }
        return ret;
    }
    
    public List<CSGController2.PolygonPath> GetInputPaths()
    {
        List<CSGController2.PolygonPath> ret = new List<CSGController2.PolygonPath>();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();

        if (poly)
        {
            ret.Add(new PolygonPath(poly.GetPoints(true).points));
        }
        else
        {
            foreach(var ctrl in operands)
                ret.AddRange(ctrl.GetPaths());
        }
        return ret;
    }

    public List<CSGController2.PolygonPath> GetPaths()
    {
        List<CSGController2.PolygonPath> ret = new List<CSGController2.PolygonPath>();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();

        if (poly)
            ret.Add(new PolygonPath(poly.GetPoints(true).points));
        else
        {
            foreach(var ctrl in operands)
                ret.AddRange(ctrl.GetPaths());
        }
        return ret;
    }

    public PolygonOperation.OperationType GetOperation()
    {
        PolygonOperation op = GetComponent<PolygonOperation>();
        if (op != null)
            return op.operation;
        return PolygonOperation.OperationType.Union;
    }
}
