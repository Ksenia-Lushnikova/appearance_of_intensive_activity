
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LogoFlicker : MonoBehaviour
{
    private Image logo;
    private Color originalColor;

    private void Awake()
    {
        logo = GetComponent<Image>();
        originalColor = logo.color;
    }

    private IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(
                Random.Range(5f, 12f)
            );

            SetBrightness(0.45f);
            yield return new WaitForSecondsRealtime(0.08f);

            SetBrightness(1f);
            yield return new WaitForSecondsRealtime(0.12f);

            SetBrightness(0.7f);
            yield return new WaitForSecondsRealtime(0.06f);

            SetBrightness(1f);
        }
    }

    private void SetBrightness(float value)
    {
        logo.color = new Color(
            originalColor.r * value,
            originalColor.g * value,
            originalColor.b * value,
            originalColor.a
        );
    }
}
