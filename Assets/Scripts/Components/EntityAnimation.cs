using UnityEngine;
using System.Collections;
using System;

public class EntityAnimation : MonoBehaviour {

    Animation anim;

    void Start() {
        anim = GetComponent<Animation>();
    }

    public void HitAnimation() {
		PlayAnimation("Character_Hit");
    }

    public void DeathAnimation() {
		PlayAnimation("Character_Die");
    }

    public void Shake() {
		PlayAnimation("Character_Shake");
    }

	void PlayAnimation(string name) {
		anim.Stop();
		anim.Play(name);
	}
}
