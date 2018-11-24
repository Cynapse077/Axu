using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class JournalPanel : UIPanel
{
    public Transform journalBase;
    public GameObject questObject;
    public Text tooltip;
    public Text ttTitle;
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
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(journalBase.GetChild(0).gameObject);
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

            if (journal.trackedQuest == q)
                desc = "<color=magenta>> Tracked <</color>\n\n" + desc;

            if (q.steps.Count > 0)
            {
                QuestStep step = q.NextStep;

                if (step.am > 0)
                {
                    goal = ("<b><color=orange>(" + step.amC + " / " + step.am + ")</color></b>");
                }
                else
                {
                    Coord dest = q.destination, curr = World.tileMap.WorldPosition;
                    int currElev = World.tileMap.currentElevation, toElev = q.NextStep.e;
                    string toDestination = Direction.Heading(curr, dest, currElev, toElev);

                    if (step.goal == "Go")
                        goal = ("<color=lightblue>  To Destination: ") + toDestination + "</color>";
                    else if (step.goal == "Return")
                        goal = "<color=orange>Speak to " + q.questGiver.name + ".</color>";
                }
            }

            ttTitle.text = q.Name;
            tooltip.text = desc + "\n\n" + goal;
        }
        else
        {
            tooltipGO.SetActive(false);
        }
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        if (SelectedMax > 0)
            EventSystem.current.SetSelectedGameObject(journalBase.GetChild(SelectedNum).gameObject);

        base.ChangeSelectedNum(newIndex);
        UpdateTooltip();
    }

    protected override void OnSelect(int index)
    {
        base.OnSelect(index);
        journal.trackedQuest = journal.quests[SelectedNum];
        Initialize();
    }
}
