using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LiftController))]
public class OfficeBuilding : MonoBehaviour
{

    Floor[] floors;
    Floor exitFloor;

    LiftController liftController;

    void Awake()
    {
        liftController = GetComponentInChildren<LiftController>();
        floors = GetComponentsInChildren<Floor>();
        foreach(var f in floors)
        {
            if (f.number != 0) continue;

            exitFloor = f;
            break;
        }
    }

    internal Floor ExitFloor => exitFloor;
    internal Floor[] Floors => floors;


}
