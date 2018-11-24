using UnityEngine;

public class UIPanel : MonoBehaviour
{
    protected int SelectedNum;
    protected int SelectedMax;
    protected bool initialized;

    float scroll;

    void OnDisable()
    {
        initialized = false;
    }

    public virtual void Initialize()
    {
        initialized = true;
    }

    public virtual void Update()
    {
        if (!initialized)
            return;

        if (GameSettings.Keybindings.GetKey("North"))
            ChangeSelectedNum(SelectedNum - 1);
        else if (GameSettings.Keybindings.GetKey("South"))
            ChangeSelectedNum(SelectedNum + 1);
        else if (GameSettings.Keybindings.GetKey("Enter"))
            OnSelect(SelectedNum);
    }

    protected virtual void OnSelect(int index) {}

    public virtual void ChangeSelectedNum(int newIndex)
    {
        SelectedNum = newIndex;

        if (SelectedNum < 0)
            SelectedNum = SelectedMax - 1;
        else if (SelectedNum >= SelectedMax)
            SelectedNum = 0;
    }
}
