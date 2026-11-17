
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimation : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("Animation Settings")]
    public float hoverScale = 1.05f;
    public float pressedScale = 0.93f;
    public float hoverOffset = 5f;
    public float pressedOffset = -6f;
    public float animationSpeed = 18f;
    private int lastState = -1;
    private float stateTime;

    private RectTransform rect;
    private Button button;

    private Vector3 originalScale;
    private Vector2 originalPosition;

    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();

        originalScale = rect.localScale;
        originalPosition = rect.anchoredPosition;
    }

    private void Update()
    {
        int state = 0;

        if (button.interactable && isHovered)
            state = isPressed ? 2 : 1;

        if (state != lastState)
        {
            lastState = state;
            stateTime = Time.unscaledTime;
        }

        float elapsed = Time.unscaledTime - stateTime;
        float offset = 0f;

        switch (state)
        {
            case 1:
                // отскакиваем
                offset = elapsed < 0.083f ? 8f : 4f;
                break;

            case 2:
                // вдавливаем
                offset = -8f;
                break;
        }

        rect.localScale = originalScale;
        rect.anchoredPosition =
            originalPosition + Vector2.up * offset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left
            && button.interactable)
            isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void OnDisable()
    {
        isHovered = false;
        isPressed = false;

        if (rect != null)
        {
            rect.localScale = originalScale;
            rect.anchoredPosition = originalPosition;
        }
    }
}
