using System.Collections;
using UnityEngine;


public class Workspace : MonoBehaviour
{
    public Floor floor;
    OfficePerson person;

    // Use this for initialization
    void Awake()
    {
        floor = GetComponentInParent<Floor>();
    }

    public bool IsFree => person == null;

    public void SetPerson(OfficePerson op) => person = op;
}
