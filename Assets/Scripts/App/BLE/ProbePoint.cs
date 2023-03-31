using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProbePoint : MonoBehaviour
{
    public const double rad2deg = 180d / System.Math.PI;

    [SerializeField]
    SpriteRenderer probeRenderer;

    [SerializeField]
    ulong value;

    // Position
    [SerializeField]
    Vector2d targetPosition;

    [SerializeField]
    Vector3 currentVelocity;

    [SerializeField]
    Vector2d currentVector = Vector2d.Zero;

    // Rotation
    [SerializeField]
    bool rotation;

    [SerializeField]
    Quaternion targetRotation;

    [SerializeField]
    Color targetColor;

    public SpriteRenderer SpriteRenderer { get => probeRenderer; set => probeRenderer = value; }

    public Sprite Sprite { get => probeRenderer.sprite; set => probeRenderer.sprite = value; }

    public ulong Value { get => value; }
    public float Scale { get => transform.localScale.x; internal set => transform.localScale = new Vector3(value, value, 0); }

    private void OnEnable()
    {
        SpriteRenderer.enabled = true;
    }

    private void OnDisable()
    {
        SpriteRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!SpriteRenderer.isVisible || !SpriteRenderer.enabled) return;

        probeRenderer.color = Color.Lerp(probeRenderer.color, targetColor, Time.deltaTime);
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetPosition.AsVector2(), ref currentVelocity, 1f);
        if (rotation)
        {
            float t = Mathf.Clamp(Time.deltaTime, 0f, .2f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, t);
        }

    }

    public void NewValue(Mean mean, float range) => NewValue(mean, range, range);
    public void NewValue(Mean mean, float rangeX, float rangeY, bool depth = false)
    {
        value = mean.Value;

        if (SpriteRenderer == null || !SpriteRenderer.isVisible)
            return;

        currentVector = mean.Vector;
        targetColor = mean.Color;
        targetPosition = currentVector * 
            (depth ? mean.ResultLength * mean.ResultLength * mean.ResultLength : 1) * 
            new Vector2d(rangeX - Scale/2, rangeY - Scale/2);
        if (rotation)
        {
            targetRotation = Quaternion.Euler(0, 0, (float)(System.Math.Atan2(currentVector.y, currentVector.x) * rad2deg));
        }
    }

    public void NewValue(ulong value, float range)
    {
        NewValue(value, range, range);
    }

    public void NewValue(ulong value, float rangeX, float rangeY)
    {
        NewValue(value, rangeX, rangeY, Utils.U64ToHSV(value));
    }
    public void NewValue(ulong value, float rangeX, float rangeY, Color color)
    {

        this.value = value;

        if (!SpriteRenderer.isVisible)
            return;

        targetColor = color;
        currentVector = CidCalculator.ulongToVector(value);
        targetPosition = currentVector * new Vector2d(rangeX - Scale / 2, rangeY - Scale / 2);
        if (rotation)
        {
            targetRotation = Quaternion.Euler(0, 0, (float)(System.Math.Atan2(currentVector.y, currentVector.x) * rad2deg));
        }
    }

    public void ApplyScaling(float factor)
    {
        transform.localScale = new Vector2(transform.localScale.x * factor, transform.localScale.y * factor);
    }
} 
