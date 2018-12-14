using UnityEngine;
using System.Collections.Generic;
using System;

public class DialogueController : MonoBehaviour
{
    public List<DialogueChoice> dialogueChoices { get; protected set; }
    public NPC myNPC { get; protected set; }

    Action acceptQuest;
    Quest myQuest;
    Journal journal;

    public void SetupDialogueOptions()
    {
        if (ObjectManager.playerJournal == null)
            return;

        myNPC = GetComponent<BaseAI>().npcBase;
        journal = ObjectManager.playerJournal;

        GetComponent<NPCSprite>().questIcon.SetActive(QuestIconActive());

        if (!string.IsNullOrEmpty(myNPC.questID))
        {
            myQuest = QuestList.GetByID(myNPC.questID);
        }

        if (!myNPC.HasFlag(NPC_Flags.Can_Speak))
            return;

        if (dialogueChoices == null)
            dialogueChoices = new List<DialogueChoice>();
        else
            dialogueChoices.Clear();

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
                World.userInterface.YesNoAction("\n" + myQuest.startDialogue, acceptQuest, null, "");
            }));
        }

        dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Chat"), () => { World.userInterface.Dialogue_Chat(myNPC.faction, myNPC.ID); }));

        if (myNPC.dialogueID != "")
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Talk"), () => { World.userInterface.Dialogue_Inquire(myNPC.dialogueID); }));

        if (myNPC.HasFlag(NPC_Flags.Merchant) || myNPC.HasFlag(NPC_Flags.Doctor) || myNPC.HasFlag(NPC_Flags.Book_Merchant))
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Trade"), () => { World.userInterface.Dialogue_Shop(); }));

        if (myNPC.HasFlag(NPC_Flags.Doctor))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Heal Wounds"), () => { World.userInterface.Dialogue_Heal(); }));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Replace Limb"), () => { World.userInterface.Dialogue_ReplaceLimb(); }));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Amputate Limb"), () => { World.userInterface.Dialogue_AmputateLimb(); }));
        }

        if (myNPC.HasFlag(NPC_Flags.Mercenary) && !myNPC.HasFlag(NPC_Flags.Follower))
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Hire"), () => { Hire(); }));

        if (myNPC.HasFlag(NPC_Flags.Follower) && !myNPC.HasFlag(NPC_Flags.Deteriortate_HP) && !myNPC.HasFlag(NPC_Flags.Static))
        {
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Trade"), () => {
                World.userInterface.Dialogue_Shop();
            }));
            dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Manage_Equipment"), () => {
                World.userInterface.CloseWindows();
                World.userInterface.OpenInventory(GetComponent<Inventory>());
            }));

            //Send back to base/pick up
            if (journal.HasFlag(ProgressFlags.Found_Base))
            {
                if (myNPC.HasFlag(NPC_Flags.At_Home))
                {
                    dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Follow_Me"), () => {
                        if (World.objectManager.NumFollowers() < 3)
                        {
                            myNPC.flags.Remove(NPC_Flags.At_Home);
                            World.userInterface.Dialogue_CustomChat(LocalizationManager.GetContent("Dialogue_Follow"));

                            CombatLog.NameMessage("Message_New_Follower", myNPC.name);
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
                        myNPC.worldPosition = World.tileMap.worldMap.GetClosestLandmark("Home Base");
                        GetComponent<BaseAI>().worldPos = myNPC.worldPosition;
                        CombatLog.NameMessage("Message_Sent_Home", myNPC.name);
                        World.tileMap.HardRebuild();
                        World.userInterface.CloseWindows();
                    }));
                }
            }
        }

        dialogueChoices.Add(new DialogueChoice(LocalizationManager.GetContent("Dialogue_Leave"), () => { World.userInterface.CloseWindows(); }));
    }

    bool QuestIconActive()
    {
        //TODO: Enable green marker when a quest can be turned in.
        return (!myNPC.isHostile && !myNPC.hostilityOverride && !myNPC.HasFlag(NPC_Flags.Follower) && myQuest != null);
    }

    void Hire()
    {
        World.userInterface.YesNoAction("YN_Hire", () => HireMe(), null, 300.ToString());
    }

    public void HireMe()
    {
        int cost = 300;

        if (ObjectManager.playerEntity.inventory.gold < cost)
        {
            Alert.NewAlert("Hire_No_Money", UIWindow.Dialogue);
        }
        else if (World.objectManager.NumFollowers() < 3)
        {
            ObjectManager.playerEntity.inventory.gold -= cost;
            World.userInterface.CloseWindows();
            GetComponent<BaseAI>().HireAsFollower();
        }
        else
        {
            Alert.NewAlert("Hire_Too_Many_Followers", UIWindow.Dialogue);
        }
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