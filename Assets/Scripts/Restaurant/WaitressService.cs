using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class WaitressService : MonoBehaviour
{
    [SerializeField]
    GameObject serviceArea;

    [SerializeField, ReadOnly]
    public ServicePoint[] servicePoints;

    [SerializeField]
    List<ServicePoint> serviceQueue;

    private void Awake()
    {
        servicePoints = serviceArea.GetComponentsInChildren<ServicePoint>();
    }

    public void Enqueue(ServicePoint sp)
    {
        serviceQueue.Add(sp);
    }   
    
    public ServicePoint Dequeue()
    {
        if (QueueCount == 0)
            return null;

        var sp = serviceQueue[0];
        serviceQueue.RemoveAt(0);
        return sp;
    }

    public int QueueCount { get => serviceQueue.Count; }

}
