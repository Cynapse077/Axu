using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DialoguePanel : MonoBehaviour
{
    public GameObject title;
    public GameObject text;
    public GameObject button;

    [HideInInspector]
    public string chatText = "";
    [HideInInspector]
    public int cMax;
    public bool nodeDialogue = false;

    DialogueController controller;
    Text titleText, dialogueText;

    public void Display(DialogueController dc)
    {
        transform.DespawnChildren();

        controller = dc;
        nodeDialogue = false;
        cMax = dc.dialogueChoices.Count - 1;

        GameObject t = SimplePool.Spawn(title, transform);
        titleText = t.GetComponentInChildren<Text>();
        titleText.text = controller.gameObject.name;

        GameObject txt = SimplePool.Spawn(text, transform);
        dialogueText = txt.GetComponentInChildren<Text>();
        dialogueText.text = Dialogue.Chat("Greetings");

        for (int i = 0; i < controller.dialogueChoices.Count; i++)
        {
            GameObject g = SimplePool.Spawn(button, transform);
            Text gText = g.GetComponentInChildren<Text>();
            gText.text = controller.dialogueChoices[i].text;

            if (controller.dialogueChoices[i].text == "Heal Wounds")
                gText.text = GetVariableName(gText.text, ObjectManager.playerEntity.stats.CostToCureWounds() == 0, ObjectManager.playerEntity.stats.CostToCureWounds());
            else if (controller.dialogueChoices[i].text == "Replace Limb")
                gText.text = GetVariableName(gText.text, ObjectManager.playerEntity.stats.CostToReplaceLimbs() < 0, ObjectManager.playerEntity.stats.CostToReplaceLimbs());

            g.GetComponent<Button>().onClick.AddListener(() => ChooseDialogue());
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Dialogue);
        }

        EventSystem.current.SetSelectedGameObject(transform.GetChild(2).gameObject);
    }

    public void Display(DialogueNode node)
    {
        transform.DestroyChildren();
        cMax = node.options.Length - 1;
        nodeDialogue = true;

        GameObject t = Instantiate(title, transform);
        titleText = t.GetComponentInChildren<Text>();
        titleText.text = controller.gameObject.name;

        GameObject txt = Instantiate(text, transform);
        dialogueText = txt.GetComponentInChildren<Text>();
        dialogueText.text = node.display;

        for (int i = 0; i < node.options.Length; i++)
        {
            if (!node.nullifyingFlag.NullOrEmpty() && ObjectManager.playerJournal.HasFlag(node.nullifyingFlag))
            {
                cMax--;
                continue;
            }

            if (!node.requiredFlag.NullOrEmpty() && !ObjectManager.playerJournal.HasFlag(node.requiredFlag))
            {
                cMax--;
                continue;
            }

            GameObject g = Instantiate(button, transform);
            g.GetComponentInChildren<Text>().text = node.options[i].display;

            string nID = node.options[i].nextID;

            g.GetComponent<Button>().onClick.AddListener(() => GetNextNode(nID));
            g.GetComponent<Button>().onClick.AddListener(() => { 
                if (!node.flag.NullOrEmpty())
                {
                    ObjectManager.playerJournal.AddFlag(node.flag);
                }
            });
            g.GetComponent<OnHover_SetSelectedIndex>().SetHoverMode(2, UIWindow.Dialogue);
            g.GetComponent<OnHover_SetSelectedIndex>().offset = 2;
        }

        if (transform.childCount > 1)
        {
            EventSystem.current.SetSelectedGameObject(transform.GetChild(2).gameObject);
        }
    }

    void Update()
    {
        if (World.userInterface.CurrentState == UIWindow.Dialogue && transform.childCount > 1)
        {
            EventSystem.current.SetSelectedGameObject(transform.GetChild(UserInterface.selectedItemNum + 2).gameObject);
        }
    }

    public void SetText(string t)
    {
        dialogueText.text = t;
    }

    public void ChooseDialogue()
    {
        if (nodeDialogue)
        {
            transform.GetChild(UserInterface.selectedItemNum + 2).GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            controller.dialogueChoices[UserInterface.selectedItemNum].callBack();
        }
    }

    void GetNextNode(string node)
    {
        World.userInterface.Dialogue_Inquire(node);
    }

    string GetVariableName(string input, bool notNeeded, int amount)
    {
        return (notNeeded) ? (input + "<color=grey>(Not Needed)</color>") : (input + " - <color=yellow>$</color>" + amount);
    }
}
