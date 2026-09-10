using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Forge : MonoBehaviour
{
    private const string ForgeLibrarySheet = "ForgeLibrary";

    [FormerlySerializedAs("ComponentSlot1")] public CardSlot MaterialSlot;
    [FormerlySerializedAs("ComponentSlot2")] public CardSlot FuelSlot;
    public CardSlot ResultSlot;

    [SerializeField] private CardLibrary cardLibrary;
    [SerializeField] private CardManager cardManager;
    [SerializeField, Min(0f)] private float dropPadding = 40f;

    public float DropPadding => dropPadding;

    public void ForgeCurrentCards()
    {
        ForgeCards(
            MaterialSlot != null ? MaterialSlot.CurrentCard : null,
            FuelSlot != null ? FuelSlot.CurrentCard : null);
    }

    public void ForgeCards(Card card1, Card card2)
    {
        if (card1 == null || card2 == null || ResultSlot == null || ResultSlot.CurrentCard != null)
            return;

        if (!IsValidIngredientPair(card1, card2))
            return;

        ResolveReferences();
        if (cardLibrary == null || cardManager == null || !cardLibrary.LoadJson())
            return;

        CardLibrary.ForgeFormulaData formula = FindFormula(card1, card2);
        if (formula == null || string.IsNullOrWhiteSpace(formula.resultCardId))
            return;

        Card result = cardManager.CreateCardInSlot(formula.resultCardId, ResultSlot);
        if (result == null)
            return;

        cardManager.DestroyCards(new List<Card> { card1, card2 });
    }

    private static bool IsValidIngredientPair(Card card1, Card card2)
    {
        return IsMaterialSlotCard(card1) && IsFuelSlotCard(card2);
    }

    public static bool IsMaterialSlotCard(Card card)
    {
        return IsMaterial(card) || IsEmotion(card);
    }

    public static bool IsFuelSlotCard(Card card)
    {
        return IsFuel(card);
    }

    private CardLibrary.ForgeFormulaData FindFormula(Card card1, Card card2)
    {
        foreach (CardLibrary.ForgeLibraryData library in cardLibrary.ForgeLibraries)
        {
            if (library == null || library.formulas == null ||
                !string.Equals(library.sourceSheet, ForgeLibrarySheet, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (CardLibrary.ForgeFormulaData formula in library.formulas)
            {
                if (formula == null)
                    continue;

                bool forward = MatchesIngredient(formula.firstIngredientId, card1) &&
                               MatchesIngredient(formula.secondIngredientId, card2);
                bool reverse = MatchesIngredient(formula.firstIngredientId, card2) &&
                               MatchesIngredient(formula.secondIngredientId, card1);
                if (forward || reverse)
                    return formula;
            }
        }

        return null;
    }

    private static bool MatchesIngredient(string ingredientId, Card card)
    {
        if (card == null || card.Data == null || string.IsNullOrWhiteSpace(card.Data.id))
            return false;

        return string.Equals(card.Data.id, ingredientId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMaterial(Card card)
    {
        if (card == null || card is Emotion || (card.Data != null && string.Equals(card.Data.type, "Emotion", StringComparison.OrdinalIgnoreCase)))
            return false;

        return card is Material || (card.Data != null && string.Equals(card.Data.type, "Material", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsFuel(Card card)
    {
        return card is Fuel || (card != null && card.Data != null &&
                                string.Equals(card.Data.type, "Fuel", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsEmotion(Card card)
    {
        return card is Emotion || (card != null && card.Data != null &&
                                   string.Equals(card.Data.type, "Emotion", StringComparison.OrdinalIgnoreCase));
    }

    private void ResolveReferences()
    {
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }
}
