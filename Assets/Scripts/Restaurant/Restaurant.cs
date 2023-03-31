using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Restaurant : MonoBehaviour
{
    [SerializeField]
    Table[] tables;
    public Table[] Tables => tables;

    List<Table> freeTables;
    List<RestaurantPersonGroup> queue;

    public RestaurauntViewpoint[] viewPoints;
    public WaitingPoint[] waitingPoints;
    public ServicePoint[] kichtenServicePoints;

    int maxQueueLength;

    public RandomNumberGenerator rng;
    int[] randomSeatOrder;

    Seat[] seats;

    public Seat[] Seats => seats;

    // Start is called before the first frame update
    void Awake()
    {
        tables = GetComponentsInChildren<Table>();
        freeTables = tables.ToList();
        queue = new List<RestaurantPersonGroup>();
        viewPoints = GetComponentsInChildren<RestaurauntViewpoint>();
        waitingPoints = GetComponentsInChildren<WaitingPoint>();
        maxQueueLength = waitingPoints.Length;
        rng = GetComponent<RandomNumberGenerator>();
        seats = tables.SelectMany(t => t.seats).ToArray();
        randomSeatOrder = new int[seats.Length];
        for (int i = 0; i < seats.Length; i++)
        {
            randomSeatOrder[i] = i;
        }

        randomSeatOrder = randomSeatOrder.OrderBy(_ => rng.NextInt()).ToArray();
    }


    public Table GetFreeTable(RestaurantPersonGroup rpg)
    {
        if (freeTables.Count == 0)
        {
            return null;
        }

        var index = rng.NextInt(0, freeTables.Count);
        var table = freeTables[index];
        freeTables.RemoveAt(index);
        table.Reservate(rpg.group);
        return table;
    }

    public WaitingPoint GetWaitingQueue(RestaurantPersonGroup rpg)
    {
        var index = queue.Count;
        if (index >= maxQueueLength)
        {
            return null;
        }

        queue.Add(rpg);
        return waitingPoints[index];
    }

    public void OnTableLeft(Table table)
    {
        freeTables.Add(table);
        if (queue.Count > 0)
        {
            var group = queue.First();
            queue.RemoveAt(0);
            group.TableGotFree();

            // move up the queue.
            for(int i = 0; i < queue.Count; i++)
            {
                if (queue[i] == null)
                    return;

                queue[i].group.MoveTo(waitingPoints[i].transform.position, waitingPoints[i].direction);
            }
        }
    }

    public RestaurauntViewpoint FindClosestViewpoint(Vector3 position)
    {

        var closest = Utils.FindClosestObject(viewPoints, position);

        if (closest == null)
        {
            Logger.LogError("No closest viewpoint found");
            return null;
        }

        return closest;

    }

    public bool MaxQueueLengthReached() => maxQueueLength <= queue.Count;

    int randomSeatIndex;
    public Seat NextRandomFreeSeat() => 
        seats[randomSeatOrder[randomSeatIndex++ % seats.Length]];
}
