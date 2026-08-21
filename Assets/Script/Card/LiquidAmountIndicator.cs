using DG.Tweening;
using UnityEngine;

public class LiquidAmountIndicator : MonoBehaviour
{
    [SerializeField] private RectTransform liquid;
    [SerializeField, Min(0f)] private float maxSwayAngle = 12f;
    [SerializeField, Min(0f)] private float maxHorizontalSway = 6f;
    [SerializeField, Min(0.01f)] private float speedForMaximumSway = 1200f;
    [SerializeField, Min(0.05f)] private float idleDelay = 0.08f;
    [SerializeField, Min(0.05f)] private float settleTime = 0.32f;
    [SerializeField, Min(0f)] private float restingRippleAngle = 0.6f;
    [SerializeField, Min(0f)] private float restingRippleFrequency = 1.4f;

    private Vector2 restingPosition;
    private Quaternion restingRotation;
    private float phase;
    private float currentAmplitude;
    private float targetAmplitude;
    private float currentFrequency;
    private float targetFrequency;
    private float lastMovementTime;
    private bool isDragging;

    private void Awake()
    {
        if (liquid == null)
            liquid = transform as RectTransform;

        if (liquid == null)
            return;

        restingPosition = liquid.anchoredPosition;
        restingRotation = liquid.localRotation;
    }

    public void BeginSway()
    {
        isDragging = true;
        lastMovementTime = UnityEngine.Time.unscaledTime;
        liquid?.DOPunchScale(new Vector3(0.03f, -0.02f, 0f), 0.12f, 1, 0.5f);
    }

    public void SetDragDelta(Vector2 dragDelta, float canvasScale)
    {
        if (liquid == null)
            return;

        float deltaTime = Mathf.Max(UnityEngine.Time.unscaledDeltaTime, 0.001f);
        float speed = dragDelta.magnitude / Mathf.Max(canvasScale, 0.01f) / deltaTime;
        float intensity = Mathf.Clamp01(speed / speedForMaximumSway);

        targetAmplitude = Mathf.Lerp(1.5f, maxSwayAngle, intensity);
        targetFrequency = Mathf.Lerp(3f, 13f, intensity);
        lastMovementTime = UnityEngine.Time.unscaledTime;
    }

    public void StopSway()
    {
        isDragging = false;
    }

    private void Update()
    {
        if (liquid == null)
            return;

        if (!isDragging || UnityEngine.Time.unscaledTime - lastMovementTime > idleDelay)
        {
            targetAmplitude = restingRippleAngle;
            targetFrequency = restingRippleFrequency;
        }

        currentAmplitude = Mathf.MoveTowards(currentAmplitude, targetAmplitude, maxSwayAngle * UnityEngine.Time.unscaledDeltaTime / settleTime);
        currentFrequency = Mathf.MoveTowards(currentFrequency, targetFrequency, 20f * UnityEngine.Time.unscaledDeltaTime / settleTime);
        phase += currentFrequency * UnityEngine.Time.unscaledDeltaTime;

        float wave = Mathf.Sin(phase);
        liquid.anchoredPosition = restingPosition + Vector2.right * (wave * maxHorizontalSway * currentAmplitude / Mathf.Max(maxSwayAngle, 0.01f));
        liquid.localRotation = restingRotation * Quaternion.Euler(0f, 0f, wave * currentAmplitude);
    }

    private void OnDisable()
    {
        isDragging = false;
        targetAmplitude = 0f;
    }
}
