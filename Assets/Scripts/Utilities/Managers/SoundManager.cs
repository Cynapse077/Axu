using UnityEngine;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class SoundManager : MonoBehaviour {

	public AudioSource sfxSource;
	public AudioSource musicSource;

	public AudioClip[] soundFX;
	public AudioClip[] music;
	const float sfxConst = 0.3f;
	const float musConst = 0.5f;
	int currentTrack = 0;

	public void Start() {
		World.soundManager = this;
		PlayMusic();
	}

	public void OpenDoor() { PlaySoundNoRandom(0, 0.6f); }
	public void CloseDoor() { PlaySoundNoRandom(1, 0.6f); }
	public void AttackSound() { PlaySound(2, 0.6f); }
	public void BlockSound() { PlaySound(3, 0.7f); }
	public void MenuTick() { PlaySound(4, 0.3f); }
	public void ShootFirearm() { PlaySoundNoRandom(6, 1.0f); }
	public void ShootBow() { PlaySoundNoRandom(19, 1.0f); }
	public void UseItem() { PlaySoundNoRandom(7, 0.4f); }
	public void Splash() { PlaySound(8, Random.Range(0.03f, 0.09f)); }
	public void BreakArea() { PlaySoundNoRandom(9, 0.7f); }
	public void Reload() { PlaySoundNoRandom(10, 0.8f); }
	public void AttackSound2() { PlaySound(11, 0.6f); }
	public void TeleportSound() { PlaySoundNoRandom(12, 1.7f); }
	public void Explosion() { PlaySoundNoRandom(13, 1.2f); }
	public void Growl() { PlaySound(14, 1.2f); }
	public void Miss() { PlaySound(15, 0.6f); }
	public void Eat() { PlaySoundNoRandom(17, 0.5f); }
	public void Drink() { PlaySoundNoRandom(18, 1.2f); }

    public void PlayAttackSound(Item i)
    {
        if (i.attackType == Item.AttackType.Bash || i.attackType == Item.AttackType.Sweep)
            AttackSound2();
        else
            AttackSound();
    }

	void PlaySound(int soundNum) {
		sfxSource.pitch = Random.Range(0.9f, 1.1f);
		sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume * sfxConst);
	}

	void PlaySound(int soundNum, float vol) {
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
		sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume * vol * sfxConst);
	}

	void PlaySoundNoRandom(int soundNum, float volume) {
		sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume * volume * sfxConst);
	}

	void Update() {
		Music();

		if (sfxSource != null)
			sfxSource.mute = GameSettings.MuteAll;
	}

	float waitTime = 5.0f;

	void Music() {
		if (musicSource != null) {
			musicSource.volume = MusicVolume * musConst;
			musicSource.mute = GameSettings.MuteAll;

			if (!musicSource.isPlaying && music != null && music.Length > 0) {
				waitTime -= Time.time;

				if (waitTime <= 0) {
					PlayMusic();
					waitTime = Random.Range(0.5f, 5f);
				}
			}
		}
	}

	void PlayMusic() {
		if (music != null && music.Length > 0) {
			int ran = Random.Range(0, music.Length - 1);
			if (ran == currentTrack)
				ran = GetRandomTrack();
			
			musicSource.clip = music[ran];
			currentTrack = ran;
			musicSource.Play();
		}
	}

	int GetRandomTrack() {
		return Random.Range(0, music.Length - 1);
	}

	float MusicVolume {
		get { return ((float)GameSettings.Master_Volume * (float)GameSettings.Mus_Volume); }
	}
	float EffectVolume {
		get { return ((float)GameSettings.Master_Volume * (float)GameSettings.SE_Volume); }
	}
}
