using UnityEngine;
using LitJson;

public class TranslatedText : IAsset
{
    public const string defaultText = "Untranslated Data";
    public string ID { get; set; }
    public string ModID { get; set; }
    public string display;
    public string display2;

    public TranslatedText() { }
    public TranslatedText(JsonData dat)
    {
        FromJson(dat);
    }

    void FromJson(JsonData dat)
    {
        ID = dat["ID"].ToString();
        dat.TryGetValue("Display", out display, defaultText);

        if (dat.ContainsKey("Tooltip"))
            dat.TryGetValue("Tooltip", out display2, defaultText);
        else if (dat.ContainsKey("Message"))
            dat.TryGetValue("Message", out display2, defaultText);
    }
}