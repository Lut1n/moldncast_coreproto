using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VecIntController : MonoBehaviour
{
    public Vector2Int lineA, lineB;
    public Vector2Int point;

    public float distance = 0.0f;
    public VecIntOperation.Result result = VecIntOperation.Result.Undefined;
}
