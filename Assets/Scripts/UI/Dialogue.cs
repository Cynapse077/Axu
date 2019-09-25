
public static class Dialogue
{
    public static string Chat(string id)
    {
        return (GameData.Get<DialogueSingle>(id)).dialogues.GetRandom();
    }

    public static string Chat(Faction faction)
    {
        if (faction != null)
            return (GameData.Get<DialogueSingle>(faction.ID)).dialogues.GetRandom();

        return "...";
    }

    public static void SelectPressed(DialogueController.DialogueChoice choice)
    {
        choice.callBack();
    }
}
