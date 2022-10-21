using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CSGController2))]
public class CSGController2Editor : Editor
{
    private void OnSceneGUI()
    {
        CSGController2 csg = target as CSGController2;

        List<CSGController2.PolygonPath> paths = csg.GetPaths();

        if (paths.Count >= 2)
        {
            foreach (var path in csg.GetInputPaths())
                DrawPolyline(path.points, Color.white * 0.3f);
                
            var polyline = Test(paths[0], paths[1], csg.GetOperation());
            for(int i=0; i<polyline.Count(); ++i)
            {
                var line = polyline.Get(i);
                // line.ComputeOrientation();
                if (line.IsCCW())
                    DrawPolyline(line.nodes, Color.blue);
                else
                    DrawPolyline(line.nodes, Color.red);
            }
        }
        else
        {
            foreach (var path in paths)
                DrawPolyline(path.points, Color.white);
        }
    }

    private void DrawDot(Vector3 p, float size, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(p, Vector3.forward, 0.04f);
    }
    
    private void DrawPolyline(List<Vector2> points, Color color)
    {
        if (points.Count < 3) return;

        Vector3 last = points[points.Count - 1];
        DrawDot(last, 0.02f, color);

        foreach (var p in points)
        {
            Vector3 curr = p;
            DrawDot(curr, 0.02f, color);
            Handles.color = color;
            Handles.DrawLine(last, curr);
            last = curr;
        }
    }


    private void DrawPolyline(List<Vector2Int> points, Color color)
    {
        if (points.Count < 3) return;

        Vector3 last = (Vector2)points[points.Count - 1] / CSGController2.Unit;
        DrawDot(last, 0.02f, color);

        foreach (var p in points)
        {
            Vector3 curr = (Vector2)p / CSGController2.Unit;
            DrawDot(curr, 0.02f, color);
            Handles.color = color;
            Handles.DrawLine(last, curr);
            last = curr;
        }
    }

    private MultiPolygon2i Convert(CSGController2.PolygonPath path)
    {
        LinearRing2i polyline = new LinearRing2i();
        foreach(var v in path.points)
        {
            polyline.nodes.Add(Vector2Int.RoundToInt(v * CSGController2.Unit));
        }

        MultiPolygon2i set = new MultiPolygon2i();
        polyline.ComputeOrientation();
        set.Add(polyline);
        return set;
    }

    private MultiPolygon2i Test(CSGController2.PolygonPath path1, CSGController2.PolygonPath path2, PolygonOperation.OperationType type)
    {
        MultiPolygon2i set1 = Convert(path1);
        MultiPolygon2i set2 = Convert(path2);

        CSGOperation operation = new CSGOperation();

        MultiPolygon2i ret =  new MultiPolygon2i();

        if (type == PolygonOperation.OperationType.Union)
            ret = operation.Union2(set1, set2);
        else if (type == PolygonOperation.OperationType.Intersection)
            ret = operation.Intersection2(set1, set2);
        else if (type == PolygonOperation.OperationType.Difference)
            ret = operation.Substraction2(set1, set2);
        else if (type == PolygonOperation.OperationType.Exclusion)
            ret = operation.Exclusion2(set1, set2);

        CSGController2 csg = target as CSGController2;
        var report = operation.GetLastReport();
        //if (report.bug && (csg.report == null || csg.report.bug == false))
            csg.report = report;
        return ret;
    }
}