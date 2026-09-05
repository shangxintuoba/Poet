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



    public void TogglePanel()
    {     
        RestartButton.SetActive(!Opened);
        QuitButton.SetActive(!Opened);
        BackButton.SetActive(!Opened);
        ToggleButton.SetActive(Opened);

        Opened = !Opened;
        
    }

    public void ShowOtherUI(bool panelState)
    {
        if (panelState == true) return;
        else
        {

        }

    }    


}
