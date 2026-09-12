using System.Collections.Generic;
using UnityEngine;

public class HandleInitialCard : MonoBehaviour
{
    private const int TouchYourInsideChoiceIndex = 0;

    /// <summary>Adds initial-test rewards to Your Body's "Touch your inside" choice.</summary>
    public bool AddInitialCards(IEnumerable<string> cardReferences)
    {
        Raw raw = GetComponent<Raw>();
        if (raw == null)
        {
            Debug.LogWarning("HandleInitialCard requires a Raw component.", this);
            return false;
        }

        return raw.AddCardsToChoice(TouchYourInsideChoiceIndex, cardReferences);
    }
}
