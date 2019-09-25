using UnityEngine;
using UnityEngine.UI;

public class AbilityButton : MonoBehaviour
{
    public Text nameText;
    public Image image;
    public bool selected;

    Vector3 selectedSize = new Vector3(1.5f, 1.5f, 1f);

    public void Setup(Ability s)
    {
        nameText.text = s.Name;
        image.sprite = s.IconSprite;

        if (s.cooldown > 0)
        {
            nameText.text = string.Format("<color=grey>{0} ({1})</color>", s.Name, s.cooldown);
        }
    }

    void Update()
    {
        image.rectTransform.localScale = Vector3.Lerp(image.rectTransform.localScale, (selected) ? selectedSize : Vector3.one, Time.deltaTime * 10f);
    }
}
