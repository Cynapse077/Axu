using LitJson;

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

    void FromJson(JsonData dat)
    {
        dat.TryGetString("Title", out title);
        dat.TryGetString("Text", out contents);
    }

    public void Read()
    {
        Alert.CustomAlert_WithTitle(title, contents);
    }
}
