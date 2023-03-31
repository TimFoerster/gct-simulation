using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static BLESender;

public class BroadcastHandler : MonoBehaviour
{
    [Serializable]
    public class ColliderDictionary : SerializableDictionary<Collider2D, BLEReceiver> { }

    [SerializeField]
    ColliderDictionary dict;

    Floor[] floors;

    // Persons
    [SerializeField]
    GenericPersonAi[] persons;

    [SerializeField]
    TimeslotEntries<GenericPersonAi>[] activePersons;

    // Beacons per floor
    BLEReceiver[] beacons;

    LayerMask bleLayer;
    PersonMovementHandler pmh;

    Collider2D[] overlaps;

    Receiver[][] staticReceiver;

    enum State
    {
        Static,
        Dynamic
    }

    List<Receiver> receivers = new List<Receiver>();
    BLEReceiver receiver;

    int senderTimeslotIndex;

    public int maxSenderTimeslot { get; private set; }

    State[] timeslotState;

    List<GenericPersonAi> prefetch;

    float receiveAccuracy;


    private void Awake()
    {
        prefetch = new List<GenericPersonAi>();
        floors = FindObjectsOfType<Floor>();
        bleLayer = LayerMask.GetMask("BLE");
        pmh = FindObjectOfType<PersonMovementHandler>();

        var settings = SimulationSettings.Instance;
        maxSenderTimeslot = (int)(settings.BroadcastInterval / Time.fixedDeltaTime);

        timeslotState = new State[maxSenderTimeslot];
    }

    private void Start()
    {
        beacons = floors.SelectMany(f => f.Receivers
            .Where(r => r.GetComponent<BLESender>() is null)
        ).ToArray();

        foreach (var beacon in beacons)
        {
            dict.Add(beacon.GetComponent<CircleCollider2D>(), beacon.GetComponent<BLEReceiver>());
        }
    }


    void FixedUpdate()
    {
        // Person is not 
        foreach (var person in activePersons[senderTimeslotIndex])
        {
            IteratePerson(person);
        }

        if (pmh.state == PersonMovementHandler.State.Static && timeslotState[senderTimeslotIndex] == State.Dynamic)
        {
            timeslotState[senderTimeslotIndex] = State.Static;
        }

        senderTimeslotIndex = (senderTimeslotIndex + 1) % maxSenderTimeslot;
    }

    public void MovementStateDynamic()
    {
        for(int i = 0; i < maxSenderTimeslot; i++)
        {
            timeslotState[i] = State.Dynamic;
        }
    }


    void IteratePerson(GenericPersonAi person)
    {

        if (pmh.state == PersonMovementHandler.State.Static &&
            timeslotState[senderTimeslotIndex] == State.Static &&
            staticReceiver[person.personIndex] != null)
        {
            person.sender.Broadcast(staticReceiver[person.personIndex], true);
            return;
        }

        overlaps = Physics2D.OverlapCircleAll(person.transform.position, 10f, bleLayer);

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (dict.TryGetValue(overlaps[i], out receiver))
            {
                var dist = (receiver.transform.position - person.transform.position).magnitude;
                if (dist <= receiver.range && dist > 0)
                    receivers.Add(new Receiver(receiver, dist));
            }
        }

        person.sender.Broadcast(receivers);

        if (pmh.state == PersonMovementHandler.State.Static)
        {
            staticReceiver[person.personIndex] = receivers.ToArray();
        }

        receivers.Clear();
    }

    public void SetPersons(GenericPersonAi[] persons)
    {
        activePersons = new TimeslotEntries<GenericPersonAi>[maxSenderTimeslot];
        for (int i = 0; i < activePersons.Length; i++)
        {
            activePersons[i] = new TimeslotEntries<GenericPersonAi>();
        }

        staticReceiver = new Receiver[persons.Length][];

        foreach(var person in prefetch)
        {
            AddSender(person);
        }

        prefetch = null;
    }

    public void AddSender(GenericPersonAi person)
    {
        if (activePersons.Length == 0)
        {
            prefetch.Add(person);
            return;
        }
        activePersons[person.sender.Timeslot].Add(person);
    }

    public void RemoveSender(GenericPersonAi person)
    {
        activePersons[person.sender.Timeslot].Remove(person);
    }

    public void AddReceiver(BLEReceiver receiver) {
        dict.Add(receiver.GetComponent<CircleCollider2D>(), receiver);
    }
}
 