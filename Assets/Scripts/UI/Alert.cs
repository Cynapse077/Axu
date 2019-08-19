
[MoonSharp.Interpreter.MoonSharpUserData]
public static class Alert
{
    static UIWindow previousWindow = UIWindow.None;

    static UserInterface userInterface
    {
        get { return World.userInterface; }
    }

    public static void CustomAlert(string content)
    {
        userInterface.NewAlert("", content);
        previousWindow = UIWindow.None;
    }

    public static void CustomAlert_WithTitle(string title, string content)
    {
        userInterface.NewAlert(title, content);
        previousWindow = UIWindow.None;
    }

    public static void NewAlert(string alertKey, UIWindow _previousWindow = UIWindow.None)
    {
        TranslatedText t = LocalizationManager.GetLocalizedContent(alertKey);
        string title = t.display;
        string message = t.display2;

        userInterface.NewAlert(title, message);
        previousWindow = _previousWindow;
    }

    public static void NewAlert(string alertKey, string name, string input)
    {
        TranslatedText t = LocalizationManager.GetLocalizedContent(alertKey);
        string title = t.display;
        string message = t.display2;

        if (message.Contains("[NAME]"))
        {
            if (name != null && name != "")
            {
                message = message.Replace("[NAME]", name);
            }
        }
        if (message.Contains("[INPUT]"))
        {
            if (input != null && input != "")
            {
                message = message.Replace("[INPUT]", input);
            }
        }

        userInterface.NewAlert(title, message);
        previousWindow = UIWindow.None;
    }

    public static void CloseAlert()
    {
        userInterface.CloseAlert();
        userInterface.OpenRelevantWindow(previousWindow);
        previousWindow = UIWindow.None;

        if (!World.userInterface.pickedTrait)
        {
            World.userInterface.LevelUp();
        }
    }
}
