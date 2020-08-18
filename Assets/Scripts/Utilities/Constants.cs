using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Axu
{
    namespace Constants
    {
        public static class C_Attributes
        {
            public const string Health = "Health";
            public const string Stamina = "Stamina";

            public const string Strength = "Strength";
            public const string Dexterity = "Dexterity";
            public const string Endurance = "Endurance";
            public const string Intelligence = "Intelligence";

            public const string Perception = "Perception";
            public const string MoveSpeed = "Speed";
        }

        public static class C_StatusEffects
        {
            public const string Blind = "Blind";
            public const string Underwater = "Underwater";
            public const string Topple = "Topple";
            public const string Drunk = "Drunk";
            public const string Confused = "Confuse";
            public const string Stuck = "Stuck";
            public const string Stunned = "Stun";
            public const string OffBalance = "OffBalance";
            public const string Strangled = "Strangle";
        }

        public static class C_Factions
        {
            public const string Followers = "followers";

            public static string HostileTo_(Faction other)
            {
                return "HostileTo_" + other.ID;
            }
        }

        public static class C_Abilities
        {
            public const string Help = "help";
            public const string Grapple = "grapple";
        }

        public static class C_NPCs
        {
            public const string TheEmpty = "empty";
            public const string TheEmpty_Tentacle = "theempty-tentacle";
            public const string Villager = "villager";
        }

        public static class C_NPCGroups
        {
            public const string Slimeites = "Slimeites 1";
        }

        public static class C_Traits
        {
            public const string Leprosy = "leprosy";
            public const string Crystalization = "crystal";
            public const string Vampirism = "vampirism";
            public const string Fledgling = "pre_vamp";
        }

        public static class C_QuestFlags
        {
            public const string FoundBase = "Found_Base";
        }

        public static class C_Landmarks
        {
            public const string Home = "Home";
            public const string River = "River";
            public const string Village = "Village";
        }

        public static class C_Items
        {
            public const string None = "none";
            public const string Pool = "pool";
            public const string Fist = "fists";
        }

        public static class C_Objects
        {
            public const string Loot = "Loot";
            public const string Bed = "Bed";
            public const string Table = "Table";
            public const string Chair_Left = "Chair_Left";
            public const string Chair_Right = "Chair_Right";
            public const string Barrel = "Barrel";
        }
    }
}