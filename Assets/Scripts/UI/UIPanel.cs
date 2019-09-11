using UnityEngine;
using System.Collections;

public abstract class UIPanel : MonoBehaviour
{
    public int SelectedNum { get; protected set; }

    protected int SelectedMax;
    protected bool initialized;

    bool canHoldKeys = false;
    bool waitForRefresh = false;
    float moveTimer = 0.0f;

    protected abstract void OnSelect(int index);

    void OnDisable()
    {
        initialized = false;
    }

    public virtual void Initialize()
    {
        initialized = true;
        moveTimer = 0.0f;
        waitForRefresh = false;
    }

    public virtual void Update()
    {
        if (initialized)
        {
            if (GameSettings.Keybindings.GetKey("Enter"))
            {
                OnSelect(SelectedNum);
            }

            if (GameSettings.Keybindings.GetKey("North", KeyPress.Up) || GameSettings.Keybindings.GetKey("South", KeyPress.Up))
            {
                moveTimer = 0;
            }
            else if (GameSettings.Keybindings.GetKey("North", KeyPress.Held) || GameSettings.Keybindings.GetKey("South", KeyPress.Held))
            {
                moveTimer += Time.deltaTime;
                canHoldKeys = moveTimer >= 0.25f;
            }

            if (canHoldKeys)
            {
                HeldKeys();
            }
            else
            {
                PressedKeys();
            }
        }
    }

    void PressedKeys()
    {
        if (GameSettings.Keybindings.GetKey("North"))
        {
            ChangeSelectedNum(SelectedNum - 1, true);
        }
        else if (GameSettings.Keybindings.GetKey("South"))
        {
            ChangeSelectedNum(SelectedNum + 1, true);
        }
    }

    void HeldKeys()
    {
        if (!waitForRefresh)
        {
            if (GameSettings.Keybindings.GetKey("North", KeyPress.Held))
            {
                ChangeSelectedNum(SelectedNum - 1, true);
                StartCoroutine(HeldKeyRefresh());
            }
            else if (GameSettings.Keybindings.GetKey("South", KeyPress.Held))
            {
                ChangeSelectedNum(SelectedNum + 1, true);
                StartCoroutine(HeldKeyRefresh());
            }
        }
    }

    IEnumerator HeldKeyRefresh()
    {
        waitForRefresh = true;
        yield return new WaitForSeconds(0.1f);
        waitForRefresh = false;
    }

    public virtual void ChangeSelectedNum(int newIndex, bool scroll)
    {
        SelectedNum = newIndex;

        if (SelectedNum < 0)
        {
            SelectedNum = SelectedMax - 1;
        }
        else if (SelectedNum >= SelectedMax)
        {
            SelectedNum = 0;
        }
    }
}
