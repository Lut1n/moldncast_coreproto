using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VecIntController))]
public class VecIntEditor : Editor
{
    private float unit = 100.0f;

    private void OnSceneGUI()
    {
        VecIntController ctrl = target as VecIntController;

        DrawPolyline(ctrl.lineA, ctrl.lineB);
        DrawDot(ctrl.lineA, 0.01f, Color.white);
        DrawDot(ctrl.lineB, 0.01f, Color.white);
        DrawDot(ctrl.point, 0.01f, Color.white);
        ctrl.lineA = DrawGizmo(ctrl.lineA);
        ctrl.lineB = DrawGizmo(ctrl.lineB);
        ctrl.point = DrawGizmo(ctrl.point);

        float d = 0.0f;
        ctrl.result = VecIntOperation.ComputePointToLine(ctrl.point, ctrl.lineA, ctrl.lineB, 1.0f, 1.0f, ref d);
        ctrl.distance = d;
    }

    private Vector2Int DrawGizmo(Vector2Int v2)
    {
        VecIntController ctrl = target as VecIntController;

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

    private void DrawPolyline(Vector2Int a, Vector2Int b)
    {
        Vector2 af = a;
        Vector2 bf = b;
        Handles.color = Color.white;
        Handles.DrawLine(af / unit, bf / unit);
    }

    private void DrawDot(Vector2Int v2, float size, Color color)
    {
        Vector3 p = new Vector3((float)v2.x / unit, (float)v2.y / unit, 0.0f);
        Handles.color = color;
        Handles.DrawSolidDisc(p, Vector3.forward, 0.04f);
    }
}
