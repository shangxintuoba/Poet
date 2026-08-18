using UnityEngine;

public class Resource : Card
{
    public enum ResourceType
    {
        Money,
        WillPower
    }

    [SerializeField] private ResourceType resourceType;
    [SerializeField, Range(1, 10)] private int value = 1;
    [SerializeField] private string[] changingText = new string[11];

    public ResourceType Type => resourceType;
    public int Value => value;

    public void SetValue(int newValue)
    {
        value = Mathf.Clamp(newValue, 1, 10);
    }

    public void ChangeValue(int amount)
    {
        SetValue(value + amount);
    }

    protected override void ShowCardDetails()
    {
        ResolveTextPanel();

        string cardName = string.IsNullOrWhiteSpace(Name)
            ? (NameText != null ? NameText.text : gameObject.name)
            : Name;
        string details = string.IsNullOrWhiteSpace(Description)
            ? cardName
            : cardName + "\n\n" + Description;

        string valueText = GetChangingText();
        if (!string.IsNullOrWhiteSpace(valueText))
            details += "\n\n" + valueText;

        textPanel?.ShowCardDescription(details);
    }

    private string GetChangingText()
    {
        if (changingText == null || value < 0 || value >= changingText.Length)
            return string.Empty;

        return changingText[value];
    }

    private void OnValidate()
    {
        value = Mathf.Clamp(value, 1, 10);
    }
}
