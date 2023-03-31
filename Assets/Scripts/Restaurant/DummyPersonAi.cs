using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyPersonAi : MonoBehaviour, IPerson
{
    PersonMovement personMovement;
    GenericPersonAi gpa;

    private void Awake()
    {
        personMovement = GetComponent<PersonMovement>();
        gpa = GetComponent<GenericPersonAi>();
    }

    void Start()
    {
        person.GoToExit();
    }

    public GenericPersonAi person { get => gpa; set => gpa = value; }
    public float Speed { set => personMovement.speed = value; }

    public bool OnEnterExit()
    {
        return true;
    }
}
