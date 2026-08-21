using UnityEngine;

public class Time : Card
{
    private const int MinutesPerDay = 24 * 60;
    private const int LastDate = 14;

    [SerializeField, Range(0, MinutesPerDay - 1)] private int currentTime;
    [SerializeField, Range(1, LastDate)] private int date = 1;

    public int CurrentTime => currentTime;
    public int Date => date;
    public string DisplayTime => $"Day {date} — {currentTime / 60:00}:{currentTime % 60:00}";

    public void TimeProgress()
    {
        TimeProgress(1);
    }

    public void TimeProgress(int travelDistance)
    {
        if (travelDistance <= 0)
            return;

        int totalMinutes = (date - 1) * MinutesPerDay + currentTime;
        int finalMinute = (LastDate - 1) * MinutesPerDay + (MinutesPerDay - 1);
        totalMinutes = Mathf.Min(totalMinutes + 10 * travelDistance, finalMinute);

        date = totalMinutes / MinutesPerDay + 1;
        currentTime = totalMinutes % MinutesPerDay;
    }

    // Keeps compatibility with the original method name.

    protected override void ShowCardDetails()
    {
        ResolveTextPanel();

        string cardName = string.IsNullOrWhiteSpace(Name)
            ? (NameText != null ? NameText.text : gameObject.name)
            : Name;
        string details = string.IsNullOrWhiteSpace(Description)
            ? cardName
            : cardName + "\n\n" + Description;

        textPanel?.ShowCardDescription(details + "\n\n" + DisplayTime);
    }

    private void OnValidate()
    {
        currentTime = Mathf.Clamp(currentTime, 0, MinutesPerDay - 1);
        date = Mathf.Clamp(date, 1, LastDate);
    }
}
