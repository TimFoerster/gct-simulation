using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.Events;

[RequireComponent(typeof(BLEReceiver))]
public class BLEProbe : MonoBehaviour
{
    public BLEReceiver receiver;
    TextMeshPro info;
    public float range = 10f;
    public float probeInterval = 2;
    float nextProbing = 0;

    [SerializeField]
    Mean mean;
    public Mean Mean { get => mean; }

    [SerializeField]
    ProbePoint probePoint;
    [SerializeField]
    Transform probePoints;
    [SerializeField]
    LineRenderer lineRenderer;

    [SerializeField]
    Sprite meanSprite;

    [SerializeField]
    float scalingFactor = 1f;

    // Circle
    float theta_scale = 0.01f;        //Set lower to add more points

    bool uiUpdate = false;
    bool uiUpdated = false;

    // Points that are displayed
    Dictionary<int, ProbePoint> _points;
    Dictionary<int, ProbePoint> points
    {
        get { return _points ?? (_points = new Dictionary<int, ProbePoint>()); }
    }

    public ProbePoint MeanPoint { get => mean.ProbePoint; }
    public Sprite MeanSprite { get => meanSprite; }

    public void SetMeanSprite(Sprite sprite)
    {
        meanSprite = sprite;
        MeanPoint.Sprite = sprite;
    } 

    BLEReceive<ulong>[] messages;

    public UnityEvent<Mean> OnChange;

    public UnityEvent OnDelete;

    bool rotate = false;

    [SerializeField]
    SimulationTime time;


    private void OnBecameVisible()
    {
        uiUpdate = true;
    }

    private void OnBecameInvisible()
    {
        uiUpdate = false;
    }

    private void OnDisable()
    {
        messages = null;
        pointsCleanup();
        OnChange.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        OnDelete.Invoke();
    }

    void Awake()
    {
        time = FindObjectOfType<SimulationTime>();   
    }

    void Start()
    {
        MeanPoint.enabled = false;
        MeanPoint.Sprite = meanSprite;
        MeanPoint.ApplyScaling(scalingFactor);
        info = GetComponentInChildren<TextMeshPro>();
        receiver.range = range;
        receiver.DisableGroupRecording();
        bakeLine();
        DoRotate();

        // Create calculation distribution, by distribute the calculation between probeInterval
        var probes = FindObjectsOfType<BLEProbe>();
        var slotStartTime = probeInterval * Mathf.Floor(time.time / probeInterval);

        for (int i = 0; i < probes.Length; i++)
        {
            if (probes[i] == this)
            {
                nextProbing = slotStartTime + i * time.fixedDeltaTime;
                break;
            }
        }
        
    }

    void FixedUpdate()
    {
        if (nextProbing > time.time) {  return; }

        nextProbing += probeInterval;
        messages = receiver.GetLastReceivedMessagesBySender().ToArray();

        if (messages.Count() > 0 || mean.Enabled)
        {
            mean.UpdateMean(messages.Select(s => s.package.cid));
        }
        
        uiUpdated = false;
    }

    void LateUpdate()
    {
        if (uiUpdated) { return; }

        uiUpdated = true;

        if (messages != null && messages.Length > 1)
        {
            MeanPoint.enabled = true;
            MeanPoint.NewValue(mean, range);
            OnChange.Invoke(mean);

            // dont update other stuff
            if (!uiUpdate)
                return;

            pointsCleanup();
            info.text = "<mspace=0.5em>\u03B8:  " + mean.Direction.ToString("000.00000000", System.Globalization.CultureInfo.InvariantCulture) + "°\n" +
                "R:    " + mean.ResultLength.ToString("0.00000000", System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                "Vm:   " + mean.Variance.ToString("0.00000000", System.Globalization.CultureInfo.InvariantCulture) + "\n" +
                "v:  " + mean.StandardDeviation.ToString("000.00000000", System.Globalization.CultureInfo.InvariantCulture) + "°\n</mspace>";

            ProbePoint point;

            foreach (var msg in messages)
            {
                var hit = points.TryGetValue(msg.package.uuid, out point);
                if (!hit)
                {
                    point = GameObject.Instantiate(
                        probePoint,
                        probePoints.transform
                    );
                    point.ApplyScaling(scalingFactor);
                    points.Add(msg.package.uuid, point);
                }

                point.NewValue(msg.package.cid, range);
            }

            return;
        }

        info.text = "";
        MeanPoint.enabled = false;
        pointsCleanup();
        OnChange.Invoke(mean);
    }

    void pointsCleanup()
    {
        if (points.Count == 0) { return; }

        if (messages == null || messages.Count() <= 1)
        {
            foreach (var p in points)
            {
                Destroy(p.Value.gameObject);
            }
            points.Clear();
            return;
        }

        var toRemove = new List<int>();

        // Cleanup
        foreach (var p in points)
        {
            bool found = false;
            foreach (var msg in messages)
            {
                if (p.Key == msg.package.uuid)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                toRemove.Add(p.Key);
                Destroy(p.Value.gameObject);
            }
        }

        foreach (var r in toRemove)
        {
            points.Remove(r);
        }

        if (points.Count <= 1)
        {
            MeanPoint.enabled = false;
            OnChange.Invoke(mean);
        }
    }

    void bakeLine()
    {
        int size = (int)(2.0f * Mathf.PI / theta_scale) + 1;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = size;
        float theta = 0f;

        for (int i = 0; i < size; i++)
        {
            theta += 2.0f * Mathf.PI * theta_scale;
            lineRenderer.SetPosition(i, 
                new Vector3(
                    range * Mathf.Cos(theta), 
                    range * Mathf.Sin(theta), 
                    0
                )
            );
        }

    }

    void OnDrawGizmos()
    {
        bakeLine();
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
    }

    public void Rotate()
    {
        rotate = true;
        DoRotate();
    }

    void DoRotate()
    {
        if (!rotate) return;
        if (probePoints != null)
            probePoints.localRotation = Quaternion.Euler(0, 0, 180);
        if (info != null)
            info.transform.localRotation = Quaternion.Euler(0, 0, 180);
    }
}
