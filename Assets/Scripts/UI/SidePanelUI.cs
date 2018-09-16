using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SidePanelUI : MonoBehaviour {

	public Text HP;
	public Text ST;
	public Image hpFill;
	public Image stFill;
	public Image xpFill;

	float lerpSpeed = 8f;
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
			UserInterface.ColorByPercent(stats.health.ToString(), (int)((float)stats.health / (float)stats.maxHealth * 100f)), stats.maxHealth, HP.fontSize - 2);
		ST.text = string.Format("<b>{0}</b> / <size={2}>{1}</size>", 
			UserInterface.ColorByPercent(stats.stamina.ToString(), (int)((float)stats.stamina / (float)stats.maxStamina * 100f)), stats.maxStamina, ST.fontSize - 2);

		hpFill.fillAmount = Mathf.Lerp(hpFill.fillAmount, (float)stats.health / (float)stats.maxHealth, Time.deltaTime * lerpSpeed);
		stFill.fillAmount = Mathf.Lerp(stFill.fillAmount, (float)stats.stamina / (float)stats.maxStamina, Time.deltaTime * lerpSpeed);
		xpFill.fillAmount = Mathf.Lerp(xpFill.fillAmount, (float)stats.MyLevel.XP / (float)stats.MyLevel.XPToNext, Time.deltaTime * lerpSpeed);
	}
}
