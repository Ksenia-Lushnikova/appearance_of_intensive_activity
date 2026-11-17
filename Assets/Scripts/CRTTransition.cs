
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CRTTransition : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName;

    [Header("Animation")]
    public float frameDelay = 0.04f;
    public float lineHeight = 3f;
    public Color lineColor = new Color(0.8f, 1f, 0.9f);

    private bool isPlaying;

    public void Play()
    {
        if (!isPlaying)
            StartCoroutine(Transition());
    }

    private IEnumerator Transition()
    {
        isPlaying = true;

        yield return new WaitForEndOfFrame();

        Texture2D screenshot =
            ScreenCapture.CaptureScreenshotAsTexture();

        screenshot.filterMode = FilterMode.Point;

        // Создаём отдельный Canvas поверх игры
        GameObject overlay = new GameObject(
            "CRT Overlay", typeof(Canvas)
        );

        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        overlay.AddComponent<GraphicRaycaster>();

        // Чёрный фон
        Image background =
            CreateGraphic<Image>("Black", overlay.transform);

        background.color = Color.black;

        // Снимок текущего экрана
        RawImage screen =
            CreateGraphic<RawImage>("Screen", overlay.transform);

        screen.texture = screenshot;
        screen.raycastTarget = false;

        // Покадровое схлопывание
        float[] heights = { 1f, 0.75f, 0.4f, 0.15f, 0.02f };

        foreach (float height in heights)
        {
            screen.rectTransform.localScale =
                new Vector3(1f, height, 1f);

            yield return new WaitForSecondsRealtime(frameDelay);
        }

        screen.enabled = false;

        // Светящаяся горизонтальная линия
        Image line =
            CreateGraphic<Image>("Line", overlay.transform);

        line.color = lineColor;
        line.raycastTarget = false;

        RectTransform rect = line.rectTransform;

        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);

        rect.offsetMin = new Vector2(0f, -lineHeight / 2f);
        rect.offsetMax = new Vector2(0f, lineHeight / 2f);

        float[] widths = { 1f, 0.6f, 0.2f, 0.005f };

        foreach (float width in widths)
        {
            rect.localScale = new Vector3(width, 1f, 1f);

            yield return new WaitForSecondsRealtime(frameDelay);
        }

        line.enabled = false;

        yield return new WaitForSecondsRealtime(0.15f);

        Destroy(screenshot);
        Destroy(overlay);

        isPlaying = false;

        // Загружаем игровую сцену
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    private T CreateGraphic<T>(string name, Transform parent)
        where T : Graphic
    {
        GameObject obj = new GameObject(
            name, typeof(RectTransform)
        );

        obj.transform.SetParent(parent, false);

        T graphic = obj.AddComponent<T>();

        RectTransform rect = graphic.rectTransform;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return graphic;
    }
}
