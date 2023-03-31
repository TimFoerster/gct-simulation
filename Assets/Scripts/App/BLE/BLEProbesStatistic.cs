using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class BLEProbesStatistic : MonoBehaviour
{
    [SerializeField]
    bool alignToObjects;

    [SerializeField]
    public BLEProbe[] probes;

    [SerializeField]
    float paddingX;    

    [SerializeField]
    float paddingY;

    [SerializeField]
    LineRenderer lineRenderer;

    [SerializeField]
    Transform points;

    [SerializeField]
    public ProbePoint[] probePoints;

    [SerializeField]
    float rangeX;

    [SerializeField]
    float rangeY;

    [SerializeField]
    bool registerAsGlobalPoints;

    [SerializeField]
    bool isGlobal;

    [SerializeField]
    BLEProbesStatistic gsProbes;

    [SerializeField]
    float probePointScaling;

    // ellipse
    [SerializeField]
    int resolution = 1000;

    [SerializeField]
    bool depth;

    ProbePoint meanPoint { get => mean.ProbePoint; }

    [SerializeField]
    Sprite meanSprite;

    [SerializeField]
    float meanUpdateInterval = 2;
    float nextMeanUpdate = 0;

    [SerializeField]
    Mean mean;

    public Mean[] means;
    public ProbePoint[] meanPoints;

    [SerializeField]
    bool meanOverMeans;

    [SerializeField]
    TMP_Text infoText;

    [SerializeField]
    float scalingFactor = 1f;

    [SerializeField]
    SimulationTime time;

    private void OnEnable()
    {
        probePoints = new ProbePoint[probes.Count()];
        UpdateBLEProbes();

        meanPoints = new ProbePoint[means.Count()];
        UpdateMeanPoints();
    }


    private void OnDisable()
    {

        mean.OnDelete.Invoke();
        mean.OnChange.RemoveAllListeners();
        mean.OnDelete.RemoveAllListeners();

        // Required for hot reload, regenerate each probe point on enable
        for (int i = 0; i < probePoints.Count(); i++)
        {
            if (probePoints[i] != null)
            {
                Destroy(probePoints[i].gameObject);
            }
        }

        probePoints = null;

        // Required for hot reload, regenerate each probe point on enable
        for (int i = 0; i < meanPoints.Count(); i++)
        {
            if (meanPoints[i] != null)
            {
                Destroy(meanPoints[i].gameObject);
            }
        }

        meanPoints = null;

    }

    void Awake()
    {
        time = FindObjectOfType<SimulationTime>();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Forward to global register
        var gsObject = GameObject.FindGameObjectWithTag("GlobalStatistic");
        gsProbes = gsObject != null ? gsObject.GetComponent<BLEProbesStatistic>() : null;

        if (alignToObjects)
        {
            transform.position = getCenter();
        }

        drawOutline();

        meanPoint.enabled = false;
        meanPoint.Sprite = meanSprite;
        meanPoint.ApplyScaling(scalingFactor);

        if (gsProbes != null && !isGlobal && registerAsGlobalPoints && gsProbes != null)
        {
            gsProbes.RegisterProbes(probes);
        }

        if (!isGlobal && registerAsGlobalPoints && gsProbes != null)
        {
            gsProbes.RegisterMean(mean);
        }

        // Create calculation distribution, by distribute the calculation between meanUpdateInterval
        var ps = FindObjectsOfType<BLEProbesStatistic>();
        var slotStartTime = meanUpdateInterval * Mathf.Floor(time.time / meanUpdateInterval);

        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i] == this)
            {
                nextMeanUpdate = slotStartTime + i * Time.fixedDeltaTime;
                break;
            }
        }
    }

    void UpdateBLEProbes(int index = 0)
    {
        while (index < probePoints.Length && index < probes.Length && probes[index] != null)
        {
            var pp = GameObject.Instantiate(
                probes[index].MeanPoint,
                points
            );
            pp.Scale = isGlobal ? probePointScaling : 1;
            pp.enabled = false;
            // Set explicit the sprite, since we are OnEnable and sprite is set at Start.
            pp.Sprite = probes[index].MeanSprite;
            pp.ApplyScaling(scalingFactor);
            probePoints[index] = pp;
            probes[index].OnChange.AddListener((m) => OnMeanUpdate(pp, m));
            probes[index].OnDelete.AddListener(delegate { OnProbeDestroy(index, this); });
            index++;
        }
    }

    public void RegisterProbes(BLEProbe[] pps)
    {
        var indexOffset = probePoints.Count();
        System.Array.Resize(ref probes, probes.Count() + pps.Count());
        System.Array.Resize(ref probePoints, probePoints.Count() + pps.Count());
        for (int i = 0; indexOffset + i < probes.Count() && i < pps.Count(); i++)
        {
            probes[indexOffset + i] = pps[i];
        }
        UpdateBLEProbes(indexOffset);
    }

    void UpdateMeanPoints(int index = 0)
    {
        while (index < means.Count() && index < meanPoints.Count() && means[index] != null)
        {
            var mean = means[index];
            var pp = GameObject.Instantiate(
                mean.ProbePoint,
                points
            );

            pp.Scale = isGlobal ? probePointScaling : 1;
            pp.enabled = false;
            // Set explicit the sprite, since we are OnEnable and sprite is set at Start.
            pp.Sprite = mean.ProbePoint.Sprite;
            pp.ApplyScaling(scalingFactor);
            meanPoints[index] = pp;
            mean.OnChange.AddListener(delegate { OnMeanUpdate(pp, mean); });
            mean.OnDelete.AddListener(delegate { OnMeanDelete(index, this); });

            index++;
        }
    }


    void OnMeanDelete(int index, BLEProbesStatistic ps)
    {
        if (ps.means != null && index < ps.means.Count())
            ps.means[index] = null;
        if (ps.meanPoints != null && index < ps.meanPoints.Count())
        {
            if (ps.meanPoints[index] != null)
            {
                Destroy(ps.meanPoints[index].gameObject);
                ps.meanPoints[index] = null;
            }
        }
    }


    public void RegisterMean(Mean mean)
    {
        var indexOffset = meanPoints.Count();
        System.Array.Resize(ref means, means.Count() + 1);  
        System.Array.Resize(ref meanPoints, meanPoints.Count() + 1);
        means[indexOffset] = mean;
        UpdateMeanPoints(indexOffset);
    }

    void OnProbeDestroy(int index, BLEProbesStatistic ps)
    {
        if (ps.probePoints != null && index < ps.probePoints.Count() && ps.probePoints[index] != null)
            Destroy(ps.probePoints[index].gameObject);
    }


    void OnMeanUpdate(ProbePoint pp, Mean mean)
    {
        if (pp == null) return;

        if (mean == null || !mean.Enabled)
        {
            pp.enabled = false;
            return;
        }

        pp.NewValue(mean, rangeX, rangeY, depth);
        pp.enabled = true;
    }

    private void OnDrawGizmos()
    {
        // getCenter();
        // drawOutline();
    }

    void FixedUpdate()
    {
        if (nextMeanUpdate > time.time) { return; }

        nextMeanUpdate += meanUpdateInterval;

        IEnumerable<ulong> values;
        bool visible;
        if (meanOverMeans) {
            var enabledMeans = means.Where(m => m != null && m.Enabled);
            visible = enabledMeans.Count() > 1;
            values = means.Where(pp => pp != null && pp.Enabled).Select(pp => pp.Value);
        }
        else
        {
            var enabledPps = probePoints.Where(pp => pp.enabled);
            visible = enabledPps.Count() > 1;
            values = probePoints.Where(pp => pp.enabled).Select(pp => pp.Value);
        }

        var changed = meanPoint.enabled != visible;
        meanPoint.enabled = visible;

        if (visible)
        {
            mean.UpdateMean(values);
            meanPoint.NewValue(mean, rangeX + 1, rangeY + 1, true);

            if (infoText)
            {
                infoText.text = "<mspace=0.5em>\u03B8:  " + mean.Direction.ToString("000.00000000", System.Globalization.CultureInfo.InvariantCulture) + "°\n" +
                    "R:    " + mean.ResultLength.ToString("0.00000000", System.Globalization.CultureInfo.InvariantCulture) + " \n" +
                    "Vm:   " + mean.Variance.ToString("0.00000000", System.Globalization.CultureInfo.InvariantCulture) + " \n" +
                    "v:  " + mean.StandardDeviation.ToString("000.00000000", System.Globalization.CultureInfo.InvariantCulture) + "°\n</mspace>";
            }
        }
        else
        {
            if (changed)
            {
                mean.UpdateMean(values);
            }
            if (infoText)
            {
                infoText.text = "";
            }
        }
    }

    Vector3 getCenter()
    {
        if (probes == null || probes.Count() == 0) { return Vector3.zero; }
        var left = probes.Min(p => p.transform.position.x - p.range);
        var right = probes.Max(p => p.transform.position.x + p.range);

        var bottom = probes.Min(p => p.transform.position.y - p.range);
        var top = probes.Max(p => p.transform.position.y + p.range);

        rangeX = Mathf.Abs(right - left) / 2 + paddingX;
        rangeY = Mathf.Abs(top - bottom) / 2 + paddingY;

        return new Vector3((left + right) / 2, (bottom + top) / 2, 0);
    }

    void drawOutline()
    {
        lineRenderer.positionCount = resolution + 1;

        for (int i = 0; i <= resolution; i++)
        {
            float angle = (float)i / (float)resolution * 2.0f * Mathf.PI;
            lineRenderer.SetPosition(i, new Vector2(rangeX * Mathf.Cos(angle), rangeY * Mathf.Sin(angle)));
        }
    }

    public void Rotate()
    {
        infoText.transform.localRotation = Quaternion.Euler(0, 0, 180);
        infoText.transform.localPosition = new Vector3(0, 10, 0);
        points.localRotation = Quaternion.Euler(0, 0, 180);

    }


}
