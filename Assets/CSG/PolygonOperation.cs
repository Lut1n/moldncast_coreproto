using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonOperation : MonoBehaviour
{
    public enum OperationType
    {
        Union,
        Intersection,
        Difference,
        Exclusion
    }

    public OperationType operation = OperationType.Union;
}
