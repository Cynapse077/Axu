using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BodyPartTargetPanel : MonoBehaviour {

	public Text title;
	public Transform bpAnchor;
	public GameObject bpButton;

	List<BodyPart> bodyParts;
	public SelectionType selType;
	Body targetBody;

	public void TargetPart(Body bod, SelectionType stype) {
		title.text = LocalizationManager.GetContent(((selType == SelectionType.Amputate) ? "Title_AmpLimb" : "Title_TargetPart"));
		selType = stype;
		targetBody = bod;
		bpAnchor.DestroyChildren();

		bodyParts = (Amputate) ? targetBody.SeverableBodyParts() : targetBody.bodyParts;

		foreach (BodyPart b in bodyParts) {
			GameObject g = (GameObject)Instantiate(bpButton, bpAnchor);
			g.GetComponentInChildren<Text>().text = (b.isAttached && b.severable) ? b.displayName : "<color=grey>" + b.displayName + "</color>";
			g.GetComponent<Button>().onClick.AddListener(() => SelectPressed());
			g.GetComponent<OnHover_SetSelectedIndex>().window = (Amputate) ? UIWindow.AmputateLimb : UIWindow.TargetBodyPart;
		}
	}

	bool Amputate {
		get { return selType == SelectionType.Amputate; }
	}

	public void SelectPressed() {
		BodyPart bp = bodyParts[UserInterface.selectedItemNum];

		if (Amputate) {
			if (bp.slot == ItemProperty.Slot_Head) {
				World.userInterface.CloseWindows();
				Alert.NewAlert("Cannot_Amputate", UIWindow.AmputateLimb);
			} else {
				World.userInterface.CloseWindows();
				World.userInterface.YesNoAction("YN_Amputate", () => { targetBody.RemoveLimb(bp); World.userInterface.CloseWindows(); }, null, bp.name);
			}
		} else if (selType == SelectionType.CalledShot) {
			ObjectManager.playerEntity.fighter.Attack(targetBody.entity.stats, false, bp);
			World.userInterface.CloseWindows();
		} else if (selType == SelectionType.Grab) {
			if (!bp.isAttached)
				return;

			ObjectManager.playerEntity.skills.Grapple_GrabPart(bp, ObjectManager.playerEntity.skills.abilities.Find(x => x.ID == "grapple"));
			World.userInterface.CloseWindows();
		}
	}

	void Update() {
		if (UserInterface.selectedItemNum >= 0 && UserInterface.selectedItemNum < bodyParts.Count)
			EventSystem.current.SetSelectedGameObject(bpAnchor.GetChild(UserInterface.selectedItemNum).gameObject);
	}

	public enum SelectionType {
		Amputate, CalledShot, Grab
	}
}
