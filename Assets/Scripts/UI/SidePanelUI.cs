using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SidePanelUI : MonoBehaviour {

	public Text HP;
	public Text ST;
	public Image hpFill;
	public Image stFill;
	public Image xpFill;
    public GameObject rad;

	readonly float lerpSpeed = 8f;
	Stats stats;
	bool initialized = false;

	public void Init() {
		stats = ObjectManager.player.GetComponent<Stats>();
		initialized = true;
	}

	void Update() {
		if (initialized)
			DisplayHPST();
	}

	void DisplayHPST() {
		HP.text = string.Format("<b>{0}</b> / <size={2}>{1}</size>", 
			UserInterface.ColorByPercent(stats.health.ToString(), (int)(stats.health / (float)stats.maxHealth * 100f)), stats.maxHealth, HP.fontSize - 2);
		ST.text = string.Format("<b>{0}</b> / <size={2}>{1}</size>", 
			UserInterface.ColorByPercent(stats.stamina.ToString(), (int)(stats.stamina / (float)stats.maxStamina * 100f)), stats.maxStamina, ST.fontSize - 2);

		hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, stats.health / (float)stats.maxHealth, Time.deltaTime * lerpSpeed);
		stFill.fillAmount = Mathf.Lerp(stFill.fillAmount, stats.stamina / (float)stats.maxStamina, Time.deltaTime * lerpSpeed);
		xpFill.fillAmount = Mathf.Lerp(xpFill.fillAmount, stats.MyLevel.XP / (float)stats.MyLevel.XPToNext, Time.deltaTime * lerpSpeed);

        int radLevel = World.tileMap.CurrentMap.mapInfo.radiation;
        bool radActive = radLevel > 0 && World.tileMap.currentElevation == 0;
        rad.SetActive(radActive);

        if (radActive)
        {
            string s = "!";

            if (radLevel < 4)
                s = "<color=green>!</color>";
            else if (radLevel < 7)
                s = "<color=yellow>!!</color>";
            else
                s = "<color=red>!!!</color>";

            rad.GetComponentInChildren<Text>().text = s;
        }
	}
}
