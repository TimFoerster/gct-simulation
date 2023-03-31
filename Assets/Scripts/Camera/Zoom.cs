using UnityEngine;

public class Zoom : MonoBehaviour
{

    [SerializeField] private Camera cam;
    private float targetZoom;

    [SerializeField] private float zoomFactor = 5f;

    void Start()
    {
        targetZoom = cam.orthographicSize;   
    }
    void LateUpdate()
    {
        float scrollData = Input.GetAxis("Mouse ScrollWheel");
        targetZoom -= scrollData * zoomFactor;
        targetZoom = Mathf.Clamp(targetZoom, 2f, 400f);
        cam.orthographicSize = targetZoom;
    }

   
}
