using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite spriteNormal;
    public Sprite spriteHover;
    public bool selected;

    Action onClick;
    Action onHover;
    Action onLeave;
    Image image;
    Text text;
    bool mouseOver;

    void Start()
    {
        image = GetComponent<Image>();
        text = GetComponentInChildren<Text>();
        image.sprite = spriteNormal;
    }

    void OnDisable()
    {
        Reset();
    }

    void Update()
    {
        if ((mouseOver || selected) && Input.GetMouseButtonDown(0))
        {
            DoAction_Click();
        }
    }

    public void Initialize(string txt, Action action)
    {
        SetText(txt);
        SetClickAction(action);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.sprite = spriteHover;
        mouseOver = true;
        selected = true;

        DoAction_Hover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.sprite = spriteNormal;
        mouseOver = false;
        selected = false;

        DoAction_Leave();
    }

    void SetClickAction(Action action)
    {
        onClick = action;
    }

    public void SetHoverAction(Action action)
    {
        onHover = action;
    }

    public void SetLeaveAction(Action action)
    {
        onLeave = action;
    }

    void SetText(string txt)
    {
        text.text = txt;
    }

    void DoAction_Click()
    {
        if (onClick != null)
        {
            onClick();
        }
    }

    void DoAction_Hover()
    {
        if (onHover != null)
        {
            onHover();
        }
    }

    void DoAction_Leave()
    {
        if (onLeave != null)
        {
            onLeave();
        }
    }

    void Reset()
    {
        text.text = "";
        image.sprite = spriteNormal;
        onClick = null;
        onHover = null;
        onLeave = null;
    }
}
