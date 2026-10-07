using System.Collections.Generic;
using UnityEngine;

// Late execution lets the regional audio writer retain base gains, then applies player mix once.
[DefaultExecutionOrder(500)]
public sealed class LegendAudioMix : MonoBehaviour
{
    public static float Master {get;private set;}=1;
    public static float Music {get;private set;}=1;
    public static float Effects {get;private set;}=1;
    public float DuckGain {get;private set;}=1;
    readonly Dictionary<AudioSource, Gain> gains=new Dictionary<AudioSource, Gain>();
    sealed class Gain {public float basis,lastApplied;public AudioClip clip;}
    bool loaded;float refreshAt;AudioSource[] sources=new AudioSource[0];
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStatics(){Master=Music=Effects=1;}
    void Awake()
    {
        if(!loaded){Master=PlayerPrefs.GetFloat("TG.Audio.Master",1);Music=PlayerPrefs.GetFloat("TG.Audio.Music",1);Effects=PlayerPrefs.GetFloat("TG.Audio.Effects",1);loaded=true;}
    }
    public static void SetMix(float master,float music,float effects)
    {
        Master=Mathf.Clamp01(master);Music=Mathf.Clamp01(music);Effects=Mathf.Clamp01(effects);
        PlayerPrefs.SetFloat("TG.Audio.Master",Master);PlayerPrefs.SetFloat("TG.Audio.Music",Music);PlayerPrefs.SetFloat("TG.Audio.Effects",Effects);
    }
    public static bool IsMusicSource(AudioSource source)=>source!=null&&source.clip!=null&&source.clip.name.StartsWith("music_");
    void Update()
    {
        if(Time.unscaledTime>=refreshAt){sources=GetComponentsInChildren<AudioSource>(true);refreshAt=Time.unscaledTime+.5f;}
        bool eventPlaying=false;
        foreach(AudioSource source in sources)if(source!=null&&!source.loop&&source.isPlaying&&source.clip!=null&&!source.clip.name.StartsWith("hoof_"))eventPlaying=true;
        if(Time.timeScale>0)DuckGain=Mathf.Lerp(DuckGain,eventPlaying?.55f:1,1-Mathf.Exp(-Time.unscaledDeltaTime*(eventPlaying?10:3)));
        ApplyMix();
    }
    void LateUpdate(){ApplyMix();}
    void ApplyMix()
    {
        foreach(AudioSource source in sources) {
            if(source==null)continue;
            if(!gains.TryGetValue(source,out Gain gain)){gain=new Gain {basis=source.volume,lastApplied=source.volume,clip=source.clip};gains.Add(source,gain);}
            if(gain.clip!=source.clip||!Mathf.Approximately(source.volume,gain.lastApplied)){gain.basis=source.volume;gain.clip=source.clip;}
            float category=IsMusicSource(source)?Music*DuckGain:Effects;
            gain.lastApplied=Mathf.Clamp01(gain.basis*Master*category);source.volume=gain.lastApplied;
        }
    }
    void OnDisable(){foreach(var pair in gains)if(pair.Key!=null)pair.Key.volume=pair.Value.basis;PlayerPrefs.Save();}
}
