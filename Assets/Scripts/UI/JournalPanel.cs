using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JournalPanel : UIPanel
{
    public Transform journalBase;
    public GameObject questObject;
    public Text tooltip;
    public GameObject tooltipGO;

    Journal journal;

    public override void Initialize()
    {
        if (journal == null)
            journal = ObjectManager.playerJournal;

        tooltipGO.SetActive(false);
        tooltip.text = "";
        OpenJournal();

        base.Initialize();
    }

    void OpenJournal()
    {
        journalBase.DespawnChildren();
        SelectedMax = 0;
        SelectedNum = 0;

        for (int i = 0; i < journal.quests.Count; i++)
        {
            GameObject g = SimplePool.Spawn(questObject, journalBase);
            g.GetComponentInChildren<Text>().text = (journal.quests[i].ID == journal.trackedQuest.ID)
                ? "<color=magenta>></color> " + journal.quests[i].Name + " <color=magenta><</color>" : journal.quests[i].Name;
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));

            SelectedMax++;
        }

        UpdateTooltip();

        if (SelectedMax > 0)
        {
            journalBase.GetChild(SelectedNum).Highlight();
        }
    }

    void UpdateTooltip()
    {
        if (SelectedMax > 0 && SelectedMax > SelectedNum)
        {
            tooltipGO.SetActive(true);
            Quest q = journal.quests[SelectedNum];

            if (q == null)
            {
                tooltip.text = "";
                return;
            }

            string desc = "<i>" + q.Description + "</i>", goal = "";

            if (q.goals.Length > 0)
            {
                for (int i = 0; i < q.goals.Length; i++)
                {
                    if (q.sequential)
                    {
                        if (q.goals[i].isComplete || q.goals[i] == q.ActiveGoal)
                        {
                            goal += (q.goals[i].isComplete ? "- (<color=yellow>DONE</color>) <color=grey>" : "- <color=white>") + q.goals[i].ToString() + "</color>\n";
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        goal += (q.goals[i].isComplete ? "- (<color=yellow>DONE</color>) <color=grey>" : "- <color=white>") + q.goals[i].ToString() + "</color>\n"; 
                    }
                }
            }

            if (q.turnsToFail > 0)
            {
                goal += "\n\n<color=orange>Fails in " + q.turnsToFail.ToString() + " turns</color>";
            }

            tooltip.text = desc + "\n\n" + goal;
        }
        else
        {
            tooltipGO.SetActive(false);
        }
    }

    public override void ChangeSelectedNum(int newIndex, bool scroll)
    {
        base.ChangeSelectedNum(newIndex, scroll);

        if (SelectedMax > 0)
        {
            journalBase.GetChild(SelectedNum).Highlight();
        }

        UpdateTooltip();
    }

    protected override void OnSelect(int index)
    {
        journal.trackedQuest = journal.quests[SelectedNum];

        journalBase.DespawnChildren();
        SelectedMax = 0;
        SelectedNum = 0;

        for (int i = 0; i < journal.quests.Count; i++)
        {
            GameObject g = SimplePool.Spawn(questObject, journalBase);
            g.GetComponentInChildren<Text>().text = (journal.quests[i].ID == journal.trackedQuest.ID)
                ? "<color=magenta>></color> " + journal.quests[i].Name + " <color=magenta><</color>" : journal.quests[i].Name;
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));

            SelectedMax++;
        }

        SelectedNum = index;

        if (SelectedMax > 0)
        {
            journalBase.GetChild(SelectedNum).Highlight();
        }
    }
}
