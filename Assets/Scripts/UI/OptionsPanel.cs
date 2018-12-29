using UnityEngine;
using UnityEngine.UI;

public class OptionsPanel : MonoBehaviour
{
    [Header("Buttons")]
    public Button ApplyButton;
    public Button DefaultButton;
    public Button VimButton;
    public Button CancelButton;

    public Button ControlsTab;
    public Button GameplayTab;

    [Space(10)]
    public GameObject ControlsPanel;
    public GameObject GameplayPanel;

    void Start()
    {
        ApplyButton.onClick.AddListener(() => ApplyChanges());
        DefaultButton.onClick.AddListener(() => Defaults());
        VimButton.onClick.AddListener(() => VimKeys());
        CancelButton.onClick.AddListener(() => {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 2)
                World.userInterface.CloseWindows();
            else
                GameObject.FindObjectOfType<MainMenuPanel>().CloseOptionsMenu();
        });

        ControlsTab.onClick.AddListener(() => SwitchtoControlsPanel());
        GameplayTab.onClick.AddListener(() => SwitchtoGameplayPanel());

        SwitchtoGameplayPanel();
    }

    public void ApplyChanges()
    {
        Screen.SetResolution(GameSettings.ScreenSize.x, GameSettings.ScreenSize.y, GameSettings.Fullscreen);

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 2)
        {
            GameObject.FindObjectOfType<CameraControl>().Resize();
            World.tileMap.SoftRebuild();
        }

        GameSettings.Save();
        ControlsPanel.GetComponent<Options_KeyPanel>().Redo();
        GameplayPanel.GetComponent<Options_GamePanel>().Initialize();
    }

    public void Defaults()
    {
        GameSettings.Defaults();
        ApplyChanges();
    }

    public void VimKeys()
    {
        GameSettings.Keybindings.VIKeys();
        ApplyChanges();
    }

    public void SwitchtoGameplayPanel()
    {
        ControlsPanel.SetActive(false);
        GameplayPanel.SetActive(true);
        GameplayPanel.GetComponent<Options_GamePanel>().Initialize();
    }

    public void SwitchtoControlsPanel()
    {
        ControlsPanel.SetActive(true);
        GameplayPanel.SetActive(false);
    }
}
