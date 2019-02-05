using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CyberneticsPanel : UIPanel
{
    public Transform cybAnchor;
    public GameObject cybButton;

    Mode mode;
    Body currentBody;

    public override void Initialize()
    {
        mode = Mode.BodyPart;
        base.Initialize();
    }

    public void SetupLists(Body bod)
    {
        currentBody = bod;
        mode = Mode.BodyPart;
        Initialize();

        for (int i = 0; i < bod.bodyParts.Count; i++)
        {
            GameObject g = Instantiate(cybButton, cybAnchor);
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
        }
    }

    public override void Update()
    {
        base.Update();
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        base.ChangeSelectedNum(newIndex);
    }

    protected override void OnSelect(int index)
    {
        switch (mode)
        {
            case Mode.BodyPart:
                mode = Mode.Cyber;
                break;

            case Mode.Cyber:

                break;
        }
    }

    enum Mode
    {
        BodyPart, Cyber
    }
}
