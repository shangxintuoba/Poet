using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MissionManager : MonoBehaviour
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


    public GameObject FinalMission;
    public GameObject DailyMission;

    public List<CardLibrary.DailyMissionData> CurrentMissionLists;

    private CardLibrary cardLibrary;

    private void Awake()
    {
        cardLibrary = FindFirstObjectByType<CardLibrary>();
        CurrentMissionLists = new List<CardLibrary.DailyMissionData> { null, null };
    }

    public void InstantiateMission()
    {
        IReadOnlyList<CardLibrary.DailyMissionData> missionList = cardLibrary.DailyMissions;
        CardLibrary.DailyMissionData selectedMission = missionList[UnityEngine.Random.Range(0, missionList.Count)];

        List<int> availableGroups = new List<int>();
        if (CurrentMissionLists[0] == null)
            availableGroups.Add(0);
        if (CurrentMissionLists[1] == null)
            availableGroups.Add(1);

        if (availableGroups.Count == 0)
            return;

        int selectedGroup = availableGroups[UnityEngine.Random.Range(0, availableGroups.Count)];
        CurrentMissionLists[selectedGroup] = selectedMission;
        SetMissionUI(selectedGroup, selectedMission);
    }

    private void SetMissionUI(int groupIndex, CardLibrary.DailyMissionData mission)
    {
        TextMeshProUGUI title = groupIndex == 0 ? m1Title : m2Title;
        TextMeshProUGUI reward = groupIndex == 0 ? m1Reward : m2Reward;
        TextMeshProUGUI firstRequirement = groupIndex == 0 ? Slot1text : Slot3text;
        TextMeshProUGUI secondRequirement = groupIndex == 0 ? Slot2text : Slot4text;

        title.text = mission.name;
        reward.text = mission.moneyReward.ToString();
        firstRequirement.text = GetRequiredCardName(mission, 0);
        secondRequirement.text = GetRequiredCardName(mission, 1);
    }

    private string GetRequiredCardName(CardLibrary.DailyMissionData mission, int index)
    {
        return cardLibrary.FindCardData(mission.requiredCards[index]).name;
    }

    public void TryCalculateResult()
    {
        //calculate if the result of 
    }

   
    public void TryCalculateFinalMission()
    {

    }

    public void ToggleMissionUI()
    {
        FinalMission.SetActive(!FinalMission.activeInHierarchy);
        DailyMission.SetActive(!DailyMission.activeInHierarchy);
    }
}
