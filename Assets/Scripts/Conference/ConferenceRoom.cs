using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RandomNumberGenerator))]
public class ConferenceRoom : MonoBehaviour
{
    public Talk[] Talks;

    Chair[] chairs;

    public int floor;

    public Chair[] Chairs { get => chairs; }

    public Transform SpeakerPosition;

    int nextChairIndex;
    int[] randomChairAccess;

    RandomNumberGenerator rng;

    void Awake()
    {
        rng = GetComponent<RandomNumberGenerator>();
        chairs = GetComponentsInChildren<Chair>();
        randomChairAccess = new int[chairs.Length];
        for (int i = 0; i < randomChairAccess.Length; i++)
        {
            randomChairAccess[i] = i;
        }
        randomChairAccess = randomChairAccess.OrderBy(_ => rng.NextInt()).ToArray();

    }

    public Chair NextRandomFreeChair()
    {
        return chairs[randomChairAccess[nextChairIndex++] % chairs.Length];
    }
}
