using UnityEngine;
using UnityEngine.UI;

public class LevelUpButton : MonoBehaviour
{
    public Text titleText;
    public Text descriptionText;

    Trait myTrait;

    public void SetValues(Trait t)
    {
        myTrait = t;
        titleText.text = myTrait.name;
        descriptionText.text = myTrait.description;
    }

    public Trait GetTrait()
    {
        return myTrait;
    }
}
