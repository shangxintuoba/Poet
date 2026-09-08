using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SkyFilter : MonoBehaviour
{
    public Color Sunrise;
    public Color Day;
    public Color Sunset;
    public Color Night;

    private Image skyImage;

    private void Awake()
    {
        skyImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.TimeCard == null)
            return;

        skyImage.color = GetSkyColor(GameManager.Instance.TimeCard.CurrentTime);
    }

    private Color GetSkyColor(int minutes)
    {
        if (minutes >= 5 * 60 && minutes < 6 * 60)
            return Color.Lerp(Night, Sunrise, InverseLerpMinutes(minutes, 5 * 60, 6 * 60));

        if (minutes >= 6 * 60 && minutes < 7 * 60)
            return Color.Lerp(Sunrise, Day, InverseLerpMinutes(minutes, 6 * 60, 7 * 60));

        if (minutes >= 16 * 60 && minutes < 18 * 60)
            return Color.Lerp(Day, Sunset, InverseLerpMinutes(minutes, 16 * 60, 18 * 60));

        if (minutes >= 18 * 60 && minutes < 19 * 60)
            return Sunset;

        if (minutes >= 19 * 60 && minutes < 20 * 60)
            return Color.Lerp(Sunset, Night, InverseLerpMinutes(minutes, 19 * 60, 20 * 60));

        if (minutes >= 7 * 60 && minutes < 16 * 60)
            return Day;

        return Night;
    }

    private static float InverseLerpMinutes(int minutes, int from, int to)
    {
        return (minutes - from) / (float)(to - from);
    }
}
