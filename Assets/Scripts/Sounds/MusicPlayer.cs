using UnityEngine;
using System.Collections;

public class MusicPlayer : MonoBehaviour
{
    [System.Serializable]
    public class Stem
    {
        public AudioSource source;
        public AudioClip clip;
        public float startingSpeedRatio;    // The stem will start when this is lower than currentSpeed/maxSpeed.
    }

	static protected MusicPlayer s_Instance;
	static public MusicPlayer instance { get { return s_Instance; } }

	public UnityEngine.Audio.AudioMixer mixer;
    public Stem[] stems;
    public float maxVolume = 0.1f;

    void Awake()
    {
        if (s_Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;

        // As this is one of the first script executed, set that here.
        Application.targetFrameRate = 30;
        AudioListener.pause = false;
        
        DontDestroyOnLoad(gameObject);
    }

	void Start()
	{
		PlayerData.Create ();

		if (PlayerData.instance.masterVolume > float.MinValue) 
		{
			mixer.SetFloat ("MasterVolume", PlayerData.instance.masterVolume);
			mixer.SetFloat ("MusicVolume", PlayerData.instance.musicVolume);
			mixer.SetFloat ("MasterSFXVolume", PlayerData.instance.masterSFXVolume);
		}
        else 
		{
			mixer.GetFloat ("MasterVolume", out PlayerData.instance.masterVolume);
			mixer.GetFloat ("MusicVolume", out PlayerData.instance.musicVolume);
			mixer.GetFloat ("MasterSFXVolume", out PlayerData.instance.masterSFXVolume);

			PlayerData.instance.Save ();
		}

		StartCoroutine(RestartAllStems());
	}

    public void SetStem(int index, AudioClip clip)
    {
        if (stems.Length <= index)
        {
            Debug.LogError("Trying to set an undefined stem");
            return;
        }

        stems[index].clip = clip;
    }

    public AudioClip GetStem(int index)
    {
        return stems.Length <= index ? null : stems[index].clip;
    }

    public IEnumerator RestartAllStems()
    {
        for (int i = 0; i < stems.Length; ++i)
        {
        	stems[i].source.clip = stems[i].clip;
			stems [i].source.volume = 0.0f;
            stems[i].source.Play();
        }

		// This is to fix a bug in the Audio Mixer where attenuation will be applied only a few ms after the source start playing.
		// So we play all source at volume 0.0f first, then wait 50 ms before finally setting the actual volume.
		yield return new WaitForSeconds(0.05f);

		for (int i = 0; i < stems.Length; ++i) 
		{
			stems [i].source.volume = stems[i].startingSpeedRatio <= 0.0f ? maxVolume : 0.0f;
		}
    }

    public void UpdateVolumes(float currentSpeedRatio)
    {
        const float fadeSpeed = 0.5f;

        for(int i = 0; i < stems.Length; ++i)
        {
            float target = currentSpeedRatio >= stems[i].startingSpeedRatio ? maxVolume : 0.0f;
            stems[i].source.volume = Mathf.MoveTowards(stems[i].source.volume, target, fadeSpeed * Time.deltaTime);
        }
    }
}
