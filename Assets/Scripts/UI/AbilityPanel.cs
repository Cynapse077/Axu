using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AbilityPanel : UIPanel
{
    [Header("Prefabs")]
    public GameObject abilityButton;
    [Header("Children")]
    public Transform abilityBase;
    public Scrollbar scrollBar;
    public AbilityTooltip tooltip;

    EntitySkills skills;

    public override void Initialize()
    {
        skills = ObjectManager.playerEntity.skills;
        UpdateAbilities();
        base.Initialize();
    }

    void UpdateAbilities()
    {
        abilityBase.DespawnChildren();
        SelectedMax = 0;
        SelectedNum = 0;

        for (int i = 0; i < skills.abilities.Count; i++)
        {
            GameObject g = SimplePool.Spawn(abilityButton, abilityBase);
            g.GetComponent<AbilityButton>().Setup(skills.abilities[i], i);
            g.GetComponent<Button>().onClick.AddListener(() => OnSelect(g.transform.GetSiblingIndex()));
            SelectedMax++;
        }

        if (SelectedMax > 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(abilityBase.GetChild(0).gameObject);
        }
            
        UpdateTooltip();
    }

    public void UpdateTooltip()
    {
        if (SelectedMax > 0)
        {
            tooltip.gameObject.SetActive(true);
            tooltip.UpdateTooltip(skills.abilities[SelectedNum]);
        }
        else
        {
            tooltip.gameObject.SetActive(false);
        }
    }

    public override void Update()
    {
        if (initialized)
        {
            if (SelectedMax > 0)
            {
                if (GameSettings.Keybindings.GetKey("GoUpStairs") && SelectedNum < SelectedMax - 1)
                {
                    int newIndex = SelectedNum + 1;
                    skills.abilities.Move(SelectedNum, newIndex);
                    UpdateAbilities();
                    SelectedNum = newIndex;
                }
                else if (GameSettings.Keybindings.GetKey("GoDownStairs") && SelectedNum > 0)
                {
                    int newIndex = SelectedNum - 1;
                    skills.abilities.Move(SelectedNum, newIndex);
                    UpdateAbilities();
                    SelectedNum = newIndex;
                }
            }

            base.Update();
        }

        if (GameSettings.Keybindings.GetKey("Abilities"))
        {
            World.userInterface.CloseWindows();
        }
    }

    public override void ChangeSelectedNum(int newIndex)
    {
        base.ChangeSelectedNum(newIndex);

        if (SelectedMax > 0)
        {
            EventSystem.current.SetSelectedGameObject(abilityBase.GetChild(SelectedNum).gameObject);
            scrollBar.value = 1f - (SelectedNum / (float)SelectedMax);
        }

        UpdateTooltip();
    }

    protected override void OnSelect(int index)
    {
        if (SelectedMax > 0)
        {
            skills.abilities[index].Cast(skills.entity);
        }
    }
}
