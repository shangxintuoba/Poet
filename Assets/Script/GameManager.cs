using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    //player state
    public Resource Money;
    public Resource WillPower;
    public GameTime TimeCard;


    //GameState
    public int BradPit_progress;
    public int Her_progress;
    public int Editor_progress;
    public int Lawyer_progress;
    public int Bar_Progress;
    public int ParkKids_Progress;

    private readonly Dictionary<string, int> lastUsedDay = new();
    private readonly HashSet<string> usedOnce = new();



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

    public void HandleGameOver()
    {

    }

}