using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RestaurauntViewpoint : MonoBehaviour, HasBounds
{
    Collider2D col;
    void Awake()
    {
        col = GetComponent<Collider2D>();
    }
    public Bounds Bounds() => col.bounds;

}
