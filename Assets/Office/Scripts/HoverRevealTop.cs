using UnityEngine;
using UnityEngine.InputSystem;

public sealed class HoverRevealTop : MonoBehaviour
{
    public RectTransform target;
    public OfficeGameController game;
    [Range(0.5f, 0.99f)] public float revealThreshold = 0.72f;
    [Min(1f)] public float zoom = 1.2f;
    [Min(0.1f)] public float smoothing = 7f;

    private RectTransform viewport;
    private float reveal;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        viewport = target.parent as RectTransform;
        target.localScale = new Vector3(zoom, zoom, 1f);
        ApplyView();
    }

    private void Update()
    {
        if (viewport == null || (game != null && game.IsPaused))
            return;

        float wanted = 0f;
        if (Mouse.current != null && (game == null || !game.GameEnded) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, Mouse.current.position.ReadValue(), null, out Vector2 local) &&
            viewport.rect.Contains(local))
        {
            float y = Mathf.InverseLerp(viewport.rect.yMin, viewport.rect.yMax, local.y);
            wanted = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(revealThreshold, 0.96f, y));
        }

        reveal = Mathf.Lerp(reveal, wanted, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        ApplyView();
    }

    private void ApplyView()
    {
        if (viewport == null)
            return;

        // Pan between the edges of the uniformly scaled art.
        float overscan = viewport.rect.height * (zoom - 1f) * 0.5f;
        target.anchoredPosition = new Vector2(0f, Mathf.Lerp(overscan, -overscan, reveal));
    }
}
