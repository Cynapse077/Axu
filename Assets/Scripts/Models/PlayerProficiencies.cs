using System.Linq;
using System.Collections.Generic;

[MoonSharp.Interpreter.MoonSharpUserData]
public class PlayerProficiencies
{
    public Dictionary<string, WeaponProficiency> Profs;

    public PlayerProficiencies()
    {
        Profs = new Dictionary<string, WeaponProficiency>
        {
            { "Blade", new WeaponProficiency(LocalizationManager.GetContent("Blade")) },
            { "Blunt", new WeaponProficiency(LocalizationManager.GetContent("Blunt")) },
            { "Polearm", new WeaponProficiency(LocalizationManager.GetContent("Polearm")) },
            { "Axe", new WeaponProficiency(LocalizationManager.GetContent("Axe")) },
            { "Firearm", new WeaponProficiency(LocalizationManager.GetContent("Firearm")) },
            { "Throwing", new WeaponProficiency(LocalizationManager.GetContent("Throwing")) },
            { "Unarmed", new WeaponProficiency(LocalizationManager.GetContent("Unarmed")) },
            { "Misc", new WeaponProficiency(LocalizationManager.GetContent("Misc")) },

            { "Armor", new WeaponProficiency(LocalizationManager.GetContent("Armor")) },
            { "Shield", new WeaponProficiency(LocalizationManager.GetContent("Shield")) },
            { "Butchery", new WeaponProficiency(LocalizationManager.GetContent("Butchery")) },
            { "MartialArts", new WeaponProficiency(LocalizationManager.GetContent("MartialArts")) }
        };
    }

    public WeaponProficiency Blade
    {
        get { return Profs["Blade"]; }
        set { Profs["Blade"] = value; }
    }
    public WeaponProficiency Blunt
    {
        get { return Profs["Blunt"]; }
        set { Profs["Blunt"] = value; }
    }
    public WeaponProficiency Polearm
    {
        get { return Profs["Polearm"]; }
        set { Profs["Polearm"] = value; }
    }
    public WeaponProficiency Axe
    {
        get { return Profs["Axe"]; }
        set { Profs["Axe"] = value; }
    }
    public WeaponProficiency Firearm
    {
        get { return Profs["Firearm"]; }
        set { Profs["Firearm"] = value; }
    }
    public WeaponProficiency Throwing
    {
        get { return Profs["Throwing"]; }
        set { Profs["Throwing"] = value; }
    }
    public WeaponProficiency Unarmed
    {
        get { return Profs["Unarmed"]; }
        set { Profs["Unarmed"] = value; }
    }
    public WeaponProficiency Misc
    {
        get { return Profs["Misc"]; }
        set { Profs["Misc"] = value; }
    }
    public WeaponProficiency Armor
    {
        get { return Profs["Armor"]; }
        set { Profs["Armor"] = value; }
    }
    public WeaponProficiency Shield
    {
        get { return Profs["Shield"]; }
        set { Profs["Shield"] = value; }
    }
    public WeaponProficiency Butchery
    {
        get { return Profs["Butchery"]; }
        set { Profs["Butchery"] = value; }
    }
    public WeaponProficiency MartialArts
    {
        get { return Profs["MartialArts"]; }
        set { Profs["MartialArts"] = value; }
    }

    public WeaponProficiency GetProficiencyFromItem(Item item)
    {
        switch (item.itemType)
        {
            case Proficiencies.Blade: return Blade;
            case Proficiencies.Blunt: return Blunt;
            case Proficiencies.Axe: return Axe;
            case Proficiencies.Polearm: return Polearm;
            case Proficiencies.Firearm: return Firearm;
            case Proficiencies.Throw: return Throwing;
            case Proficiencies.Unarmed: return Unarmed;
            case Proficiencies.Armor: return Armor;
            case Proficiencies.Shield: return Shield;

            default: return Misc;
        }
    }

    public List<WeaponProficiency> GetProfs()
    {
        return Profs.Values.ToList();
    }
}
