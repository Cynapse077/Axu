using System.Text;
using LitJson;
using System.IO;

public static class ObituaryCreator
{
    private static string[] DeathCauses;
    private static string[] FinalWords;
    private static string[] AltNames;

    public static string GetNewObituary(Coord wPos, Coord lPos)
    {
        if (DeathCauses == null)
        {
            FillLists();
        }

        System.Random ran = new System.Random((wPos + lPos).GetHashCode());
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<color=grey>---------------</color>");
        sb.AppendLine(string.Format("HereLies".Localize(), GetName(ran)));
        sb.AppendLine("<color=grey>---------------</color>");
        sb.AppendLine();
        sb.AppendLine();

        sb.AppendLine(DeathCauses.GetRandom(ran));
        sb.AppendLine();

        sb.AppendLine(FinalWords.GetRandom(ran));

        return sb.ToString();
    }

    private static string GetName(System.Random ran)
    {
        if (ran.OneIn(100))
        {
            return AltNames.GetRandom(ran);
        }

        return NameGenerator.CharacterName(ran);
    }

    private static void FillLists()
    {
        string path = Path.Combine(ModUtility.ModFolderPath, "Core", "Dialogue", "Obituaries.json");

        string contents = File.ReadAllText(path);
        JsonData dat = JsonMapper.ToObject(contents);

        var causes = dat["Causes"];
        DeathCauses = new string[causes.Count];
        for (int i = 0; i < causes.Count; i++)
        {
            DeathCauses[i] = causes[i].ToString();
        }

        var finalWords = dat["Final Words"];
        FinalWords = new string[finalWords.Count];
        for (int i = 0; i < finalWords.Count; i++)
        {
            FinalWords[i] = finalWords[i].ToString();
        }

        var names = dat["Names"];
        AltNames = new string[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            AltNames[i] = names[i].ToString();
        }
    }
}