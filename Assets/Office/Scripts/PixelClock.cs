using UnityEngine;
using UnityEngine.UI;

public sealed class PixelClock : MonoBehaviour
{
    public Image[] digitImages = new Image[4];
    public Image colonImage;
    public Sprite[] digitSprites = new Sprite[10];

    private int lastMinutes = -1;

    public void SetMinutes(int totalMinutes)
    {
        if (lastMinutes == totalMinutes)
            return;

        lastMinutes = totalMinutes;
        int hours = Mathf.Clamp(totalMinutes / 60, 0, 99);
        int minutes = Mathf.Clamp(totalMinutes % 60, 0, 59);
        int[] values = { hours / 10, hours % 10, minutes / 10, minutes % 10 };

        for (int i = 0; i < digitImages.Length && i < values.Length; i++)
        {
            if (digitImages[i] != null && digitSprites != null && digitSprites.Length > values[i])
            {
                Sprite digit = digitSprites[values[i]];
                digitImages[i].sprite = digit;
                if (digit != null)
                {
                    // A digit may include extra transparent columns. Keep the glyph at
                    // the original aspect ratio while the digit positions stay fixed.
                    RectTransform rect = digitImages[i].rectTransform;
                    rect.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, rect.rect.height * digit.rect.width / digit.rect.height);
                }
            }
        }

        if (colonImage != null)
            colonImage.enabled = true;
    }
}
