using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class PersonMovement : MonoBehaviour
{
    public enum State
    {
        Idle,
        Moving,
        Reached
    }

    public State state;

    public Vector3 destination;
    public Vector3 nextPosition;

    LineRenderer lineRenderer;
    //Rigidbody2D rigibody;
    public NavMeshPath path;

    public int pathIndex;

    // max speed
    public float speed = 1.5f;

    // units / sec^2 
    public float accelartion = 5f;

    // turning speed in deg/s
    public float angularSpeed = 100f;

    public float currentSpeed = 0;

    PersonMovementHandler pmh;
    public int pmhIndex;

    public GenericPersonAi person;

    LevelChangePoint levelChange;
    Vector3 localDestination;



    public int currentFloor;

    // Start is called before the first frame update
    void Awake()
    {
        state = State.Idle;
        lineRenderer = GetComponent<LineRenderer>();
        //rigibody = GetComponent<Rigidbody2D>();
        pmh = FindObjectOfType<PersonMovementHandler>();
        var f = GetComponentInParent<Floor>();
        currentFloor = f is null ? 0 : f.number;
    }

    private void OnEnable()
    {
        if (path == null)
            path = new NavMeshPath();
    }

    public void Init(PersonMovementHandler personMovementHandler)
    {
        this.pmh = personMovementHandler;
    }

    int tries = 0;
    int targetFloor;

    public bool SetDestination(Vector3 target, int floor)
    {
        targetFloor = floor;
        destination = target;
        if (currentFloor == floor)
            return SetDestinationInternal(target);

        levelChange = FindLevelChange(currentFloor, floor);

        if (levelChange is null)
        {
            Logger.LogError("No Level Change found from " + currentFloor + " to " + floor, this);
            return false;
        }

        return SetDestinationInternal(levelChange.startPos);
    }

    LevelChangePoint FindLevelChange(int startFloor, int targetFloor)
    {
        if (startFloor == targetFloor)
            return null;

        var goingUp = startFloor < targetFloor;

        var lcs = pmh.LevelChanges[startFloor][targetFloor];

        return lcs.Length switch
        {
            0 => goingUp ? FindLevelChange(startFloor, targetFloor - 1) : FindLevelChange(startFloor, targetFloor + 1),
            1 => lcs[0],
            _ => lcs.OrderBy(lc => Vector3.Distance(lc.startPos, transform.position)).First(),
        };
    }

    bool SetDestinationInternal(Vector3 target, int tries)
    {

        if (tries > 10) return false;

        var r = SetDestinationInternal(target);
        this.tries = tries;
        return r;
    }

    public bool SetDestination(Transform transform)
    {
        return SetDestination(transform.position, transform.GetComponentInParent<Floor>().number);
    }

    bool SetDestinationInternal(Vector3 target)
    {
        tries = 0;
        MakeDynamic();

        var p = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path);
        if (!p || path.status == NavMeshPathStatus.PathInvalid || path.corners.Length <= 1)
        {
            Logger.LogWarning(transform.name + ": Destination can not be reached", this);
            /*
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { transform.position, target });
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            */
            return false;
        }

        pmh.NewDestination(pmhIndex);
        /*
        lineRenderer.positionCount = path.corners.Length;
        lineRenderer.SetPositions(path.corners);
        */
        pathIndex = 1;
        nextPosition = path.corners[pathIndex];
        state = State.Moving;
        localDestination = target;

        return true;

    }

    bool changingLevel = false;

    public bool PointReached()
    {
        if (state != State.Moving)
        {
            return true;
        }

        pathIndex++;
        if (pathIndex < path.corners.Length)
        {
            nextPosition = path.corners[pathIndex];
            return false;
        }

        float off;

        off = Vector3.Distance(transform.position, localDestination);
        if (off > 0.2)
        {
            if (SetDestinationInternal(localDestination, tries + 1 ))
                return false;

            Logger.LogWarning("Max retries reached, teleporting " + transform.name + " (" + transform.position.x + "; " + transform.position.y + ") -> (" + localDestination.x + "; " + localDestination.y + ")", transform);
            transform.position = localDestination;
        }

        if (levelChange is null)
        {
            state = State.Reached;
            currentSpeed = 0;
            return true;
        }

        if (!changingLevel)
        {
            SetDestinationInternal(levelChange.endPos);
            changingLevel = true;
            return false;
        }

        currentFloor = levelChange.toFloor;
        levelChange = null;
        changingLevel = false;
        SetDestination(destination, targetFloor);
        return false;
    }

    internal void OnEnterExit()
    {
        pmh.Remove(pmhIndex);
    }

    public bool CanReachPosition(Vector3 position)
    {
        NavMeshPath path = new NavMeshPath();
        return NavMesh.CalculatePath(transform.position, position, NavMesh.AllAreas, path);
    }


    public bool Move()
    {
        if (state != State.Moving)
        {
            return true;
        }
        // var position = transform.position;

        Vector3 off = nextPosition - transform.position;
        var dist = off.sqrMagnitude; // math.distancesq(nextPosition, position);

        // further away then 3cm (0.03)^2 
        if (dist > 0.0003)
        //if (dist > 0.01)
        {
            currentSpeed = Mathf.Min(currentSpeed + accelartion * Time.fixedDeltaTime, speed);
            // rigibody.AddForce(direction * speed);
            transform.position = transform.position + off.normalized * currentSpeed * Time.fixedDeltaTime;

            return false;
        }

        // end of path
        transform.position = nextPosition;
        return true;
    }

    internal void ResetPath()
    {
        // lineRenderer.positionCount = 0;
        path.ClearCorners();
        state = State.Idle;
        currentSpeed = 0;
    }

    internal void MakeStatic()
    {
        ResetPath();
        //rigibody.simulated = false;
        //rigibody.bodyType = RigidbodyType2D.Static;
        gameObject.isStatic = true;
    }

    internal void MakeDynamic()
    {
        //rigibody.simulated = true;
        //rigibody.bodyType = RigidbodyType2D.Dynamic;
        gameObject.isStatic = false;
    }

    public void OnFloorEnter(int floor) =>
        currentFloor = floor;

    internal float remainingDistance => (transform.position - destination).magnitude;

    internal bool destinationReached => state == State.Reached;

    public bool IsMoving => state == State.Moving;
}

