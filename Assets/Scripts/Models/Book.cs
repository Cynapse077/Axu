using LitJson;
using System.Collections.Generic;

public class Book : IAsset
{
    public string ID { get; set; }
    public string ModID { get; set; }
    public string title;
    public string contents;

    public Book() { }
    public Book(JsonData dat)
    {
        FromJson(dat);
    }

    public void FromJson(JsonData dat)
    {
        dat.TryGetString("Title", out title, title);
        dat.TryGetString("Text", out contents, contents);
        ID = title;
    }

    public void Read()
    {
        Alert.CustomAlert_WithTitle(title, contents);
    }

    public IEnumerable<string> LoadErrors()
    {
        if (title.NullOrEmpty())
        {
            yield return "Title not set.";
        }

        if (contents.NullOrEmpty())
        {
            yield return "No contents text set.";
        }
    }
}
