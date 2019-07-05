[System.Serializable]
[MoonSharp.Interpreter.MoonSharpUserData]
public class Addiction
{
    public string addictedID;
    public int chanceToAddict, lastTurnTaken, currUse;
    public bool addicted, withdrawal;
    Trait applicableTrait;

    const int TimeToCure = 20000;
    const int TimeToWithdrawal = 4000;
    const int CravingReminderTime = 800;
    const int FalloffTime = 3000;

    int timeBetweenDoses
    {
        get { return World.turnManager.turn - lastTurnTaken; }
    }

    public string Name
    {
        get
        {
            if (ItemList.GetItemByID(addictedID) != null)
            {
                return ItemList.GetItemByID(addictedID).DisplayName() + " Addiction";            
            }
            else
            {
                return ItemList.GetLiquidByID(addictedID).Name + " Addiction";
            }
        }
    }

    string itemDisplay
    {
        get
        {
            if (ItemList.GetItemByID(addictedID) != null)
            {
                return ItemList.GetItemByID(addictedID).DisplayName();
            }
            else
            {
                return ItemList.GetLiquidByID(addictedID).Name;
            }
        }
    }

    public Addiction(string _itemID, bool _addicted, bool _withdrawal, int ltt, int chance, int _currUse)
    {
        addictedID = _itemID;
        addicted = _addicted;
        withdrawal = _withdrawal;
        lastTurnTaken = ltt;
        chanceToAddict = chance;
        currUse = _currUse;

        applicableTrait = TraitList.GetTraitByID("addiction_" + addictedID);
        World.turnManager.incrementTurnCounter += IncrementTurnCounter;
    }

    public Addiction(string id, int chance)
    {
        addictedID = id;
        chanceToAddict = chance;
        withdrawal = false;
        addicted = false;

        lastTurnTaken = World.turnManager.turn;
        applicableTrait = TraitList.GetTraitByID("addiction_" + addictedID);
        World.turnManager.incrementTurnCounter += IncrementTurnCounter;
    }

    public void ItemUse(Stats stats)
    {
        int val = World.difficulty.Level == Difficulty.DiffLevel.Rogue ? 4 : 2;
        lastTurnTaken = World.turnManager.turn;
        currUse++;

        if (!addicted)
        {
            int newChance = (chanceToAddict * (currUse)) / val;

            if (SeedManager.combatRandom.Next(100) < newChance)
            {
                FullAddiction(stats);
            }
        }
        else if (withdrawal)
        {
            withdrawal = false;
            AffectStats(stats, true);
        }
    }

    public void Cure(Stats stats)
    {
        if (addicted)
        {
            World.turnManager.incrementTurnCounter -= IncrementTurnCounter;

            if (withdrawal)
            {
                AffectStats(stats, true);
            }

            CombatLog.NameMessage("Message_Add_Subside", itemDisplay);
            stats.RemoveTrait("addiction_" + addictedID);
            stats.addictions.Remove(this);
        }
    }

    void FullAddiction(Stats stats)
    {
        addicted = true;
        Alert.NewAlert("Become_Addicted");
        stats.GiveTrait("addiction_" + addictedID);
    }

    void IncrementTurnCounter()
    {
        if (addicted)
        {
            //Fully Shrugged it off
            if (timeBetweenDoses >= TimeToCure)
            {
                Cure(ObjectManager.playerEntity.stats);
            }
            //Enter into a new withdrawal phase
            else if (timeBetweenDoses >= TimeToWithdrawal && !withdrawal)
            {
                withdrawal = true;
                AffectStats(ObjectManager.playerEntity.stats, false);
                CombatLog.NameMessage("Message_Add_Withdrawals", itemDisplay);
            }
            else
            {
                if (World.turnManager.turn % CravingReminderTime == 0)
                {
                    CombatLog.NameMessage("Message_Add_Crave", itemDisplay);
                    ObjectManager.playerEntity.CancelWalk();
                }
            }
        }
        else
        {
            //If not addicted, slowly reduce number of doses given. Decreases chance of getting addicted over time.
            if (timeBetweenDoses >= FalloffTime)
            {
                currUse--;
                lastTurnTaken = World.turnManager.turn;
            }
        }
    }

    void AffectStats(Stats stats, bool cure)
    {
        int am = cure ? -1 : 1;

        foreach (Stat_Modifier sm in applicableTrait.stats)
        {
            stats.Attributes[sm.Stat] += (sm.Amount * am);
        }
    }
}