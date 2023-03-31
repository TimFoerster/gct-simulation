using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(ProbePoint))]
public class Mean : MonoBehaviour
{
    public const double rad2deg = 180d / Math.PI;

    Vector2d[] vectors;
    public double Direction { get; private set; }
    public double ResultLength { get; private set; }

    public double Variance { get; private set; }

    public double StandardDeviation { get; private set; }

    public bool Enabled { get; private set; }
    public ulong Value { get; private set; }
    public byte[] HexValue { get => BitConverter.GetBytes(Value).Reverse().ToArray(); }
    public Color Color { get; private set; }
    public Vector2d Vector { get; internal set; }

    public UnityEvent OnChange = new UnityEvent();

    public UnityEvent OnDelete = new UnityEvent();

    public ProbePoint ProbePoint;

    public SpriteRenderer Renderer;

    public void Awake()
    {
        Renderer = GetComponent<SpriteRenderer>();
    }
    public void Start()
    {
        ProbePoint = GetComponent<ProbePoint>();
    }

    public void UpdateMean(IEnumerable<ulong> values)
    {
        Enabled = values.Count() >= 2;
        if (Enabled) { 
            values2Vectors(values);
            calculateMeans();
            calculateColor();
        }
        OnChange.Invoke();
    }

    private void OnDestroy()
    {
        OnDelete.Invoke();

    }

    void values2Vectors(IEnumerable<ulong> values)
    {
        vectors = new Vector2d[values.Count()];
        for(int i = 0; i < values.Count(); i++)
        {
            vectors[i] = CidCalculator.ulongToVector(values.ElementAt(i));
        }
    }

    void calculateMeans()
    {

        var sumVector = Vector2d.Zero;
        foreach (var val in vectors)
        {
            sumVector += val;
        }

        Vector = sumVector.Normalize();
        Value = CidCalculator.vectorToUlong(sumVector);
        Direction = (Math.Atan2(sumVector.y, sumVector.x) * Mathf.Rad2Deg + 360) % 360;
        ResultLength = sumVector.Magnitude() / vectors.Length;
        Variance = 1 - ResultLength;
        StandardDeviation = System.Math.Sqrt(-2d * System.Math.Log10(ResultLength)) * rad2deg;
    }


    void calculateColor()
    {
        var colorBytes = BitConverter.GetBytes(Value);
        float h = colorBytes[7] / 255f;
        // float s = colorBytes[6] / 255f;

        Color = Color.HSVToRGB(h, 1, (float)ResultLength, true);
    }


}