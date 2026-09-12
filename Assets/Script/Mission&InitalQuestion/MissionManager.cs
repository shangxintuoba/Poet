using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MissionManager : MonoBehaviour, IPointerClickHandler
{
    public CardSlot Slot1;
    public CardSlot Slot2;
    public TextMeshProUGUI Slot1text;
    public TextMeshProUGUI Slot2text;
    public TextMeshProUGUI m1Title;
    public TextMeshProUGUI m1Reward;

    public CardSlot Slot3;
    public CardSlot Slot4;
    public TextMeshProUGUI Slot3text;
    public TextMeshProUGUI Slot4text;
    public TextMeshProUGUI m2Title;
    public TextMeshProUGUI m2Reward;

    public PanelDescription m1;
    public PanelDescription m2;

    public GameObject FinalMission;
    public GameObject DailyMission;

    public List<CardLibrary.DailyMissionData> CurrentMissionLists;
    public List<string> CurrentFinalMissionRequiredCards { get; } = new List<string>();

    [Header("Panel Toggle")]
    [SerializeField, Min(0f)] private float slideDownDistance = 200f;
    [SerializeField, Min(0f)] private float slideDuration = 0.18f;

    private CardLibrary cardLibrary;
    private RectTransform panelRect;
    private Vector2 openPosition;
    private Tween panelMoveTween;
    private bool isPanelOpen = true;

    private void Awake()
    {
        cardLibrary = FindFirstObjectByType<CardLibrary>();
        CurrentMissionLists = new List<CardLibrary.DailyMissionData> { null, null };
        panelRect = transform as RectTransform;
        if (panelRect != null)
            openPosition = panelRect.anchoredPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            TogglePanelPosition();
    }

    /// <summary>Slides the mission panel down, or restores it to its initial position.</summary>
    public void TogglePanelPosition()
    {
        if (panelRect == null)
            return;

        isPanelOpen = !isPanelOpen;
        Vector2 target = isPanelOpen
            ? openPosition
            : openPosition + Vector2.down * slideDownDistance;

        panelMoveTween?.Kill();
        panelMoveTween = panelRect.DOAnchorPos(target, slideDuration)
            .SetEase(Ease.OutQuad);
    }

    public void InstantiateMission()
    {
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        if (cardLibrary == null || !cardLibrary.LoadJson())
            return;

        IReadOnlyList<CardLibrary.DailyMissionData> missionList = cardLibrary.DailyMissions;
        if (missionList == null || missionList.Count == 0)
            return;

        List<int> availableGroups = new List<int>();
        if (CurrentMissionLists[0] == null)
            availableGroups.Add(0);
        if (CurrentMissionLists[1] == null)
            availableGroups.Add(1);

        if (availableGroups.Count == 0)
            return;

        int selectedGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
        CardLibrary.DailyMissionData selectedMission = missionList[UnityEngine.Random.Range(0, missionList.Count)];
        CurrentMissionLists[selectedGroup] = selectedMission;
        SetMissionUI(selectedGroup, selectedMission);
    }

    /// <summary>At the beginning of each day, fills any empty Daily Mission positions.</summary>
    public void RefreshDailyMissions()
    {
        if (CurrentMissionLists == null || CurrentMissionLists.Count != 2)
            CurrentMissionLists = new List<CardLibrary.DailyMissionData> { null, null };

        while (CurrentMissionLists[0] == null || CurrentMissionLists[1] == null)
        {
            int beforeCount = CountActiveDailyMissions();
            InstantiateMission();
            if (CountActiveDailyMissions() == beforeCount)
                break;
        }
    }

    private void SetMissionUI(int groupIndex, CardLibrary.DailyMissionData mission)
    {
        TextMeshProUGUI title = groupIndex == 0 ? m1Title : m2Title;
        TextMeshProUGUI reward = groupIndex == 0 ? m1Reward : m2Reward;
        TextMeshProUGUI firstRequirement = groupIndex == 0 ? Slot1text : Slot3text;
        TextMeshProUGUI secondRequirement = groupIndex == 0 ? Slot2text : Slot4text;
        PanelDescription panelDescription = groupIndex == 0 ? m1 : m2;

        title.text = mission.name;
        reward.text = "$" + mission.moneyReward;
        firstRequirement.text = GetRequiredCardName(mission, 0);
        secondRequirement.text = GetRequiredCardName(mission, 1);

        if (panelDescription != null)
        {
            panelDescription.PanelName = mission.name;
            panelDescription.Description = mission.description;
        }
    }

    private string GetRequiredCardName(CardLibrary.DailyMissionData mission, int index)
    {
        return cardLibrary.FindCardData(mission.requiredCards[index]).name;
    }

    public void TryCalculateResult()
    {
        TryCalculateDailyMission(0);
        TryCalculateDailyMission(1);
    }

   
    public void TryCalculateFinalMission()
    {
        CardSlot[] slots = GetFinalMissionSlots();
        if (CurrentFinalMissionRequiredCards.Count == 0 ||
            CurrentFinalMissionRequiredCards.Count != slots.Length)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!MatchesRequiredFinalCard(slots[i].CurrentCard, CurrentFinalMissionRequiredCards[i]))
                return;
        }

        GameManager.Instance?.HandleGameWin();
    }

    /// <summary>Returns whether a card may be placed in the specified mission slot.</summary>
    public bool CanPlaceCardInMissionSlot(Card card, CardSlot slot)
    {
        if (card == null || card.Data == null || slot == null)
            return false;

        if (FinalMission != null && slot.transform.IsChildOf(FinalMission.transform))
        {
            CardSlot[] finalSlots = GetFinalMissionSlots();
            int slotIndex = System.Array.IndexOf(finalSlots, slot);
            return slotIndex >= 0 && slotIndex < CurrentFinalMissionRequiredCards.Count &&
                   MatchesRequiredFinalCard(card, CurrentFinalMissionRequiredCards[slotIndex]);
        }

        if (!TryGetDailyMissionSlot(slot, out int groupIndex, out int requirementIndex))
            return false;

        CardLibrary.DailyMissionData mission = CurrentMissionLists != null && groupIndex < CurrentMissionLists.Count
            ? CurrentMissionLists[groupIndex]
            : null;
        return mission != null && mission.requiredCards != null && requirementIndex < mission.requiredCards.Length &&
               MatchesRequiredFinalCard(card, mission.requiredCards[requirementIndex]);
    }

    /// <summary>Checks Final Mission after a valid card has entered one of its slots.</summary>
    public void OnMissionCardPlaced(CardSlot slot)
    {
        if (FinalMission != null && slot != null && slot.transform.IsChildOf(FinalMission.transform))
        {
            TryCalculateFinalMission();
            return;
        }

        if (TryGetDailyMissionSlot(slot, out int groupIndex, out _))
            TryCalculateDailyMission(groupIndex);
    }

    /// <summary>Updates Final Mission's requirement labels from the InitialTest selections.</summary>
    public void SetFinalMissionRequiredCards(IEnumerable<string> cardReferences)
    {
        CurrentFinalMissionRequiredCards.Clear();
        if (cardReferences != null)
        {
            foreach (string cardReference in cardReferences)
            {
                if (!string.IsNullOrWhiteSpace(cardReference))
                    CurrentFinalMissionRequiredCards.Add(cardReference.Trim());
            }
        }

        if (FinalMission == null)
        {
            Debug.LogWarning("MissionManager has no FinalMission assigned.");
            return;
        }

        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        cardLibrary?.LoadJson();

        CardSlot[] slots = GetFinalMissionSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            TextMeshProUGUI requirementLabel = slots[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (requirementLabel == null)
                continue;

            if (i >= CurrentFinalMissionRequiredCards.Count)
            {
                requirementLabel.text = string.Empty;
                continue;
            }

            string cardReference = CurrentFinalMissionRequiredCards[i];
            CardLibrary.CardData cardData = cardLibrary != null
                ? cardLibrary.FindCardData(cardReference)
                : null;
            requirementLabel.text = cardData != null && !string.IsNullOrWhiteSpace(cardData.name)
                ? cardData.name
                : cardReference;
        }

        if (CurrentFinalMissionRequiredCards.Count > slots.Length)
            Debug.LogWarning("Final Mission has fewer slots than required cards.");
    }

    private CardSlot[] GetFinalMissionSlots()
    {
        return FinalMission != null
            ? FinalMission.GetComponentsInChildren<CardSlot>(true)
            : System.Array.Empty<CardSlot>();
    }

    private static bool MatchesRequiredFinalCard(Card card, string requiredReference)
    {
        if (card == null || card.Data == null || string.IsNullOrWhiteSpace(requiredReference))
            return false;

        return string.Equals(card.Data.id, requiredReference, System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(card.Data.name, requiredReference, System.StringComparison.OrdinalIgnoreCase);
    }

    private void TryCalculateDailyMission(int groupIndex)
    {
        if (CurrentMissionLists == null || groupIndex < 0 || groupIndex >= CurrentMissionLists.Count)
            return;

        CardLibrary.DailyMissionData mission = CurrentMissionLists[groupIndex];
        if (mission == null || mission.requiredCards == null || mission.requiredCards.Length < 2)
            return;

        CardSlot[] slots = GetDailyMissionSlots(groupIndex);
        if (slots[0].CurrentCard == null || slots[1].CurrentCard == null ||
            !MatchesRequiredFinalCard(slots[0].CurrentCard, mission.requiredCards[0]) ||
            !MatchesRequiredFinalCard(slots[1].CurrentCard, mission.requiredCards[1]))
            return;

        CardManager cardManager = FindFirstObjectByType<CardManager>();
        if (cardManager != null)
            cardManager.DestroyCards(new List<Card> { slots[0].CurrentCard, slots[1].CurrentCard });

        GameManager.Instance?.Money?.ChangeValue(mission.moneyReward);
        CurrentMissionLists[groupIndex] = null;
        ClearDailyMissionUI(groupIndex);
    }

    private bool TryGetDailyMissionSlot(CardSlot slot, out int groupIndex, out int requirementIndex)
    {
        groupIndex = -1;
        requirementIndex = -1;
        if (slot == Slot1) { groupIndex = 0; requirementIndex = 0; return true; }
        if (slot == Slot2) { groupIndex = 0; requirementIndex = 1; return true; }
        if (slot == Slot3) { groupIndex = 1; requirementIndex = 0; return true; }
        if (slot == Slot4) { groupIndex = 1; requirementIndex = 1; return true; }
        return false;
    }

    private CardSlot[] GetDailyMissionSlots(int groupIndex)
    {
        return groupIndex == 0
            ? new[] { Slot1, Slot2 }
            : new[] { Slot3, Slot4 };
    }

    private int CountActiveDailyMissions()
    {
        int count = 0;
        foreach (CardLibrary.DailyMissionData mission in CurrentMissionLists)
        {
            if (mission != null)
                count++;
        }
        return count;
    }

    private void ClearDailyMissionUI(int groupIndex)
    {
        TextMeshProUGUI title = groupIndex == 0 ? m1Title : m2Title;
        TextMeshProUGUI reward = groupIndex == 0 ? m1Reward : m2Reward;
        TextMeshProUGUI firstRequirement = groupIndex == 0 ? Slot1text : Slot3text;
        TextMeshProUGUI secondRequirement = groupIndex == 0 ? Slot2text : Slot4text;
        PanelDescription panelDescription = groupIndex == 0 ? m1 : m2;

        if (title != null) title.text = string.Empty;
        if (reward != null) reward.text = string.Empty;
        if (firstRequirement != null) firstRequirement.text = string.Empty;
        if (secondRequirement != null) secondRequirement.text = string.Empty;
        if (panelDescription != null)
        {
            panelDescription.PanelName = string.Empty;
            panelDescription.Description = string.Empty;
        }
    }

    public void ToggleMissionUI()
    {
        FinalMission.SetActive(!FinalMission.activeInHierarchy);
        DailyMission.SetActive(!DailyMission.activeInHierarchy);
    }

    private void OnDestroy()
    {
        panelMoveTween?.Kill();
    }
}
