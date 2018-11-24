using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour
{
    public Text NameText;

    public void Setup(Skill s, int index)
    {
        NameText.alignment = TextAnchor.MiddleLeft;

        string n = "  " + s.Name;

        if (index < 9)
            n = "  " + (index + 1).ToString() + ") " + s.Name;
        else if (index == 9)
            n = "  0) " + s.Name;

        NameText.text = n;

        if (s.cooldown > 0)
            NameText.text = "<color=grey>" + n + " (" + s.cooldown + ")</color>";
    }
}
