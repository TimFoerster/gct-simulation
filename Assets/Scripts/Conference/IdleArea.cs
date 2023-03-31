using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Collider2D))]
public class IdleArea : MonoBehaviour
{
    public enum AreaType
    {
        Outside,
        Foyer,
        Room
    }

    public AreaType areaType;

    Collider2D col;
    public Collider2D Collider { get => col; }

    // Start is called before the first frame update
    void Awake()
    {
        col = GetComponent<Collider2D>();
    }

}
