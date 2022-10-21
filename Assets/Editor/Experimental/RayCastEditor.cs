using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RayCastController))]
public class RayCastEditor : Editor
{
    private float unit = 100.0f;

    private void OnSceneGUI()
    {
        RayCastController ctrl = target as RayCastController;

        for(int i=0; i<ctrl.path.Count; ++i)
            ctrl.path[i] = DrawGizmo(ctrl.path[i]);
        ctrl.origin = DrawGizmo(ctrl.origin);
        ctrl.point = DrawGizmo(ctrl.point);


        DrawPolyline(ctrl.path, ctrl.subdiv, Color.white);
        DrawDot(ctrl.origin, 0.01f, Color.white);
        DrawDot(ctrl.point, 0.01f, Color.white);

        Handles.color = Color.red;
        Handles.DrawLine((Vector2)(ctrl.origin) / unit, (Vector2)(ctrl.point) / unit);

        List<VecIntOperation.Line> iLines = new List<VecIntOperation.Line>();
        List<Vector2Int> iPoints = new List<Vector2Int>();

        float d = 0.0f;
        ctrl.result = VecIntOperation.PolygonRayCast(ctrl.origin, ctrl.point, ctrl.path, ctrl.subdiv, ref iLines, ref iPoints);
        ctrl.ilines = iLines.Count;
        ctrl.ipoints = iPoints.Count;
        ctrl.distance = d;
        
        foreach(var l in iLines)
        {
            Handles.color = Color.blue;
            Handles.DrawLine((Vector2)(l.a) / unit, (Vector2)(l.b) / unit);
        }

        foreach(var p in iPoints)
        {
            DrawDot(p, 0.02f, Color.blue);
        }
    }

    private Vector2Int DrawGizmo(Vector2Int v2)
    {
        RayCastController ctrl = target as RayCastController;

        Vector3 p = new Vector3((float)v2.x / unit, (float)v2.y / unit, 0.0f);
        EditorGUI.BeginChangeCheck();
        p = Handles.DoPositionHandle(p, Quaternion.identity);   // draw
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(ctrl, "Move Point");
            EditorUtility.SetDirty(ctrl);
            v2 = new Vector2Int((int)(p.x * unit), (int)(p.y * unit));
        }
        return v2;

    }

    private void DrawPolyline(List<Vector2Int> points, int subdiv, Color color)
    {
        if (points.Count < 3) return;

        Vector2 last = points[points.Count - 1];
        DrawDot(last, 0.02f, color);

        foreach (var p in points)
        {
            Vector2 curr = p;
            DrawDot(curr, 0.02f, color);
            Handles.color = color;
            Handles.DrawLine(last / unit, curr / unit);

            for(int i=0; i<subdiv; ++i)
            {
                float f = (i + 1.0f) / (subdiv + 1.0f);
                DrawDot(Vector2.Lerp(last, curr, f), 0.02f, Color.grey);
            }

            last = curr;
        }
    }

    private void DrawDot(Vector2 v2, float size, Color color)
    {
        Vector3 p = new Vector3((float)v2.x / unit, (float)v2.y / unit, 0.0f);
        Handles.color = color;
        Handles.DrawSolidDisc(p, Vector3.forward, 0.04f);
    }
}
