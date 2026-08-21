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




}