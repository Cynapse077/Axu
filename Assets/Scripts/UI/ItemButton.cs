using UnityEngine;
using UnityEngine.UI;

public class ItemButton : MonoBehaviour
{
    public Image icon;
    public bool selected;

    Vector3 selectedSize = new Vector3(1.5f, 1.5f, 1f);

    public void Setup(Sprite img)
    {
        icon.sprite = img;
    }

    void Update()
    {
        icon.rectTransform.localScale = Vector3.Lerp(icon.rectTransform.localScale, (selected) ? selectedSize : Vector3.one, Time.deltaTime * 10f);
    }
}
