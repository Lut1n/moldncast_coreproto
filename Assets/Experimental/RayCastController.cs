using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastController : MonoBehaviour
{
    public List<Vector2Int> path;
    public int subdiv = 0;
    public Vector2Int origin, point;

    public float distance = 0.0f;
    public int ilines = 0;
    public int ipoints = 0;
    public VecIntOperation.Result result = VecIntOperation.Result.Undefined;
    public bool autoRay = false;
}
