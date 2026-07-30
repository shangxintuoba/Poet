using UnityEngine;

public sealed class GameManager : MonoBehaviour
{

    public enum Scene_Large
    {
        BookStore,
        Home,
        Park,
        Restaurant,
        McDonald,
        Bar,
        Club,
        Cinema,
        Street_Home,
        Street_Book,
        Street_Downtown,
        Street_NewDistrict,
        Sea,
        Supermarket,
        CBDMarket,
        EditorOffice,
        HerHome,
        Gallery,
        Museum,
        Subway,
    }

    public int Date;



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




    public void SwitchScene()
    {

    }
    

    public void TimeProgress()
    {

    }
}