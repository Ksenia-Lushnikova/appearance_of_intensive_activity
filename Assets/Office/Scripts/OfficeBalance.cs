using UnityEngine;

public static class OfficeBalance
{
    public const int StartMinutes = 8 * 60 + 24;
    public const int EndMinutes = 18 * 60;

    public static float ProductivityCap(int reprimands)
    {
        return Mathf.Clamp(100f - Mathf.Max(0, reprimands) * (100f / 3f), 0f, 100f);
    }

    public static int CurrentShiftMinutes(float elapsedSeconds, float shiftDurationSeconds)
    {
        float progress = shiftDurationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / shiftDurationSeconds);
        return Mathf.RoundToInt(Mathf.Lerp(StartMinutes, EndMinutes, progress));
    }

    public static string FormatClock(int totalMinutes)
    {
        int hours = Mathf.Max(0, totalMinutes) / 60;
        int minutes = Mathf.Max(0, totalMinutes) % 60;
        return $"{hours:00}:{minutes:00}";
    }

    public static bool HasPremium(int reprimands)
    {
        return reprimands < 2;
    }
}
