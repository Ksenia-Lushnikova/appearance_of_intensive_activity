
using UnityEngine;
using UnityEngine.UI;

public class MonitorFlicker : MonoBehaviour
{
    private RawImage glow;
    private Texture2D texture;
    private float nextFrame;

    private void Start()
    {
        GameObject obj = new GameObject(
            "MonitorGlow",
            typeof(RectTransform),
            typeof(RawImage)
        );

        obj.transform.SetParent(transform, false);

        glow = obj.GetComponent<RawImage>();
        glow.raycastTarget = false;

        RectTransform rect = glow.rectTransform;

        rect.anchorMin = new Vector2(0.37f, 0.59f);
        rect.anchorMax = new Vector2(0.50f, 0.78f);

        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        texture = new Texture2D(32, 32);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 15.5f;
                float dy = (y - 15.5f) / 15.5f;

                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);

                texture.SetPixel(x, y,
                    new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        glow.texture = texture;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextFrame)
            return;

        float alpha = Random.Range(0.04f, 0.18f);

        glow.color = new Color(
            0.08f, 0.23f, 0.28f, alpha
        );

        nextFrame = Time.unscaledTime
            + Random.Range(0.08f, 0.35f);
    }

    private void OnDestroy()
    {
        if (texture != null)
            Destroy(texture);
    }
}
