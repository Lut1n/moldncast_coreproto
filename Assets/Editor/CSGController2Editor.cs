using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CSGController2))]
public class CSGController2Editor : Editor
{
    List<int> nodeLabels = new List<int>();
    List<int> nodeDots = new List<int>();

    private void OnSceneGUI()
    {
        CSGController2 csg = target as CSGController2;

        List<MultiPolygon2i> paths = csg.GetEditorPaths();

        if (paths.Count >= 2)
        {
            var polyline = ComputePath(paths[0], paths[1], csg.GetOperation());
            nodeLabels.Clear();

            var report = csg.report;
            foreach(var set in report.subdivideds)
                DrawRingIntSet(set, Color.white * 0.3f, report.pointInfos);

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
            for(int i=0; i<paths.Count; ++i)
            {
                for (int j = 0; j<paths[i].Count(); ++j)
                    DrawPolyline(paths[i].Get(j).nodes, Color.white);
            }
        }

        DrawLabels();
    }

    private void DrawLabels()
    {
        CSGController2 csg = target as CSGController2;
        var report = csg.report;
        var infos = report.pointInfos;

        for(int i=0; i<infos.Count; ++i)
        {
            if (!nodeLabels.Contains(i))
            {
                Vector3 pos = (Vector2)infos[i].value / CSGController2.Unit;
                Handles.Label(pos, "node #" + i);
                nodeLabels.Add(i);
            }
        }
    }

    private void DrawDot(Vector3 p, float size, Color color)
    {
        Handles.color = color;
        Handles.DrawSolidDisc(p, Vector3.forward, 0.04f);
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
    
    private void DrawRingIntSet(RingIntSet set, Color color, List<PointInfo> infos)
    {
        for(int i=0; i<set.rings.Count; ++i)
        {
            var ring = set.rings[i];
            if (ring.nodes.Count < 3) continue;

            int lastId = ring.nodes[ring.nodes.Count - 1];
            Vector2Int lastValue = infos[lastId].value;

            Vector3 last = (Vector2)lastValue / CSGController2.Unit;
            DrawDot(last, 0.02f, color);

            foreach (var id in ring.nodes)
            {
                Vector2Int value = infos[id].value;
                Vector3 curr = (Vector2)value / CSGController2.Unit;
                DrawDot(curr, 0.02f, color);
                Handles.color = color;
                Handles.DrawLine(last, curr);
                last = curr;
            }
        }
    }

    private MultiPolygon2i ComputePath(MultiPolygon2i set1, MultiPolygon2i set2, OperationType type)
    {
        CSGOperation operation = new CSGOperation();

        MultiPolygon2i ret =  new MultiPolygon2i();

        if (type == OperationType.Union)
            ret = operation.Union2(set1, set2);
        else if (type == OperationType.Intersection)
            ret = operation.Intersection2(set1, set2);
        else if (type == OperationType.Difference)
            ret = operation.Substraction2(set1, set2);
        else if (type == OperationType.Exclusion)
            ret = operation.Exclusion2(set1, set2);

        CSGController2 csg = target as CSGController2;
        csg.report = operation.GetLastReport();

        return ret;
    }
}
