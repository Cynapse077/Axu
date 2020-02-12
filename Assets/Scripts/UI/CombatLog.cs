using System.Text;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public static class CombatLog
{
    static string Arrow
    {
        get
        {
            char arrow = '\u25BA';
            return arrow.ToString();
        }
    }

    public static void NewMessage(string message)
    {
        Append(message);
    }

    public static void SimpleMessage(string key)
    {
        Append(LocalizationManager.GetContent(key));
    }

    public static void NameItemMessage(string key, string n, string item)
    {
        StringBuilder sb = new StringBuilder(LocalizationManager.GetContent(key));

        sb.Replace("[ITEM]", item);
        sb.Replace("[NAME]", n);

        Append(sb.ToString());
    }

    public static void Action(string key, string action, string itemName)
    {
        StringBuilder sb = new StringBuilder(LocalizationManager.GetContent(key));

        sb.Replace("[ACTION]", action);
        sb.Replace("[NAME]", itemName);

        Append(sb.ToString());
    }

    public static void CombatMessage(string key, string attacker, string defender, bool defenderIsPlayer)
    {
        string colorCode = defenderIsPlayer ? "<color=orange>" : "<color=cyan>";

        StringBuilder sb = new StringBuilder(colorCode + LocalizationManager.GetContent(key) + "</color>");

        sb.Replace("[ATTACKER]", attacker);
        sb.Replace("[DEFENDER]", defender);

        Append(sb.ToString());
    }

    public static void NewIndirectCombat(string key, int dmg, string source, string defender, string defBP, bool defenderIsPlayer)
    {
        string colorCode = defenderIsPlayer ? "<color=orange>" : "<color=cyan>";

        StringBuilder sb = new StringBuilder(colorCode + LocalizationManager.GetContent(key) + "</color>");

        sb.Replace("[SOURCE]", source);
        sb.Replace("[DEFENDER]", defender);
        sb.Replace("[DAMAGE]", dmg.ToString());
        sb = sb.Replace("[BODY PART]", defBP);

        Append(sb.ToString());
    }

    public static void NewSimpleCombat(string key, int dmg, string defender, bool defenderIsPlayer)
    {
        string colorCode = defenderIsPlayer ? "<color=orange>" : "<color=cyan>";

        StringBuilder sb = new StringBuilder(colorCode + LocalizationManager.GetContent(key) + "</color>");

        sb.Replace("[DEFENDER]", defender);
        sb.Replace("[DAMAGE]", dmg.ToString());

        Append(sb.ToString());
    }

    public static void Combat_Full(bool defenderIsPlayer, int dmg, bool crit, string defender, bool miss = false, string attacker = "", string defBP = "", string item = "")
    {
        string key = crit ? "Damage_Weapon_Crit" : "Damage_Weapon";
        if (dmg <= 0)
            key = "Block_Defense";
        if (miss)
            key = "Miss";
        string colorCode = defenderIsPlayer ? "<color=orange>" : "<color=cyan>";

        StringBuilder sb = new StringBuilder(colorCode + LocalizationManager.GetContent(key) + "</color>");

        sb.Replace("[ATTACKER]", attacker);
        sb.Replace("[DEFENDER]", defender);
        sb.Replace("[DAMAGE]", dmg.ToString());
        sb = sb.Replace("[BODY PART]", defBP);
        sb = sb.Replace("[ITEM]", item);

        Append(sb.ToString());
    }

    public static void NameMessage(string key, string name, string textToReplace = "[NAME]")
    {
        Append(LocalizationManager.GetContent(key).Replace(textToReplace, name));
    }

    public static void DisplayItemsBelow(Inventory inv)
    {
        string message = "";
        bool addedLast = false;
        const int max = 3;

        for (int i = 0; i < inv.items.Count; i++)
        {
            if (i < max)
                message += " " + inv.items[i].DisplayName() + (inv.items[i].stackable && inv.items[i].amount > 1 ? " x" + inv.items[i].amount : "") + ",";
            else if (i == max)
                message += " " + inv.items[i].DisplayName() + (inv.items[i].stackable && inv.items[i].amount > 1 ? " x" + inv.items[i].amount : "");
            else if (i == max + 1 && !addedLast)
            {
                message += " ...";
                addedLast = true;
            }
        }

        if (inv.items.Count > 0)
            NameMessage("Pass_By", message);
    }

    static void Append(string text)
    {
        World.userInterface.NewLogMessage(Arrow + text.CapFirst());
    }
}
