using System;
using System.Collections.Generic;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    [SerializeField] private TextPanelUI textPanel;
    [SerializeField] private CardLibrary cardLibrary;
    [SerializeField] private CardManager cardManager;

    public bool IsTyping => textPanel != null && textPanel.IsTyping;

    public void ShowDescription(string description)
    {
        textPanel?.ShowCardDescription(description);
    }

    public void HideDescription()
    {
        textPanel?.RestoreDialogueAfterCardDescription();
    }

    public void ShowNode(string nodeId, int currentTime)
    {
        if (textPanel == null)
            return;

        ResolveDependencies();
        CardLibrary.NodeData node = cardLibrary != null ? cardLibrary.FindNodeData(nodeId) : null;
        if (node == null)
        {
            textPanel.SetDisplayedText(string.Empty);
            return;
        }

        GetNodePeriodContent(node, currentTime, out string text, out string[] choiceIds);
        List<CardLibrary.NodeChoiceData> choices = new List<CardLibrary.NodeChoiceData>();
        foreach (string choiceId in choiceIds ?? Array.Empty<string>())
        {
            CardLibrary.NodeChoiceData choice = cardLibrary.FindNodeChoiceData(choiceId);
            if (choice != null && CanUseNodeChoice(choice))
                choices.Add(choice);
        }

        List<string> choiceTexts = choices.ConvertAll(choice =>
            choice.moneyRequired > 0
                ? $"{choice.text} ($ {choice.moneyRequired})"
                : choice.text);
        textPanel.ShowTextWithChoices(text, choiceTexts, selectedIndex =>
        {
            if (selectedIndex >= 0 && selectedIndex < choices.Count)
                UseNodeChoice(choices[selectedIndex]);
        });
    }

    private static void GetNodePeriodContent(CardLibrary.NodeData node, int currentTime,
        out string text, out string[] choiceIds)
    {
        currentTime = Mathf.Clamp(currentTime, 0, 1439);
        if (currentTime >= 6 * 60 && currentTime < 13 * 60)
        {
            text = node.textMorning;
            choiceIds = node.choicesMorning;
        }
        else if (currentTime >= 13 * 60 && currentTime < 16 * 60)
        {
            text = node.textAfternoon;
            choiceIds = node.choicesAfternoon;
        }
        else if (currentTime >= 16 * 60 && currentTime < 19 * 60)
        {
            text = node.textSunset;
            choiceIds = node.choicesSunset;
        }
        else if (currentTime >= 19 * 60)
        {
            text = node.textNight;
            choiceIds = node.choicesNight;
        }
        else
        {
            text = node.textMidnight;
            choiceIds = node.choicesMidnight;
        }
    }

    private bool CanUseNodeChoice(CardLibrary.NodeChoiceData choice)
    {
        if (GameManager.Instance == null || choice == null)
            return false;

        string key = "NodeChoice:" + choice.id;
        return (!choice.oncePerDay || (GameManager.Instance.TimeCard != null && GameManager.Instance.CanUseToday(key))) &&
               (!choice.useOnlyOnce || GameManager.Instance.CanUseOnce(key));
    }

    private static bool CanAffordNodeChoice(CardLibrary.NodeChoiceData choice)
    {
        if (choice == null || choice.moneyRequired <= 0)
            return true;

        return GameManager.Instance != null &&
               GameManager.Instance.Money != null &&
               GameManager.Instance.Money.Value >= choice.moneyRequired;
    }

    private void UseNodeChoice(CardLibrary.NodeChoiceData choice)
    {
        if (!CanUseNodeChoice(choice) || !CanAffordNodeChoice(choice))
            return;

        ResolveDependencies();
        cardManager?.CreateCards(new List<string>(choice.cardsAdded ?? Array.Empty<string>()));
        cardManager?.DestroyCardsByDataIds(new List<string>(choice.cardsRemoved ?? Array.Empty<string>()));
        if (choice.deltaWillPower != 0 && GameManager.Instance.WillPower != null)
            GameManager.Instance.WillPower.ChangeValue(choice.deltaWillPower);
        if (choice.deltaMoney != 0 && GameManager.Instance.Money != null)
            GameManager.Instance.Money.ChangeValue(choice.deltaMoney);

        string key = "NodeChoice:" + choice.id;
        if (choice.oncePerDay)
            GameManager.Instance.MarkUsedToday(key);
        if (choice.useOnlyOnce)
            GameManager.Instance.MarkUsedOnce(key);

        textPanel.ClearChoices();

        if (!string.IsNullOrWhiteSpace(choice.goToFarNode))
            FindFirstObjectByType<Map>()?.TryGoToFarNode(choice.goToFarNode);
    }

    private void ResolveDependencies()
    {
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }
}
