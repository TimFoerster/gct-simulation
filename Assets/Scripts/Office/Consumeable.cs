using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Scheduleable : MonoBehaviour
{

    public Floor floor;
    public Collider2D Collider { get; private set; }

    private void Awake()
    {
        floor = GetComponentInParent<Floor>();
        Collider = GetCollider();
        ConsumeableAwake();
    }

    protected abstract Collider2D GetCollider();

    protected abstract void ConsumeableAwake();


    public abstract void OnRecrationEnter(PersonSchedule recreation);

    public abstract void Consumed(PersonSchedule recreation);
}
