using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;


public class GenericPersonAi : MonoBehaviour
{
    public Exit exit;

    [SerializeField]
    GameObject selectedGameObject;
    public float inState;

    [SerializeField]
    SpriteRenderer personRenderer;

    [SerializeField]
    protected BoxCollider2D selectionColider;

    INeed[] needs;

    INeed currentNeed;

    public PersonGroup PersonGroup;

    [SerializeField]
    bool left = false;

    public bool Left => left;

    public IPerson PersonInstance { protected get; set; }

    [SerializeField]
    PersonMovement personMovement;

    public BLESender sender;
    public BLEReceiver receiver;

    public int personIndex;

    public float spawnAt;
    public float leaveTime;

    [SerializeField]
    List<Floor> floors = new List<Floor>();

    public void EnterFloor(Floor floor) => floors.Add(floor);
    public void ExitFloor(Floor floor) => floors.Remove(floor);
    public List<Floor> Floors => floors;

    [SerializeField]
    TracingApp app;
    BroadcastHandler broadcastHandler;

    public GenericSimulation simulation;

    private void Awake()
    {
        needs = GetComponents<INeed>();
    }

    public void Init(BroadcastHandler bh, AppHandler appHandler, PersonMovementHandler personMovementHandler, SimulationTime time)
    {
        broadcastHandler = bh;
        app.Init(appHandler, time);
        personMovement.Init(personMovementHandler);
    }

    // Start is called before the first frame update
    protected void Start()
    {
        Logger.Log(transform.name + " start");
        SetSelectedVisible(false);
        if (sender != null)
             broadcastHandler.AddSender(this);
        if (receiver != null)
            broadcastHandler.AddReceiver(receiver);
    }

    public void SetPersonIndex(int index)
    {
        personIndex = index;
        receiver.GlobalIndex = index;
        sender.GlobalIndex = index;
    }

    internal bool IsVisible()
    {
        return personRenderer != null && personRenderer.isVisible;
    }

    public virtual IEnumerable<HasReceivers> ReceiverSearchGroup()
    {
        return floors;
        /*

        if (PersonInstance != null)
            return PersonInstance.ReceiverSearchGroup();

        return FindObjectsOfType<BLEReceiver>();
        */
    }

    public bool CanReachPosition(Vector3 position)
    {
        return personMovement.CanReachPosition(position);
    }

    public void SetSelectedVisible(bool vis)
    {
        selectedGameObject.SetActive(vis);
    }

    public bool IsSelected { get => selectedGameObject.activeSelf; }

    public virtual bool MoveTo(Transform targetPosition)
    {
        return personMovement.SetDestination(targetPosition);
    }

    internal void OnFloorEnter(int number)
    {
        personMovement.OnFloorEnter(number);
    }

    public virtual bool MoveTo(Vector3 targetPosition, int floor)
    {
        return personMovement.SetDestination(targetPosition, floor);
    }

    public virtual void OnEnterExit()
    {
        if (PersonInstance != null)
            PersonInstance.OnEnterExit();

        if (PersonGroup != null)
            PersonGroup.OnMemberDelete(this);

        personMovement.OnEnterExit();
        broadcastHandler.RemoveSender(this);
        app.OnEnterExit();
        gameObject.SetActive(false);
        Logger.Log(transform.name + " left");
        left = true;
        simulation.OnPersonLeft(this);
    }

    public void OnDestroy()
    {
        Logger.Log(transform.name + " destroy");
    }

    public bool CanBeDeleted()
    {
        return 
            !isActiveAndEnabled && 
            left &&
            (sender == null || sender.Synced || !sender.IsRecording) &&
            (receiver == null || receiver.Synced || !receiver.IsRecording) &&
            (app == null || app.Synced || !app.IsRecording);
    }

    public INeed CheckForNeeds()
    {
        if (currentNeed != null)
        {
            return null;
        }

        foreach(var need in needs)
        {
            if (need.HasNeed() && need.CanFullfillNeed())
            {
                MakeMovementDynamic();
                currentNeed = need;
                return need;
            }
        }
        return null;
    }

    public void FullfillNeed()
    {
        currentNeed.FullfillNeed();
    }

    public void CheckAndFullfillNeeds()
    {
        if (CheckForNeeds() != null)
            FullfillNeed();
    }

    bool leaving = false;

    void FixedUpdate()
    {
        if (currentNeed != null && currentNeed.IsNeedCompleted())
        {
            currentNeed.AfterNeedCompleted();
            currentNeed = null;
            return;
        }

        if (currentNeed is null && leaving && 
            (personMovement.remainingDistance <= 0.03f || personMovement.destinationReached) )
        {
            OnEnterExit();
        }

    }


    public INeed HasNeed { get => currentNeed; }

    internal void GoToExit()
    {
        if (exit == null)
            exit = FindObjectOfType<Exit>();

        MoveTo(exit.transform);
        leaving = true;
    }

    internal void GoToExit(int floor)
    {
        if (exit == null)
            exit = FindObjectOfType<Exit>();

        MoveTo(exit.transform.position, floor);
        leaving = true;
    }

    internal void MakeMovementStatic()
    {
        personMovement.MakeStatic();
    }

    internal void MakeMovementDynamic()
    {
        personMovement.MakeDynamic();
    }
}
