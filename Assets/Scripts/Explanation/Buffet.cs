using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buffet : MonoBehaviour
{

    [SerializeField]
    Transform[] queueTransforms;

    [SerializeField]
    Collider2D bufferCollider;

    [SerializeField]
    Collider2D waitingArea;

    [SerializeField] RandomNumberGenerator rng;

    ExplanationPerson[] queuePerson;
    int nextQueuePosition;

    [SerializeField]
    List<ExplanationPerson> waitingList;

    void Awake()
    {
        queuePerson = new ExplanationPerson[queueTransforms.Length];
    }

    public Vector3 OnPersonArrive(ExplanationPerson person)
    {
        if (nextQueuePosition >= queueTransforms.Length)
        {
            waitingList.Add(person);
            return Utils.RandomPositionInBounds(waitingArea.bounds, rng);
        }
        
        var pos = queueTransforms[nextQueuePosition].position;
        queuePerson[nextQueuePosition] = person;
        if (nextQueuePosition == 0)
        {
            // person.OnConsumeBuffet();
        }

        nextQueuePosition++;
        return pos;
    }

    public void OnPersonComplete()
    {
        queuePerson[0] = null;

        for(int i = 1; i < nextQueuePosition; i++)
        {
            var newIndex = i - 1;
            queuePerson[newIndex] = queuePerson[i];
            queuePerson[newIndex].person.MoveTo(queueTransforms[newIndex].position, 0);
        }

        nextQueuePosition--;
        queuePerson[nextQueuePosition] = null;

        if (waitingList.Count > 0)
        {
            OnPersonArrive(waitingList[0]);
            waitingList.RemoveAt(0);
        }

        if (queuePerson[0] != null)
        {
            // queuePerson[0].OnConsumeBuffet();
        }

    }

    public Vector3 GetClosestPosition(Vector3 point)
     => bufferCollider.bounds.ClosestPoint(point);
} 
