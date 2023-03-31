using System;
using UnityEngine;

internal interface RTSUnit
{
    internal void SetSelectedVisible(bool v);
    void MoveTo(Vector3 vector3, int v);
}