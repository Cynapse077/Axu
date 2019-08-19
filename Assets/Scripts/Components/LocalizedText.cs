using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    public string key;
    public bool setBySelf = false;
    string _baseText;

    public string BaseText
    {
        get
        {
            if (string.IsNullOrEmpty(_baseText))
                GetLocalizedText(key);

            return _baseText;
        }
    }

    void Start()
    {
        if (setBySelf && GetComponent<Text>())
        {
            GetComponent<Text>().text = BaseText;
        }
    }

    public void GetLocalizedText(string searchKey = "")
    {
        if (searchKey == "")
            searchKey = key;

        TranslatedText content = LocalizationManager.GetLocalizedContent(searchKey);

        _baseText = content.display;

        if (GetComponent<OnHover_ShowTooltip>() != null)
        {
            GetComponent<OnHover_ShowTooltip>().textToDisplay = content.display2;
        }
    }

    public void SetText(string searchKey)
    {
        GetLocalizedText(searchKey);
        GetComponent<Text>().text = BaseText;
    }
}
