using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandingArea : MonoBehaviour
{
    ushort amount = 0;
    BoxCollider2D areaCollider;

    public Bounds Bounds { get => areaCollider.bounds; }

    private void Awake()
    {
        areaCollider = GetComponent<BoxCollider2D>();
    }
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Increase()
    {
        amount++;
    }

}
