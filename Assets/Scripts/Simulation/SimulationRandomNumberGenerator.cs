using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is loaded before other random number generators to guarantee deteministic
public class SimulationRandomNumberGenerator : RandomNumberGenerator
{
    public void Reset()
    {
        seed = default;
        counter = default;
        Init();
    }
}
