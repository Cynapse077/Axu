using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseMenuPanel : MonoBehaviour
{
    public Button[] buttons;
    Animator anim;

    public int SelectedMax
    {
        get { return buttons.Length; }
    }

    public void Init()
    {
        anim = GetComponent<Animator>();

        buttons[0].onClick.AddListener(() => World.userInterface.OpenPlayerInventory());
        buttons[1].onClick.AddListener(() => World.userInterface.OpenCharacterPanel());
        buttons[2].onClick.AddListener(() => World.userInterface.OpenAbilities());
        buttons[3].onClick.AddListener(() => World.userInterface.OpenMap());
        buttons[4].onClick.AddListener(() => World.userInterface.OpenJournal());
        buttons[5].onClick.AddListener(() => World.userInterface.OpenOptionsFromButton());
        buttons[6].onClick.AddListener(() => World.userInterface.OpenSaveAndQuitDialogue());
    }

    public void TogglePause(bool paused)
    {
        anim.SetBool("Paused", paused);

        if (paused)
            UpdateSelected(0);
    }

    public void UpdateSelected(int index)
    {
        if (index < 0 || index > buttons.Length - 1)
            return;

        EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
    }
}
