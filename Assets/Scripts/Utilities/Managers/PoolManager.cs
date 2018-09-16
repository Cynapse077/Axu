using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour {

	public GameObject[] slashEffects;
	[Space(10)]
	public GameObject[] abilityEffects;
	[Space(10)]
	public GameObject shootEffect;
	public GameObject throwEffect;
	public GameObject teleBeam;
	public GameObject splash;
	public GameObject damageEffect;
	public GameObject blockEffect;
	public GameObject lightningBolt;
	public GameObject roarEffect;
	public GameObject exclamationMark;

	void Start() {
		World.poolManager = this;
		PreloadSlashEffects();
	}

	void PreloadSlashEffects() {
		for (int i = 0; i < slashEffects.Length; i++) {
			SimplePool.Preload(slashEffects[i], 3);
		}
	}
}
