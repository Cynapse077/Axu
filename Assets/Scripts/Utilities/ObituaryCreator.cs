using System.Text;
using System.Collections.Generic;

public static class ObituaryCreator
{
    static readonly string[] deathCauses = new string[]
    {
        "Torn to pieces by Scrags.",
        "Assaulted by Puggles.",
        "Dug too deep and too greedily.",
        "Made a dangerous bet.",
        "Sold their organs for pocket change. The heart as not the best choice.",
        "Thought they could fly.\nThey could not.",
        "Are you truly certain its me buried here?",
        "He didn't die, but his cryopod won't open up.",
        "Ate a piece of unidentified meat from the ground.",
        "Tried to wrestle a alpha scrag.",
        "Ate a whole mutant. The whole thing. With a fork.",
        "Wrote the Ensis Safety Standards in the human labs. Obviously that didn't go well.",
        "Somehow believed bathing in radiation would give them super powers.",
        "Starved after being told hunger had been removed.",
        "Decapitated themselves to swap heads."
    };

    static readonly string[] finalWords = new string[]
    {
        "They will be dearly missed.",
        "Good riddance.",
        "Sunscreen saves lives.",
        "Good job, dumnbass.",
        "Alas, we hardly new ye.",
        "Will not be missed."
    };

    static readonly string[] alternateNames = new string[]
    {
        "<i><color=grey>[The stone is worn. You could not make out a name.]>/color></i>",
        "a man who sold his world.",
        "Sin",
        "John",
        "Icarus",
        "Shorn",
        "Moog"
    };

    public static string GetNewObituary(Coord wPos, Coord lPos)
    {
        int seed = (wPos + lPos).GetHashCode();
        System.Random ran = new System.Random(seed);
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("<color=grey>---------------</color>");
        sb.AppendLine(string.Format("Here lies {0}.", GetName(ran)));
        sb.AppendLine("<color=grey>---------------</color>");
        sb.AppendLine();
        sb.AppendLine();

        sb.AppendLine(deathCauses.GetRandom(ran));
        sb.AppendLine();

        sb.AppendLine(finalWords.GetRandom(ran));

        return sb.ToString();
    }

    static string GetName(System.Random ran)
    {
        if (ran.OneIn(100))
        {
            return alternateNames.GetRandom(ran);
        }

        return NameGenerator.CharacterName(ran);        
    }
}