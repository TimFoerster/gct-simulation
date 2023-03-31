using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public struct ParkWaypoint
{
    public Vector3 destination;
    public Transform transform;
    public float waitFor;
}
