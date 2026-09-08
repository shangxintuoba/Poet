using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public sealed class CharacterState
    {
        public int Progress;
        public string CurrentNodeIndex;
    }

    //player state
    public Resource Money;
    public Resource WillPower;
    public GameTime TimeCard;


    //GameState
    private readonly Dictionary<string, int> lastUsedDay = new();
    private readonly HashSet<string> usedOnce = new();
    private readonly Dictionary<string, CharacterState> characterStates = new();

    public GameObject BreakDownPrefab;
    public GameObject[] GameOverCards;
    public GameObject[] WinCards;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject forgePanel;
    [SerializeField] private GameObject missionPanel;

    private readonly List<Card> breakDownCards = new();
    private bool isGameOver;


    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public bool CanUseToday(string key)
    {
        int currentDay =TimeCard.Date;

        return !lastUsedDay.TryGetValue(key, out int usedDay)
            || usedDay != currentDay;
    }

    public void MarkUsedToday(string key)
    {
        int currentDay =TimeCard.Date;
        lastUsedDay[key] = currentDay;
    }


    public bool CanUseOnce(string key)
    {
        return !usedOnce.Contains(key);
    }

    public void MarkUsedOnce(string key)
    {
        usedOnce.Add(key);
    }

    public CharacterState GetCharacterState(CardLibrary.CardData characterData)
    {
        if (characterStates.TryGetValue(characterData.id, out CharacterState state))
            return state;

        CardLibrary.CharacterProgressData initialProgress = null;
        if (characterData.characterProgress != null)
        {
            foreach (CardLibrary.CharacterProgressData progressData in characterData.characterProgress)
            {
                if (initialProgress == null || progressData.progress < initialProgress.progress)
                    initialProgress = progressData;
            }
        }

        state = new CharacterState
        {
            Progress = initialProgress != null ? initialProgress.progress : 0,
            CurrentNodeIndex = initialProgress != null ? initialProgress.node : string.Empty
        };
        characterStates[characterData.id] = state;
        return state;
    }

    public void SetCharacterProgress(CardLibrary.CardData characterData, int progress)
    {
        CharacterState state = GetCharacterState(characterData);
        state.Progress = progress;

        if (characterData.characterProgress == null)
            return;

        foreach (CardLibrary.CharacterProgressData progressData in characterData.characterProgress)
        {
            if (progressData.progress == progress)
            {
                state.CurrentNodeIndex = progressData.node;
                return;
            }
        }
    }

    public int GetCharacterProgress(string characterId)
    {
        return characterStates.TryGetValue(characterId, out CharacterState state)
            ? state.Progress
            : 0;
    }

    public void HandleGameOver()
    {
        if (isGameOver)
            return;

        CardManager cardManager = FindFirstObjectByType<CardManager>();
        breakDownCards.RemoveAll(card => card == null || !cardManager.CardsOwned.Contains(card));

        Card breakDownCard = cardManager.CreateCardFromPrefab(BreakDownPrefab.GetComponent<Card>());
        breakDownCards.Add(breakDownCard);

        if (breakDownCards.Count < 3)
            return;

        isGameOver = true;
        cardManager.ClearAllCards();

        Destroy(mapPanel);
        Destroy(forgePanel);
        Destroy(missionPanel);

        foreach (GameObject gameOverCardPrefab in GameOverCards)
            cardManager.CreateCardFromPrefab(gameOverCardPrefab.GetComponent<Card>());

    }


}
