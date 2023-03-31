using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPerson
{
    public GenericPersonAi person { get; }

    public float Speed { set; }

    public bool OnEnterExit();
}
