using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CSGController2 : MonoBehaviour
{
    static public int Unit = 1000;

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

    private MultiPolygon2i ComputePath()
    {
        MultiPolygon2i ret = new MultiPolygon2i();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();
        if (poly)
        {
            ret.Add(poly.GetPoints());
        }
        else if (operands.Count >= 2)
        {
            MultiPolygon2i set1 = operands[0].ComputePath();
            MultiPolygon2i set2 = operands[1].ComputePath();
            
            CSGOperation operation = new CSGOperation();
            var type = GetOperation();

            if (type == OperationType.Union)
                ret = operation.Union2(set1, set2);
            else if (type == OperationType.Intersection)
                ret = operation.Intersection2(set1, set2);
            else if (type == OperationType.Difference)
                ret = operation.Substraction2(set1, set2);
            else if (type == OperationType.Exclusion)
                ret = operation.Exclusion2(set1, set2);
            
            report = operation.GetLastReport();
        }
        return ret;
    }

    public List<MultiPolygon2i> GetEditorPaths()
    {
        List<MultiPolygon2i> ret = new List<MultiPolygon2i>();
        RegularPolygon poly = GetComponent<RegularPolygon>();
        var operands = GetCSGChildren();

        if (poly)
        {
            MultiPolygon2i set = new MultiPolygon2i();
            set.Add(poly.GetPoints());
            ret.Add(set);
        }
        else if (operands.Count >= 2)
        {
            ret.Add(operands[0].ComputePath());
            ret.Add(operands[1].ComputePath());
        }
        return ret;
    }

    public OperationType GetOperation()
    {
        PolygonOperation op = GetComponent<PolygonOperation>();
        if (op != null)
            return op.operation;
        return OperationType.Union;
    }
}
