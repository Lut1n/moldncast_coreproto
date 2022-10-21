using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CSGController))]
public class CSGControllerEditor : Editor
{
    private void OnSceneGUI()
    {
        CSGController csg = target as CSGController;

        List<PolygonPath> paths = csg.GetPaths();

        if (paths.Count >= 2)
        {
            foreach (var path in csg.GetInputPaths())
                DrawPolyline(path.points, Color.white * 0.3f);
                
            var polyline = Test(paths[0], paths[1], csg.GetOperation());
            for(int i=0; i<polyline.PathCount(); ++i)
            {
                var line = polyline.GetPath(i);
                line.ComputeOrientation();
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

    private PolyLine.PolygonPathSet Convert(PolygonPath path)
    {
        PolyLine.PolygonPathVec2 polyline = new PolyLine.PolygonPathVec2();
        polyline.nodes.AddRange(path.points);

        PolyLine.PolygonPathSet set = new PolyLine.PolygonPathSet();
        set.AddPath(polyline);
        return set;
    }

    private PolyLine.PolygonPathSet Test(PolygonPath path1, PolygonPath path2, PolygonOperation.OperationType type)
    {
        PolyLine.PolygonPathSet set1 = Convert(path1);
        PolyLine.PolygonPathSet set2 = Convert(path2);

        PolyLine.PolygonOperation operation = new PolyLine.PolygonOperation();

        PolyLine.PolygonPathSet ret =  new PolyLine.PolygonPathSet();

        if (type == PolygonOperation.OperationType.Union)
            ret = operation.Union2(set1, set2);
        else if (type == PolygonOperation.OperationType.Intersection)
            ret = operation.Intersection2(set1, set2);
        else if (type == PolygonOperation.OperationType.Difference)
            ret = operation.Substraction2(set1, set2);
        else if (type == PolygonOperation.OperationType.Exclusion)
            ret = operation.Exclusion2(set1, set2);

        CSGController csg = target as CSGController;
        var report = operation.GetLastReport();
        //if (report.bug && (csg.report == null || csg.report.bug == false))
            csg.report = report;
        return ret;
    }
}
