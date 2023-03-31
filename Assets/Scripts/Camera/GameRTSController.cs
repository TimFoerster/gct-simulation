using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRTSController : MonoBehaviour {

    [SerializeField] private Transform selectionAreaTransform;

    private Vector3 startPosition;
    //private List<RTSUnit> selectPersons;
    public Transform followingObject;

    private Camera mainCamera;

    private void Start() {
        //selectPersons = new List<RTSUnit>();
        selectionAreaTransform.gameObject.SetActive(false);
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            // Left Mouse Button Pressed
            selectionAreaTransform.gameObject.SetActive(true);
            startPosition = Utils.GetMouseWorldPosition();
        }

        if (Input.GetMouseButton(0)) {
            // Left Mouse Button Held Down
            Vector3 currentMousePosition = Utils.GetMouseWorldPosition();
            Vector3 lowerLeft = new Vector3(
                Mathf.Min(startPosition.x, currentMousePosition.x),
                Mathf.Min(startPosition.y, currentMousePosition.y)
            );
            Vector3 upperRight = new Vector3(
                Mathf.Max(startPosition.x, currentMousePosition.x),
                Mathf.Max(startPosition.y, currentMousePosition.y)
            );
            selectionAreaTransform.position = lowerLeft;
            selectionAreaTransform.localScale = upperRight  - lowerLeft;
        }

        if (Input.GetMouseButtonUp(0)) {
            // Left Mouse Button Released
            selectionAreaTransform.gameObject.SetActive(false);

            Collider2D[] collider2DArray = Physics2D.OverlapAreaAll(startPosition, Utils.GetMouseWorldPosition());

            // Deselect all Units
            /*
            foreach (var unitRTS in selectPersons) {
                unitRTS.SetSelectedVisible(false);
            }
            selectPersons.Clear();
            */
            // remove train view
            if (followingObject != null)
            {
                followingObject = null;
                mainCamera.transform.parent = transform.parent;
            }

            // Select Units within Selection Area
            foreach (Collider2D collider2D in collider2DArray) {
                var unitRTS = collider2D.GetComponent<RTSUnit>();
                if (unitRTS != null) {
                    unitRTS.SetSelectedVisible(true);
                    //selectPersons.Add(unitRTS);
                }

                var train = collider2D.GetComponent<TrainLogic>();
                if (train != null)
                {
                    followingObject = train.transform;
                    mainCamera.transform.parent = train.transform;
                }
            }

        }

        if (Input.GetMouseButtonDown(1)) {
            // Right Mouse Button Pressed
            Vector3 moveToPosition = Utils.GetMouseWorldPosition();

            List<Vector3> targetPositionList = GetPositionListAround(moveToPosition, new float[] { 10f, 20f, 30f }, new int[] { 5, 10, 20 });


            /*
            int targetPositionListIndex = 0;
            foreach (var unitRTS in selectPersons) {
                unitRTS.MoveTo(targetPositionList[targetPositionListIndex], -1);
                targetPositionListIndex = (targetPositionListIndex + 1) % targetPositionList.Count;
            }*/
        }

        if (Input.GetKeyDown(KeyCode.Delete))
        {
            /*
            if (selectPersons.Count > 0)
            {
                foreach (var p in selectPersons)
                {
                    Destroy(p);
                }
                selectPersons.Clear();
            }*/
        }

    }

    private void Destroy(RTSUnit p)
    {
        throw new NotImplementedException();
    }

    private List<Vector3> GetPositionListAround(Vector3 startPosition, float[] ringDistanceArray, int[] ringPositionCountArray) {
        List<Vector3> positionList = new List<Vector3>();
        positionList.Add(startPosition);
        for (int i = 0; i < ringDistanceArray.Length; i++) {
            positionList.AddRange(GetPositionListAround(startPosition, ringDistanceArray[i], ringPositionCountArray[i]));
        }
        return positionList;
    }

    private List<Vector3> GetPositionListAround(Vector3 startPosition, float distance, int positionCount) {
        List<Vector3> positionList = new List<Vector3>();
        for (int i = 0; i < positionCount; i++) {
            float angle = i * (360f / positionCount);
            Vector3 dir = ApplyRotationToVector(new Vector3(1, 0), angle);
            Vector3 position = startPosition + dir * distance;
            positionList.Add(position);
        }
        return positionList;
    }

    private Vector3 ApplyRotationToVector(Vector3 vec, float angle) {
        return Quaternion.Euler(0, 0, angle) * vec;
    }

}
