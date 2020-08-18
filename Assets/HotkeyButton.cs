using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HotkeyButton : MonoBehaviour, IPointerClickHandler
{
    public KeyCode keyCode;
    public Image icon;
    public Image cooldown;
    public Text indexText;
    Action onUse;
    Ability ability;

    public void Initialize(int index)
    {
        int id = index + 1;

        if (id > 9)
        {
            id = 0;
        }

        string kc = "Alpha" + id;

        keyCode = kc.ToEnum<KeyCode>();
        indexText.text = id.ToString();
    }

    public void Setup(Ability ab, Action action)
    {
        ability = ab;
        onUse = action;
        icon.sprite = ability.IconSprite;
    }

    public void DoAction()
    {
        onUse?.Invoke();
    }

    void Update()
    {
        if (ability != null)
        {
            cooldown.fillAmount = ability.cooldown / (float)ability.maxCooldown;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        DoAction();
    }
}