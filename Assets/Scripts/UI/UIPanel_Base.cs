using UnityEngine;

public class UIPanel_Base : MonoBehaviour
{
    public int selected;
    public int selectedMax;
    public int column;
    public int maxColumns;

    public virtual void HandleInput()
    {
        if (PlayerInput.lockInput)
        {
            return;
        }

        if (GameSettings.Keybindings.GetKey("South"))
        {
            selected++;
            Wrap(ref selected, selectedMax);
            World.soundManager.MenuTick();
        }

        if (GameSettings.Keybindings.GetKey("North"))
        {
            selected--;
            Wrap(ref selected, selectedMax);
            World.soundManager.MenuTick();
        }

        if (GameSettings.Keybindings.GetKey("East"))
        {
            column++;
            Wrap(ref column, maxColumns);
            World.soundManager.MenuTick();
        }

        if (GameSettings.Keybindings.GetKey("West"))
        {
            column--;
            Wrap(ref column, maxColumns);
            World.soundManager.MenuTick();
        }

        if (GameSettings.Keybindings.GetKey("Enter"))
        {
            OnEnter();
            World.soundManager.MenuTick();
        }

        if (GameSettings.Keybindings.GetKey("Pause"))
        {
            OnExit();
            World.userInterface.CloseWindows();
        }
    }

    void Wrap(ref int index, int max)
    {
        if (index < 0)
            index = max;
        else if (index > max)
            index = 0;
    }

    public virtual void OnEnter()
    {

    }

    public virtual void OnExit()
    {

    }
}
