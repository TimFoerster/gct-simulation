using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class LevelChangePoint
{
    public int fromFloor;
    public int toFloor;

    public Vector3 startPos;
    public Vector3 endPos;

    public LevelChange levelChange;
}

public class LevelChange : MonoBehaviour
{
    public Floor FromFloor;
    public Floor ToFloor;

    NavMeshLink link;

    void Awake()
    {
        link = GetComponent<NavMeshLink>();
    }

    public Vector3 Start => link.startPoint;
    public Vector3 End => link.endPoint;

    public bool Bidirectional => link.bidirectional;

    public LevelChangePoint LevelChangePoint(bool reverse = false) =>
        new LevelChangePoint
        {
            fromFloor = reverse ? ToFloor.number : FromFloor.number,
            toFloor = reverse ? FromFloor.number : ToFloor.number,
            startPos = transform.position +  (reverse ? End : Start),
            endPos = transform.position + (reverse ? Start : End),
            levelChange = this
        };
}
