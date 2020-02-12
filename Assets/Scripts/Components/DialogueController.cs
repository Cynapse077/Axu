using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class DialogueController : MonoBehaviour
{
    public List<DialogueChoice> dialogueChoices { get; protected set; }
    public NPC myNPC { get; protected set; }

    BaseAI bai;
    Action acceptQuest;
    Quest myQuest;
    Journal journal;

    const int costToHire = 300;

    public void SetupDialogueOptions()
    {
        if (ObjectManager.playerJournal == null)
        {
            return;
        }

        bai = GetComponent<BaseAI>();
        myNPC = bai.npcBase;
        journal = ObjectManager.playerJournal;

        GetComponent<NPCSprite>().questIcon.SetActive(QuestIconActive());

        if (!myNPC.HasFlag(NPC_Flags.Can_Speak))
        {
            return;
        }

        if (!myNPC.questID.NullOrEmpty() && !ObjectManager.playerJournal.HasCompletedQuest(myNPC.questID) && !ObjectManager.playerJournal.HasQuest(myNPC.questID))
        {
            if (GameData.TryGet(myNPC.questID, out Quest q))
            {
                myQuest = q.Clone();
            }            
        }

        if (dialogueChoices == null)
        {
            dialogueChoices = new List<DialogueChoice>();
        }
        else
        {
            dialogueChoices.Clear();
        }

        if (myQuest != null)
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Quest"), () => {
                acceptQuest = () => {
                    World.userInterface.CloseWindows();
                    journal.StartQuest(myQuest);
                    myNPC.questID = "";
                    myQuest = null;
                    SetupDialogueOptions();
                };
                World.userInterface.YesNoAction(myQuest.startDialogue, acceptQuest, null, "");
            }));
        }

        if (!string.IsNullOrEmpty(myNPC.dialogueID))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Chat"), () => World.userInterface.Dialogue_Inquire(myNPC.dialogueID)));
        } else
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Chat"), () => World.userInterface.Dialogue_Chat(myNPC.faction)));
        }

        if (myNPC.HasFlag(NPC_Flags.Merchant) || myNPC.HasFlag(NPC_Flags.Doctor) || myNPC.HasFlag(NPC_Flags.Book_Merchant))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Trade"), () => World.userInterface.Dialogue_Shop()));
        }

        if (myNPC.HasFlag(NPC_Flags.Doctor))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Heal Wounds"), () => World.userInterface.Dialogue_Heal()));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Replace Limb"), () => World.userInterface.Dialogue_ReplaceLimb()));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Amputate Limb"), () => World.userInterface.Dialogue_AmputateLimb()));
            dialogueChoices.Add(new DialogueChoice("Cybernetics (1x Bottle Cap)", () => World.userInterface.OpenCyberneticsPanel(ObjectManager.playerEntity.body)));
        }

        if (myNPC.HasFlag(NPC_Flags.Mercenary) && !bai.isFollower())
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Hire"), () => { Hire(); }));
        }

        if (bai.isFollower() && !myNPC.HasFlag(NPC_Flags.Deteriortate_HP) && !myNPC.HasFlag(NPC_Flags.Static))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Trade"), () => World.userInterface.Dialogue_Shop()));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Manage_Equipment"), () => {
                World.userInterface.CloseWindows();
                World.userInterface.OpenInventory(GetComponent<Inventory>());
            }));

            //Send back to base/pick up
            CheckBase();
        }

        CheckQuests();

        dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Leave"), () => World.userInterface.CloseWindows()));
    }

    void CheckQuests()
    {
        List<Quest> quests = ObjectManager.playerJournal.quests;

        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].ActiveGoal != null)
            {
                if (quests[i].ActiveGoal.goalType == "FetchPropertyGoal")
                {
                    FetchPropertyGoal fpg = (FetchPropertyGoal)quests[i].ActiveGoal;

                    if (fpg.npcTarget == myNPC.ID)
                    {
                        dialogueChoices.Add(new DialogueChoice("Hand Over Items", () => World.userInterface.GiveItem(fpg.itemProperty)));
                    }
                }
                else if (quests[i].ActiveGoal.goalType == "FetchGoal")
                {
                    FetchGoal fg = (FetchGoal)quests[i].ActiveGoal;

                    if (fg.npcTarget == myNPC.ID)
                    {
                        dialogueChoices.Add(new DialogueChoice("Hand Over Items", () => World.userInterface.GiveItem(fg.itemID)));
                    }
                }
                else if (quests[i].ActiveGoal.goalType == "Fetch_Homonculus")
                {
                    Fetch_Homonculus fh = (Fetch_Homonculus)quests[i].ActiveGoal;

                    if (fh.npcTarget == myNPC.ID)
                    {
                        dialogueChoices.Add(new DialogueChoice("Hand Over Items", () => World.userInterface.GiveItem(fh.itemProperty)));
                    }
                }
            }
        }
    }

    void CheckBase()
    {
        if (journal.HasFlag("Found_Base"))
        {
            if (myNPC.HasFlag(NPC_Flags.At_Home))
            {
                dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Follow_Me"), () => {
                    if (World.objectManager.NumFollowers() < 3)
                    {
                        myNPC.flags.Remove(NPC_Flags.At_Home);
                        World.userInterface.Dialogue_CustomChat(LocalizationManager.GetContent("Dialogue_Follow"));

                        CombatLog.NameMessage("Message_New_Follower", bai.entity.Name);
                        World.userInterface.CloseWindows();
                    }
                    else
                    {
                        World.userInterface.Dialogue_CustomChat(LocalizationManager.GetContent("Dialogue_TooManyFollowers"));
                    }
                }));
            }
            else
            {
                dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Send_Home"), () => {
                    myNPC.flags.Add(NPC_Flags.At_Home);
                    myNPC.worldPosition = World.tileMap.worldMap.GetClosestLandmark("Home");
                    CombatLog.NameMessage("Message_Sent_Home", bai.entity.Name);
                    World.tileMap.HardRebuild();
                    World.userInterface.CloseWindows();
                }));
            }
        }
    }

    bool QuestIconActive()
    {
        return !bai.isHostile && !myNPC.HasFlag(NPC_Flags.Follower) && myQuest != null;
    }

    void Hire()
    {
        World.userInterface.YesNoAction("YN_Hire".Localize(), () => HireMe(), null, costToHire.ToString());
    }

    public void HireMe()
    {
        if (ObjectManager.playerEntity.inventory.gold < costToHire)
        {
            Alert.NewAlert("Hire_No_Money".Localize(), UIWindow.Dialogue);
        }
        else if (World.objectManager.NumFollowers() < 3)
        {
            ObjectManager.playerEntity.inventory.gold -= costToHire;
            World.userInterface.CloseWindows();
            GetComponent<BaseAI>().HireAsFollower();
        }
        else
        {
            Alert.NewAlert("Hire_Too_Many_Followers", UIWindow.Dialogue);
        }

        SetupDialogueOptions();
    }

    public struct DialogueChoice
    {
        public string text;
        public Action callBack;

        public DialogueChoice(string _text, Action _callBack)
        {
            text = _text;
            callBack = _callBack;
        }
    }
}