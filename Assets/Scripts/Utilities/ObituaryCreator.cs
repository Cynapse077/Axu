using System.Text;

public static class ObituaryCreator
{
    static readonly string[] deathCauses = new string[]
    {
        "Torn to pieces by Scrags.",
        "Assaulted by Puggles.",
        "Dug too deep and too greedily.",
        "Made a dangerous bet. They lost.",
        "Sold their organs for pocket change.",
        "Thought they could fly.\nThey could not.",
        "Ate a piece of unidentified meat from the ground.",
        "Tried to wrestle an alpha scrag.",
        "Ate a whole mutant. The whole thing. With a fork.",
        "Wrote the Ensis Labs Safety Standards. Hundreds died in the resulting fire, including them.",
        "Believed bathing in radiation would give them super powers.",
        "Starved after being told hunger had been removed.",
        "Decapitated themselves to swap heads.",
        "Drank their own vomit. Vomited it up. Rinse and repeat.",
        "Shot in the head.",
        "Death by natural causes. The rarest death there is on Axu."
    };

    static readonly string[] finalWords = new string[]
    {
        "They will be dearly missed.",
        "Good riddance.",
        "Sunscreen saves lives.",
        "Good job, dumnbass. I mean... \"dumbass\".",
        "Alas, we hardly new ye.",
        "Will not be missed.",
        "\"Are you truly certain its me buried here?\"",
        "We took all the valuables. Please don't dig them up.",
        "What an ass."
    };

    static readonly string[] alternateNames = new string[]
    {
        "<i><color=grey>[The stone is worn. You could not make out a name.]>/color></i>",
        "A man who sold his world.",
        "Sin",
        "John",
        "Icarus",
        "Shorn",
        "Moog"
    };

    public static string GetNewObituary(Coord wPos, Coord lPos)
    {
        System.Random ran = new System.Random((wPos + lPos).GetHashCode());
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