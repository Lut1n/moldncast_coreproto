using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

public class CSGController : MonoBehaviour
{
    public PolyLine.PolygonOperation.Report report;

    private List<CSGController> GetCSGChildren()
    {
        List<CSGController> ret = new List<CSGController>();
        for(int i=0; i<transform.childCount; ++i)
        {
            var ctrl = transform.GetChild(i).GetComponent<CSGController>();
            if (ctrl) ret.Add(ctrl);
        }
        return ret;
    }
    
    public List<PolygonPath> GetInputPaths()
    {
        List<PolygonPath> ret = new List<PolygonPath>();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();

        if (poly)
            ret.Add(poly.GetPoints());
        else
        {
            foreach(var ctrl in operands)
                ret.AddRange(ctrl.GetPaths());
        }
        return ret;
    }

    public List<PolygonPath> GetPaths()
    {
        List<PolygonPath> ret = new List<PolygonPath>();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();

        if (poly)
            ret.Add(poly.GetPoints());
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
