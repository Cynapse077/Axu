using System.Collections.Generic;
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

    public void FromJson(JsonData dat)
    {
        ID = dat["ID"].ToString();
        dat.TryGetString("Display", out display, defaultText);

        if (dat.ContainsKey("Tooltip"))
            dat.TryGetString("Tooltip", out display2, defaultText);
        else if (dat.ContainsKey("Message"))
            dat.TryGetString("Message", out display2, defaultText);
    }

    public IEnumerable<string> LoadErrors()
    {
        if (display.NullOrEmpty())
        {
            yield return "Display text is null or empty.";
        }
    }
}