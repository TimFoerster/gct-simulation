using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PersonGroup))]
public class DummyPersonGroup : MonoBehaviour
{
    PersonGroup personGroup;

    private void Awake()
    {
        personGroup = GetComponent<PersonGroup>();
    }
}
