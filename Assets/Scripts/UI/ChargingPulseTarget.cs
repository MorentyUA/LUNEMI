using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChargingPulseTarget : MonoBehaviour
{
    [Header("Что пульсирует")]
    [SerializeField] private Image targetImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("Цвет")]
    [SerializeField] private bool pulseColorEnabled = true;
    [SerializeField] private bool useCurrentColorAsNormal = true;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pulseColor = Color.cyan;
    [SerializeField] private float colorPulseSpeed = 6f;

    [Header("Размер")]
    [SerializeField] private bool pulseScale = true;
    [SerializeField] private float scalePower = 0.08f;
    [SerializeField] private float scalePulseSpeed = 7f;

    private Vector3 startScale;
    private readonly HashSet<Object> activeSources = new HashSet<Object>();

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();

        startScale = transform.localScale;

        if (useCurrentColorAsNormal)
        {
            if (targetImage != null)
                normalColor = targetImage.color;

            if (targetSpriteRenderer != null)
                normalColor = targetSpriteRenderer.color;
        }

        ApplyNormalState();
    }

    private void Update()
    {
        if (activeSources.Count > 0)
        {
            if (pulseColorEnabled)
            {
                float pulse = (Mathf.Sin(Time.time * colorPulseSpeed) + 1f) * 0.5f;
                Color currentColor = Color.Lerp(normalColor, pulseColor, pulse);

                if (targetImage != null)
                    targetImage.color = currentColor;

                if (targetSpriteRenderer != null)
                    targetSpriteRenderer.color = currentColor;
            }

            if (pulseScale)
            {
                float scalePulse = 1f + Mathf.Sin(Time.time * scalePulseSpeed) * scalePower;
                transform.localScale = startScale * scalePulse;
            }
        }
        else
        {
            ApplyNormalState();
        }
    }

    public void SetPulseActive(Object source, bool active)
    {
        if (source == null)
            return;

        if (active)
            activeSources.Add(source);
        else
            activeSources.Remove(source);
    }

    public void ForceStopAllPulse()
    {
        activeSources.Clear();
        ApplyNormalState();
    }

    private void ApplyNormalState()
    {
        if (pulseColorEnabled)
        {
            if (targetImage != null)
                targetImage.color = normalColor;

            if (targetSpriteRenderer != null)
                targetSpriteRenderer.color = normalColor;
        }

        transform.localScale = startScale;
    }
}
