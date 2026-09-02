using DG.Tweening;
using UnityEngine;

public class Resource : Card
{
    public GameObject Amount_Indicator;

    public enum ResourceType
    {
        Money,
        WillPower
    }

    [SerializeField] private ResourceType resourceType;
    [SerializeField, Range(0, 10)] private int value = 1;
    [SerializeField] private string[] changingText = new string[11];
    [SerializeField, Min(0f)] private float minimumIndicatorHeight = 7f;
    [SerializeField, Min(0f)] private float indicatorHeightDuration = 0.18f;

    private RectTransform amountIndicatorRect;
    [SerializeField, Min(0f)]
    private float maximumIndicatorHeight;
    private Tween indicatorHeightTween;

    public ResourceType Type => resourceType;
    public int Value => value;

    private void OnEnable()
    {
        RefreshAmountIndicator(false);
        RegisterWithGameManager();
    }

    private void RegisterWithGameManager()
    {
        GameManager gameManager = GameManager.Instance;

        if (resourceType == ResourceType.Money)
            gameManager.Money = this;
        else if (resourceType == ResourceType.WillPower)
            gameManager.WillPower = this;
    }

    public void SetValue(int newValue)
    {
        value = Mathf.Clamp(newValue, 0, 10);
        RefreshAmountIndicator(true);
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

    private void RefreshAmountIndicator(bool animate)
    {
        if (amountIndicatorRect == null && Amount_Indicator != null)
            amountIndicatorRect = Amount_Indicator.GetComponent<RectTransform>();

        if (amountIndicatorRect == null)
            return;

        if (maximumIndicatorHeight <= 0f)
            maximumIndicatorHeight = amountIndicatorRect.sizeDelta.y;

        float normalizedValue = value / 10f;
        float height = Mathf.Lerp(minimumIndicatorHeight, maximumIndicatorHeight, normalizedValue);
        Vector2 targetSize = new Vector2(amountIndicatorRect.sizeDelta.x, height);

        indicatorHeightTween?.Kill();
        if (animate && Application.isPlaying)
            indicatorHeightTween = amountIndicatorRect.DOSizeDelta(targetSize, indicatorHeightDuration).SetEase(Ease.OutQuad);
        else
            amountIndicatorRect.sizeDelta = targetSize;
    }

    private string GetChangingText()
    {
        if (changingText == null || value < 0 || value >= changingText.Length)
            return string.Empty;

        return changingText[value];
    }

    private void OnValidate()
    {
        value = Mathf.Clamp(value, 0, 10);
        RefreshAmountIndicator(false);
    }

    private void OnDestroy()
    {
        indicatorHeightTween?.Kill();

        if (resourceType == ResourceType.Money && GameManager.Instance.Money == this)
            GameManager.Instance.Money = null;
        else if (resourceType == ResourceType.WillPower && GameManager.Instance.WillPower == this)
            GameManager.Instance.WillPower = null;
    }
}
