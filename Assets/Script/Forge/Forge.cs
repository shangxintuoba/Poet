using System;
using System.Collections.Generic;
using UnityEngine;

public class Forge : MonoBehaviour
{
    public CardSlot ComponentSlot1;
    public CardSlot ComponentSlot2;
    public CardSlot ResultSlot;

    [SerializeField] private CardLibrary cardLibrary;
    [SerializeField] private CardManager cardManager;
    [SerializeField, Min(0f)] private float dropPadding = 40f;

    public bool TryPlaceNearestComponentCard(Card card, Vector2 screenPosition, Camera eventCamera)
    {
        if (card == null || !IsForgeIngredient(card) || !IsInsideDropArea(screenPosition, eventCamera))
            return false;

        CardSlot nearestSlot = FindNearestEmptyComponentSlot(card.transform.position);
        return nearestSlot != null && nearestSlot.TryPlace(card);
    }

    public bool TryPlaceComponentCard(CardSlot slot, Card card)
    {
        if (slot != ComponentSlot1 && slot != ComponentSlot2)
            return false;

        return card != null && slot.CurrentCard == null && IsForgeIngredient(card);
    }

    public void ForgeCurrentCards()
    {
        ForgeCards(
            ComponentSlot1 != null ? ComponentSlot1.CurrentCard : null,
            ComponentSlot2 != null ? ComponentSlot2.CurrentCard : null);
    }

    public void ForgeCards(Card card1, Card card2)
    {
        if (card1 == null || card2 == null || ResultSlot == null || ResultSlot.CurrentCard != null)
            return;

        string forgeType = GetForgeType(card1, card2);
        if (string.IsNullOrEmpty(forgeType))
            return;

        ResolveReferences();
        if (cardLibrary == null || cardManager == null || !cardLibrary.LoadJson())
            return;

        CardLibrary.ForgeLibraryData library = FindForgeLibrary(forgeType);
        CardLibrary.ForgeFormulaData formula = FindFormula(library, card1, card2);
        if (formula == null || string.IsNullOrWhiteSpace(formula.resultCardName))
            return;

        Card result = cardManager.CreateCardInSlot(formula.resultCardName, ResultSlot);
        if (result == null)
            return;

        cardManager.DestroyCards(new List<Card> { card1, card2 });
    }

    private bool IsForgeIngredient(Card card)
    {
        return IsEmotion(card) || TryGetMaterialType(card, out _);
    }

    private CardSlot FindNearestEmptyComponentSlot(Vector3 cardPosition)
    {
        CardSlot nearestSlot = null;
        float nearestDistance = float.MaxValue;

        foreach (CardSlot slot in new[] { ComponentSlot1, ComponentSlot2 })
        {
            if (slot == null || slot.CurrentCard != null)
                continue;

            float distance = Vector3.Distance(cardPosition, slot.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlot = slot;
            }
        }

        return nearestSlot;
    }

    private bool IsInsideDropArea(Vector2 screenPosition, Camera eventCamera)
    {
        RectTransform panel = transform as RectTransform;
        if (panel == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screenPosition, eventCamera, out Vector2 localPoint))
            return false;

        Rect rect = panel.rect;
        rect.xMin -= dropPadding;
        rect.xMax += dropPadding;
        rect.yMin -= dropPadding;
        rect.yMax += dropPadding;
        return rect.Contains(localPoint);
    }

    private string GetForgeType(Card card1, Card card2)
    {
        bool firstEmotion = IsEmotion(card1);
        bool secondEmotion = IsEmotion(card2);

        if (firstEmotion && secondEmotion)
            return "Emotion";

        bool firstMaterial = TryGetMaterialType(card1, out string firstType);
        bool secondMaterial = TryGetMaterialType(card2, out string secondType);

        if (firstEmotion)
            return secondMaterial && string.Equals(secondType, "Universal", StringComparison.OrdinalIgnoreCase)
                ? "Emotion"
                : null;

        if (secondEmotion)
            return firstMaterial && string.Equals(firstType, "Universal", StringComparison.OrdinalIgnoreCase)
                ? "Emotion"
                : null;

        if (!firstMaterial || !secondMaterial)
            return null;

        bool firstUniversal = string.Equals(firstType, "Universal", StringComparison.OrdinalIgnoreCase);
        bool secondUniversal = string.Equals(secondType, "Universal", StringComparison.OrdinalIgnoreCase);

        if (firstUniversal && secondUniversal)
            return "Universal";
        if (firstUniversal)
            return secondType;
        if (secondUniversal)
            return firstType;

        return string.Equals(firstType, secondType, StringComparison.OrdinalIgnoreCase)
            ? firstType
            : null;
    }

    private CardLibrary.ForgeLibraryData FindForgeLibrary(string forgeType)
    {
        foreach (CardLibrary.ForgeLibraryData library in cardLibrary.ForgeLibraries)
        {
            if (library != null && string.Equals(library.type, forgeType, StringComparison.OrdinalIgnoreCase))
                return library;
        }
        return null;
    }

    private CardLibrary.ForgeFormulaData FindFormula(CardLibrary.ForgeLibraryData library, Card card1, Card card2)
    {
        if (library == null || library.formulas == null)
            return null;

        string firstName = GetCardName(card1);
        string secondName = GetCardName(card2);
        foreach (CardLibrary.ForgeFormulaData formula in library.formulas)
        {
            if (formula == null)
                continue;

            bool forward = formula.firstIngredientName == firstName && formula.secondIngredientName == secondName;
            bool reverse = formula.firstIngredientName == secondName && formula.secondIngredientName == firstName;
            if (forward || reverse)
                return formula;
        }
        return null;
    }

    private static bool IsEmotion(Card card)
    {
        return card is Emotion || (card.Data != null && card.Data.type == "Emotion");
    }

    private static bool TryGetMaterialType(Card card, out string materialType)
    {
        materialType = null;
        if (card == null || IsEmotion(card))
            return false;

        if (card.Data != null && card.Data.type == "Material")
        {
            materialType = card.Data.materialType;
            return !string.IsNullOrWhiteSpace(materialType);
        }

        if (card is Material material)
        {
            materialType = material.type.ToString();
            return true;
        }

        return false;
    }

    private static string GetCardName(Card card)
    {
        if (card == null)
            return string.Empty;
        if (card.Data != null && !string.IsNullOrWhiteSpace(card.Data.name))
            return card.Data.name;
        if (!string.IsNullOrWhiteSpace(card.Name))
            return card.Name;
        return card.gameObject.name;
    }

    private void ResolveReferences()
    {
        if (cardLibrary == null)
            cardLibrary = FindFirstObjectByType<CardLibrary>();
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }
}
