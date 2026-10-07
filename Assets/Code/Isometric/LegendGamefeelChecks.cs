using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Opt-in focused checks; root verification invokes Run after scene setup. Never starts in gameplay.
public static class LegendGamefeelChecks
{
    public static IEnumerator Run(ThanhGiongCampaignController actor, Action<bool,string> check)
    {
        var audio=actor.GetComponent<ThanhGiongCampaignAudio>();
        var seeds=actor.GetComponent<LegendSeedSkills>();
        string map=actor.gameObject.scene.name;
        check(audio!=null&&audio.CombatIntensity>=0&&audio.CombatIntensity<=1,map+" adaptive music intensity remains bounded");
        check(audio!=null&&audio.SpatialEmitterCount<=5,map+" spatial ambience emitter budget");
        Rect[] areas={new Rect(0,0,1280,720),new Rect(20,20,1920,1040),new Rect(0,0,3440,1440),new Rect(0,0,800,600)};
        check(areas.All(a=>a.width/ThanhGiongCampaignHUD.CanvasScale(a)>=1280-.1f&&a.height/ThanhGiongCampaignHUD.CanvasScale(a)>=720-.1f),map+" safe-area HUD canvas fits four viewport ratios");
        if(audio==null||seeds==null)yield break;
        if(map=="ThungLungVuotSong") {
            check(audio.SpatialEmitterCount>=2,"river has water and bank spatial ambience layers");
            var loop=audio.GetComponentsInChildren<AudioSource>().FirstOrDefault(s=>s.clip!=null&&s.clip.name=="ambient_water_lap");
            var body=actor.GetComponent<CharacterController>();
            var movement=actor.GetComponent<MountedHorseController>();
            Vector3 initial=actor.transform.position;bool wasBody=body.enabled,wasMovement=movement.enabled,wasActor=actor.enabled;
            body.enabled=false;movement.enabled=false;actor.enabled=false;
            if(loop!=null) {
                Vector3 anchor=loop.transform.position;actor.transform.position=anchor;
                yield return null;float near=audio.SpatialAmbienceGain;
                actor.transform.position=anchor+Vector3.right*300;yield return null;
                check(near>.001f&&audio.SpatialAmbienceGain<near*.1f,"river ambience fades with player distance");
                check(Vector3.Distance(loop.transform.position,anchor)<.001f,"spatial river anchor remains fixed while horse moves");
            }
            actor.transform.position=initial;body.enabled=wasBody;movement.enabled=wasMovement;actor.enabled=wasActor;
            movement.ResetMovementState();Physics.SyncTransforms();yield return null;
        }
        if(!seeds.IsPlantingContext)yield break;
        var baseline=seeds.CaptureState();seeds.SelectSeed(0);int cues=audio.SeedCueCount;
        bool planted=seeds.TryPlantAt(actor.transform.position+actor.transform.forward*1.5f);
        check(planted&&audio.SeedCueCount>cues&&audio.LastCue=="seed_0","planting has distinct original Lumen audio feedback");
        if(planted) {
            yield return new WaitForSeconds(.45f);
            check(audio.SeedCueCount>=cues+2,"seed bloom has delayed secondary audio feedback");
            float phase=seeds.VisualPhase,duck=actor.GetComponent<LegendAudioMix>().DuckGain;
            Time.timeScale=0;yield return new WaitForSecondsRealtime(.1f);
            check(Mathf.Approximately(phase,seeds.VisualPhase)&&Mathf.Approximately(duck,actor.GetComponent<LegendAudioMix>().DuckGain),"pause freezes seed animation and music duck phase");
            Time.timeScale=1;
            check(seeds.ActiveMaterialCount==seeds.ActivePlantCount,"seed effects own one material per active plant");
        }
        seeds.RestoreState(baseline);
        check(seeds.ActiveMaterialCount==0&&seeds.ActivePlantCount==0,"checkpoint seed reset releases visual materials");
    }
}
