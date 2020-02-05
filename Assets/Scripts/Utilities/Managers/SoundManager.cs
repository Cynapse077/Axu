using UnityEngine;
using System.Collections;
using MoonSharp.Interpreter;

[MoonSharpUserData]
public class SoundManager : MonoBehaviour
{
    const float sfxConst = 0.2f;
    const float musConst = 0.4f;

    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioClip[] soundFX;
    public AudioClip[] music;
    public bool playOnAwake = true;
    public bool startFading = true;

    int currentTrack = 0;
    float waitTime = 5.0f;
    bool fadeOut, fadeIn;
    bool initialized;
    AudioClip[] zoneTracks;

    void Start()
    {
        World.soundManager = this;

        if (musicSource != null)
        {
            if (startFading)
            {
                fadeIn = true;
                musicSource.volume = 0.0f;
            }

            if (playOnAwake)
            {
                initialized = true;
                PlayMusic();
            }
        }

        zoneTracks = music;
    }

    public void OpenDoor() { PlaySoundNoRandom(0, 0.6f); }
    public void CloseDoor() { PlaySoundNoRandom(1, 0.6f); }
    public void AttackSound() { PlaySound(2, 0.55f); }
    public void BlockSound() { PlaySound(3, 0.7f); }
    public void MenuTick() { PlaySound(4, 0.2f); }
    public void ShootFirearm() { PlaySoundNoRandom(6, 1.0f); }
    public void ShootBow() { PlaySoundNoRandom(19, 1.0f); }
    public void UseItem() { PlaySoundNoRandom(7, 0.4f); }
    public void Splash() { PlaySound(8, Random.Range(0.03f, 0.09f)); }
    public void BreakArea() { PlaySoundNoRandom(9, 0.7f); }
    public void Reload() { PlaySoundNoRandom(10, 0.8f); }
    public void AttackSound2() { PlaySound(11, 0.55f); }
    public void TeleportSound() { PlaySoundNoRandom(12, 1.7f); }
    public void Explosion() { PlaySoundNoRandom(13, 1.2f); }
    public void Growl() { PlaySound(14, 1.2f); }
    public void Miss() { PlaySound(15, 0.25f); }
    public void Eat() { PlaySoundNoRandom(17, 0.5f); }
    public void Drink() { PlaySoundNoRandom(18, 1.2f); }
    public void Block() { PlaySoundNoRandom(20, 0.25f); }

    public void PlayAttackSound(Item i)
    {
        if (i.attackType == Item.AttackType.Bash || i.attackType == Item.AttackType.Sweep)
            AttackSound2();
        else
            AttackSound();
    }

    void PlaySound(int soundNum)
    {
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume);
    }

    void PlaySound(int soundNum, float vol)
    {
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume * vol);
    }

    void PlaySoundNoRandom(int soundNum, float volume)
    {
        sfxSource.PlayOneShot(soundFX[soundNum], EffectVolume * volume);
    }

    void Update()
    {
        if (musicSource != null)
        {
            Music();
        }

        if (sfxSource != null)
        {
            sfxSource.mute = GameSettings.MuteAll;
        }
    }

    void Music()
    {
        if (fadeIn)
        {
            fadeOut = false;

            musicSource.volume += (Time.deltaTime * 0.5f);

            if (musicSource.volume >= MusicVolume)
            {
                fadeIn = false;
            }
        }
        else if (fadeOut)
        {
            fadeIn = false;

            musicSource.volume -= (Time.deltaTime * 0.5f);

            if (musicSource.volume <= 0.0f)
            {
                fadeOut = false;
            }
        }
        else
        {
            musicSource.volume = MusicVolume;
        }
        
        musicSource.mute = GameSettings.MuteAll;

        if (initialized && !musicSource.isPlaying && (music.Length > 0 || zoneTracks.Length > 0))
        {
            waitTime -= Time.time;

            if (waitTime <= 0)
            {
                PlayMusic();
                waitTime = Random.Range(0.7f, 2f);
            }
        }
    }

    public void InitializeAndPlay()
    {
        initialized = true;
        PlayMusic();
    }

    void PlayMusic()
    {
        int ran = (music.Length > 0) ? Random.Range(0, music.Length) : Random.Range(0, zoneTracks.Length);

        if (ran == currentTrack)
        {
            ran = GetRandomTrack();
        }

        musicSource.clip = (music.Length > 0) ? music[ran] : zoneTracks[ran];
        currentTrack = ran;
        musicSource.Play();
    }

    public void OverrideMusic(AudioClip[] audioClips)
    {
        //If we have a matching track, don't fade in and out
        for (int i = 0; i < audioClips.Length; i++)
        {
            if (musicSource.clip == audioClips[i])
            {
                initialized = true;
                return;
            }
        }

        zoneTracks = audioClips;
        StartCoroutine(FadeMusicIn());
        initialized = true;
    }

    IEnumerator FadeMusicIn()
    {
        if (musicSource.volume >= MusicVolume)
        {
            fadeOut = true;

            while (fadeOut)
            {
                yield return null;
            }
        }

        fadeIn = true;

        int trackNum = Random.Range(0, zoneTracks.Length);

        musicSource.clip = zoneTracks[trackNum];
        currentTrack = trackNum;
        musicSource.Play();
    }

    int GetRandomTrack()
    {
        int max = (music.Length > 0) ? music.Length : zoneTracks.Length;
        return Random.Range(0, max);
    }

    float MusicVolume
    {
        get { return ((float)GameSettings.Master_Volume * (float)GameSettings.Mus_Volume) * musConst; }
    }
    float EffectVolume
    {
        get { return ((float)GameSettings.Master_Volume * (float)GameSettings.SE_Volume) * sfxConst; }
    }
}
