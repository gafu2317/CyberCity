using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource audioSource_BGM;
    [SerializeField] private AudioSource audioSource_SE;

    [SerializeField] private Slider slider_BGM;
    [SerializeField] private Slider slider_SE;

    private float volume_BGM = 1;
    public float volume_SE {private set; get;} = 1;


    [SerializeField] private AudioClip BGM_Menu;
    public enum BGM
    {
        Menu
    }
    public BGM playngBGM {private set; get;} = BGM.Menu;
    private float[] clipVolume_BGM = new float[]{
        
    };

    [SerializeField] private AudioClip SE_;
    public enum SE
    {
        
    }
    private float[] clipVolume_SE = new float[]{
        
    };

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetBGMPlayVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        audioSource_BGM.volume = volume * volume_BGM * clipVolume_BGM[(int)playngBGM];
    }

    public void PlayBGM(BGM clip_BGM)
    {
        AudioClip playClip = null;
        switch (clip_BGM)
        {
            case BGM.Menu:
                playClip = BGM_Menu;
                break;

        }
        audioSource_BGM.clip = playClip;
        audioSource_BGM.volume = volume_BGM * clipVolume_BGM[(int)clip_BGM];
        audioSource_BGM.Play();
        playngBGM = clip_BGM;
    }

    public void StopBGM()
    {
        audioSource_BGM.Stop();
    }

    public void SetVolumeBGM()
    {
        volume_BGM = slider_BGM.value;
        audioSource_BGM.volume = volume_BGM;
    }

    public void SetVolumeBGM(float volume)
    {
        volume_BGM = volume;
        slider_BGM.value = volume;
        audioSource_BGM.volume = volume;
    }

    public void PlaySE(SE clip_SE)
    {
        AudioClip playClip = null;
        switch (clip_SE)
        {
            

        }
        audioSource_SE.PlayOneShot(playClip, volume_SE * clipVolume_SE[(int)clip_SE]);
    }

    public void StopSE()
    {
        audioSource_SE.Stop();
    }

    public void SetVolumeSE(float volume)
    {
        volume_SE = volume;
        slider_SE.value = volume;
    }
}
