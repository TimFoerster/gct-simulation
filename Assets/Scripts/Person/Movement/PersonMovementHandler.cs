using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PersonMovementHandler : MonoBehaviour
{
    LevelChangePoint[][][] levelChanges;

    public LevelChangePoint[][][] LevelChanges => levelChanges;

    [SerializeField]
    PersonMovement[] personMovementList;

    [SerializeField]
    List<int> activeMovements = new List<int>();


    public enum State
    {
        Dynamic,
        Static
    }

    public State state;

    BroadcastHandler broadcastHandler;

    private void Awake()
    {
        broadcastHandler = FindObjectOfType<BroadcastHandler>();
    }

    void OnEnable()
    {
        CreateLevelMatrix();
        Init();
    }

    bool started;

    void Start()
    {
        started = true;
        CreateLevelMatrix();
    }

    void CreateLevelMatrix()
    {
        if (!started)
            return;

        var floors = FindObjectsOfType<Floor>();

        var lcs = FindObjectsOfType<LevelChange>();

        levelChanges = new LevelChangePoint[floors.Length][][];

        for (int i = 0; i < floors.Length; i++)
        {
            levelChanges[i] = new LevelChangePoint[floors.Length][];
            for (int j = 0; j < floors.Length; j++)
            {
                levelChanges[i][j] = lcs.Where(l => 
                    l.FromFloor.number == i && 
                    l.ToFloor.number == j || (
                        l.Bidirectional && 
                        l.FromFloor.number == j && 
                        l.ToFloor.number == i
                    )
                ).Select(l => l.LevelChangePoint( l.FromFloor.number == j))
                .ToArray();
            }
        }

    }

    void FixedUpdate()
    {
        if (activeMovements.Count == 0) return;
        // serial version
        var removeAble = new List<int>();

        for (int i = 0; i < activeMovements.Count; i++)
        {
            if (personMovementList[activeMovements[i]].Move() && 
                personMovementList[activeMovements[i]].PointReached())
            {
                removeAble.Add(i);
            }
        }

        for (int i = removeAble.Count - 1; i >= 0; i--)
        {
            activeMovements.RemoveAt(removeAble[i]);
        }

        if (activeMovements.Count == 0)
        {
            state = State.Static;
        }
    }

    internal void Remove(int pmhIndex)
    {
        activeMovements.Remove(pmhIndex);
    }

    public void SetPersons(PersonMovement[] pm) {
        personMovementList = pm;
        Init();
    }

    public void Init()
    {
        
        if (personMovementList == null) return;

        for (int i = 0; i < personMovementList.Length; i++)
        {
            personMovementList[i].pmhIndex = i;
        }

    }

    public void NewDestination(int index) 
    {
        if (!activeMovements.Contains(index))
            activeMovements.Add(index);

        if (state == State.Static)
        {
            state = State.Dynamic;
            broadcastHandler.MovementStateDynamic();
        }

    }

}
