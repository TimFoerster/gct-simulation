using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConferenceBuilding : MonoBehaviour
{
    [Serializable]
    sealed class FloorEntries
    {
        [SerializeField]
        public IdleArea[] foyers;

        [SerializeField]
        public Seat[] seats;

        public FloorEntries(IdleArea[] foyers, Seat[] seats)
        {
            this.foyers = foyers;
            this.seats = seats;
        }
    }

    Floor[] floors;

    [SerializeField]
    FloorEntries[] floorEntries;

    public IdleArea[] Foyers(int floorIndex) => floorEntries[floorIndex].foyers;

    public Seat[] StandingPositions(int floorIndex) => floorEntries[floorIndex].seats;




    void Awake()
    {
        floors = FindObjectsOfType<Floor>().OrderBy(f => f.number).ToArray();
        floorEntries = floors
            .Select(f =>
                new FloorEntries(
                    f.GetComponentsInChildren<IdleArea>()
                    .Where(area => area.areaType == IdleArea.AreaType.Foyer)
                    .ToArray(),
                    f.GetComponentsInChildren<Seat>()
                )
            ).ToArray();
    }

}
