using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TrainUI : MonoBehaviour
{
    [SerializeField] TextMeshPro nextStation;
    [SerializeField] TextMeshPro info;
    [SerializeField] TrainMovement movement;
    [SerializeField] TrainLogic logic;
    [SerializeField] TrainPhysics physics;
    [SerializeField] TrainCamera cam;
    [SerializeField] Transform overlay;
    GameRTSController rtsController;

    private void Awake()
    {
        rtsController = FindObjectOfType<GameRTSController>();
    }
    // Start is called before the first frame update
    void Start()
    {
        UpdateCamera();
    }

    public void LateUpdate()
    {

        var state = movement.state;
        switch (state)
        {

            case TrainMovement.State.Driving:
            case TrainMovement.State.Arriving:
                info.text = state.ToString() + ": " + Mathf.RoundToInt(movement.speed * 3.6f) + " km/h " + Mathf.RoundToInt(movement.remainingDistance) + " m";
                break;

            case TrainMovement.State.Arrived:
                var tlState = logic.state;
                switch (tlState)
                {
                    case TrainLogic.State.OpenedDoors:
                        info.text = tlState.ToString() + ": " + (logic.inState + logic.waitingTime - Time.time).ToString("0.0") + "s";
                        break;
                    case TrainLogic.State.ClosedDoors:
                        info.text = tlState.ToString();
                        break;
                }
                break;

            case TrainMovement.State.Leaving:
                info.text = state.ToString() + ": " + (physics.movingPersons == null ? "Checking Passangers" : "Waiting for " + physics.movingPersons.Count + " Passangers to sit.");
                break;
        }
    }

    public void UpdateDirection(TrainDirection direction)
    {

        if (direction == TrainDirection.West)
        {
            transform.localPosition = new Vector3(-70, 11, 0);
            transform.Rotate(new Vector3(0, 0, 180f));
        }

    }

    public void NextStop(int trainIndex, Station station)
    {
        nextStation.text = trainIndex + ": " + station.transform.name;
    }

    public void NextStop(int trainIndex)
    {
        nextStation.text = trainIndex + ": End";
    }

    public void DetachMainCamera()
    {
        if (Camera.main != null && Camera.main.transform.parent == transform)
        {
            Camera.main.transform.parent = null;
        }
    }

    public void AttachMainCamera()
    {
        rtsController.followingObject = logic.transform;
        Camera.main.transform.position = cam.transform.position;
        Camera.main.transform.parent = transform;
    }

    public void UpdateCamera()
    {
        if (logic.direction == TrainDirection.West)
        {
            transform.localPosition = new Vector3(-35, 12, 0);
            overlay.localPosition = new Vector3(-35, -4, 0);

            foreach (var probe in transform.parent.GetComponentsInChildren<BLEProbe>())
            {
                probe.Rotate();
            }
            foreach (var probe in transform.parent.GetComponentsInChildren<BLEProbesStatistic>())
            {
                probe.Rotate();
            }
        }

        cam.UpdateCameraRect(logic.direction, logic.trainIndex);
    }


}
