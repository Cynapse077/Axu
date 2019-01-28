using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoctorActionsPanel : UIPanel
{
    public Transform optionAnchor;
    public GameObject buttonObject;

    public override void Initialize()
    {
        SelectedNum = 0;
        base.Initialize();
    }

    void RefreshActions()
    {
        
    }

    protected override void OnSelect(int index)
    {
        base.OnSelect(index);
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        base.ChangeSelectedNum(newIndex);
    }

    public override void Update()
    {
        base.Update();
    }
}
