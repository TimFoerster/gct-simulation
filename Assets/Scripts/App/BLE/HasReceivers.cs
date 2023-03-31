using System.Collections.Generic;
using UnityEngine;

public interface HasReceivers
{
    public List<BLEReceiver> Receivers { get; }

    public void NewReceiver(BLEReceiver obj);
    public void RemoveReceiver(BLEReceiver obj);
    public bool ContainsReceiver(BLEReceiver obj);

    public Transform Persons { get; }
}