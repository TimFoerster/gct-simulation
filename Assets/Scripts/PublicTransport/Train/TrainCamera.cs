using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainCamera : MonoBehaviour
{
    [SerializeField]
    Camera cam;
    [SerializeField]
    CapsuleCollider2D trainCollider;
    [SerializeField]
    TrainUI ui;
    TrainDirection direction;
    int trainIndex;


    // Start is called before the first frame update
    void Start()
    {
        UpdateCam();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, default);

            if (hit.collider == trainCollider)
            {
                ui.AttachMainCamera();
            }
        }
    }

    public void UpdateCameraRect(TrainDirection direction, int trainIndex)
    {
        this.direction = direction;
        this.trainIndex = trainIndex;
        UpdateCam();
    }

    private void UpdateCam()
    {
        if (direction == TrainDirection.Unknown) { return; }

        var height = trainIndex / 2; 
        if (direction == TrainDirection.East)
        {
            cam.rect = new Rect(new Vector2(0, 1 - cam.rect.height * (height + 1)), cam.rect.size);
        }
        else
        {
            transform.localPosition = new Vector3(-35, 12, -1);
            cam.rect = new Rect(new Vector2(1 - cam.rect.width, 1 - cam.rect.height * (height + 1)), cam.rect.size);
        }
    }

}
