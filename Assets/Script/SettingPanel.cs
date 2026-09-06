using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingPanel : MonoBehaviour
{
    public GameObject StartButton;
    public GameObject RestartButton;
    public GameObject QuitButton;
    public GameObject BackButton;
    public GameObject Title;
    public GameObject ToggleButton;
    private bool Opened;

    public GameObject TextPanel;
    public GameObject CardPanel;


    public void TogglePanel()
    {     
        RestartButton.SetActive(!Opened);
        QuitButton.SetActive(!Opened);
        BackButton.SetActive(!Opened);
        ToggleButton.SetActive(Opened);
        ShowOtherUI();
        Opened = !Opened;
        
    }

    public void ShowOtherUI()
    {
        if (Opened)
        {
            //move the typer
        }
        else
        {

        }

    }

    public void Quit()
    {
        //quitGame
    }

    public void Restart()
    {
        //reset the gamaestate
        Start();
    }

    public void Start()
    {
        
    }
}
