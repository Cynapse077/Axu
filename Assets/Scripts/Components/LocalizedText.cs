using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    public string key;
    public bool setBySelf = false;
    string _baseText;
    Text myText;

    public string BaseText
    {
        get
        {
            if (!ModManager.PreInitialized)
                return "";

            if (string.IsNullOrEmpty(_baseText))
                GetLocalizedText(key);

            return _baseText;
        }
    }

    void Start()
    {
        myText = GetComponent<Text>();

        if (setBySelf && myText != null)
        {
            myText.text = BaseText;
        }
    }

    public void GetLocalizedText(string searchKey = null)
    {
        if (searchKey.NullOrEmpty())
        {
            searchKey = key;
        }

        TranslatedText content = LocalizationManager.GetLocalizedContent(searchKey);

        _baseText = content.display;

        if (GetComponent<OnHover_ShowTooltip>() != null)
        {
            GetComponent<OnHover_ShowTooltip>().textToDisplay = content.display2;
        }
    }

    public void SetText(string searchKey)
    {
        if (myText == null)
        {
            myText = GetComponent<Text>();
        }

        GetLocalizedText(searchKey);
        myText.text = BaseText;
    }
}
