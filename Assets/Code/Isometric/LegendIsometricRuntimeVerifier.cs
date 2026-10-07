using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Explicit integration suite, never starts in a normal game session.
public sealed class LegendIsometricRuntimeVerifier : MonoBehaviour
{
    readonly List<string> results=new();int failures;
    string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../IsometricVerification"));
    void Start(){DontDestroyOnLoad(gameObject);Directory.CreateDirectory(Folder);StartCoroutine(Run());}
    void Check(bool success,string caption){results.Add((success?"PASS ":"FAIL ")+caption);if(!success)failures++;Debug.Log("ISOMETRIC_CHECK "+results.Last());}
    IEnumerator Run()
    {
        yield return null;while(FindFirstObjectByType<GodotPortRuntimeVerifier>()!=null)yield return null;
        for(int i=0;i<GodotPortRuntimeVerifier.Maps.Length;i++){
            string map=GodotPortRuntimeVerifier.Maps[i];Time.timeScale=1;yield return SceneManager.LoadSceneAsync(map);yield return new WaitForSeconds(1f);
            var actor=FindFirstObjectByType<ThanhGiongCampaignController>();var camera=Camera.main;var follow=camera.GetComponent<IsometricCameraFollow>();var skills=actor.GetComponent<LegendSeedSkills>();var checkpoint=actor.GetComponent<LegendCheckpoint>();var atmosphere=FindFirstObjectByType<LegendAtmosphere>();
            Check(camera.orthographic&&Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.x,35.26439f))<.1f&&Mathf.Abs(Mathf.DeltaAngle(camera.transform.eulerAngles.y,45))<.1f,map+" fixed orthographic isometric framing");
            Check(skills&&checkpoint&&actor.GetComponent<LegendAudioMix>()&&atmosphere,map+" gameplay and atmosphere installed");
            var volume=FindObjectsByType<Volume>().FirstOrDefault(v=>v.enabled&&v.isGlobal&&v.sharedProfile&&v.sharedProfile.TryGet<FilmGrain>(out var grain)&&grain.intensity.value>0);
            Check(volume&&camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing,map+" painted URP grading and grain enabled");
            Check(checkpoint.HasCheckpoint,map+" initial checkpoint captured");
            Check(actor.GetComponent<CharacterController>().isGrounded,map+" grounded after projection change");
            Check(atmosphere.MoteCount<=atmosphere.MoteBudget&&atmosphere.MoteBudget==36,map+" bounded ambient motes");
            Check(atmosphere.RayCount==4,map+" four bounded painted sunshafts");
            Check(actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m&&m.shader.name=="ThanhGiong/LegendSurface")),map+" mounted actor uses painted materials");
            if(i==3)Check(atmosphere.RippleCount>0,"river has animated water ripple layer");
            var models=actor.battleRoot?actor.battleRoot.GetComponentsInChildren<ThanhGiongEnemy>(true):new ThanhGiongEnemy[0];
            Check(models.All(e=>e.GetComponent<LegendEnemyPresentation>()&&e.GetComponent<LegendEnemyPresentation>().HasNativeArt),map+" enemy placeholders replaced with authored rigs");
            var landscape=FindFirstObjectByType<LegendLandscapePresentation>();
            Check(landscape&&landscape.Built&&landscape.ClusterCount>0&&landscape.ClusterCount<=64&&landscape.PlantCount>0&&landscape.PlantCount<=800&&landscape.VertexCount<=60000&&landscape.RendererCount<=4,map+" landscape clusters built within rendering budget");
            Check(landscape&&landscape.RouteViolationCount==0&&landscape.GetComponentsInChildren<Collider>().Length==0,map+" landscape preserves routes and collision");
            if(i==3)Check(landscape&&landscape.ReedCount>0,"river banks have animated reed clusters");
            if(i==2||i==3){
                var living=models.Where(e=>e.gameObject.activeInHierarchy&&e.HealthRatio>0).Select(e=>e.GetComponent<LegendEnemyPresentation>()).ToArray();
                foreach(var presentation in living) if(presentation) presentation.RefreshGroundSupportNow();
                yield return new WaitForFixedUpdate();
                foreach(var presentation in living) if(presentation) presentation.RefreshGroundSupportNow();
                Check(living.Length>0&&living.All(p=>p.HasGroundSupport&&Mathf.Abs(p.GroundGap-.03f)<.2f),map+" living enemy art rests on supported terrain");
            }
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Folder,map+".png"));
            if(i==2)yield return BattleChecks(actor,skills,checkpoint);
            if(i==1){Check(!skills.TryPlantAt(actor.transform.position+actor.transform.forward),"Q skills blocked during equipment ritual");}
            if(i==5){Check(!skills.TryPlantAt(actor.transform.position+actor.transform.forward),"Q skills blocked during ascension ritual");}
            yield return LegendGamefeelChecks.Run(actor,Check);
        }
        string status="ISOMETRIC_VERIFY "+(failures==0?"PASS":"FAIL")+" failures="+failures;results.Add(status);File.WriteAllLines(Path.Combine(Folder,"runtime_results.txt"),results);Debug.Log(status);Time.timeScale=1;LegendVerificationPreferences.Restore();Destroy(gameObject);
    }
    IEnumerator BattleChecks(ThanhGiongCampaignController actor,LegendSeedSkills skills,LegendCheckpoint checkpoint)
    {
        var movement=actor.GetComponent<MountedHorseController>();var body=actor.GetComponent<CharacterController>();var audio=actor.GetComponent<ThanhGiongCampaignAudio>();
        var enemies=actor.battleRoot.GetComponentsInChildren<ThanhGiongEnemy>(true);foreach(var enemy in enemies){enemy.StopAllCoroutines();enemy.enabled=false;if(enemy.Body){if(!enemy.Body.isKinematic){enemy.Body.linearVelocity=Vector3.zero;enemy.Body.angularVelocity=Vector3.zero;}enemy.Body.isKinematic=true;}}
        Vector3 start=actor.transform.position;Quaternion rotation=actor.transform.rotation;Vector3 direction=Vector3.right;
        GameObject wall=new GameObject("Verification Dodge Wall");wall.transform.position=start+direction*2+Vector3.up*.8f;wall.transform.localScale=new Vector3(.2f,4,8);wall.AddComponent<BoxCollider>();Physics.SyncTransforms();
        Check(movement.TryDodge(direction)&&movement.IsDodging&&movement.IsDodgeInvulnerable,"mounted dodge starts with invulnerability");
        float hp=actor.Health;Check(!actor.DamagePlayer(8)&&Mathf.Approximately(hp,actor.Health),"dodge rejects incoming damage");
        Check(!movement.TryDodge(direction),"dodge cannot be stacked");
        var atmosphere=FindFirstObjectByType<LegendAtmosphere>();float ambientTime=atmosphere.MotionTime;
        var landscape=FindFirstObjectByType<LegendLandscapePresentation>();float landscapeTime=landscape.MotionTime;
        var rig=actor.GetComponent<GodotMountedMotion>();float motionTime=rig.MotionTime;
        foreach(float duration in new[]{.32f,.58f}){
            GodotMountedMotion.EvaluateAttack(.12f/duration,duration,out float windup,out float strike);
            Check(strike>.99f&&windup<.01f,"mounted attack animation meets damage contact at .12 seconds duration "+duration);
            GodotMountedMotion.EvaluateAttack(1,duration,out windup,out strike);
            Check(strike<.01f&&windup<.01f,"mounted attack animation returns to rest duration "+duration);
        }
        Time.timeScale=0;Vector3 frozen=actor.transform.position;float timer=movement.DodgeRemaining;yield return new WaitForSecondsRealtime(.15f);
        Check(Vector3.Distance(frozen,actor.transform.position)<.001f&&Mathf.Approximately(timer,movement.DodgeRemaining),"pause freezes dodge distance and timer");
        Check(Mathf.Approximately(ambientTime,atmosphere.MotionTime),"pause freezes atmosphere and sunshaft phase");
        Check(Mathf.Approximately(landscapeTime,landscape.MotionTime)&&Mathf.Approximately(motionTime,rig.MotionTime),"pause freezes landscape sway and mounted rig");
        Check(!skills.TryPlantAt(start+direction),"pause rejects seed planting");Time.timeScale=1;
        yield return new WaitForSeconds(.5f);Check(!movement.IsDodging&&Vector3.Dot(actor.transform.position-start,direction)<1.7f,"dodge stops at thin wall");Destroy(wall);yield return null;
        body.enabled=false;actor.transform.SetPositionAndRotation(start,rotation);body.enabled=true;movement.ResetMovementState();Physics.SyncTransforms();yield return new WaitForSeconds(.6f);
        var baseline=skills.CaptureState();
        foreach(var enemy in enemies)if(enemy.gameObject.activeInHierarchy){enemy.StopAllCoroutines();enemy.enabled=false;}
        for(int seed=0;seed<5;seed++){
            skills.RestoreState(baseline);skills.SelectSeed(seed);int before=skills.PlantedCount,charges=skills.ChargesRemaining;
            Vector3 requested=start+Vector3.forward*1.5f;
            bool planted=skills.TryPlantAt(requested);Check(planted&&skills.PlantedCount==before+1&&skills.ChargesRemaining==charges-1&&skills.Mana<baseline.mana,"seed "+(seed+1)+" consumes mana and charge on supported ground");
            Check(!skills.TryPlantAt(requested)&&skills.CooldownRemaining>0,"seed "+(seed+1)+" cooldown prevents repeat cast");
            if(seed==0&&planted){var enemy=enemies.FirstOrDefault(e=>e.gameObject.activeInHierarchy&&e.HealthRatio>0);if(enemy){Vector3 old=enemy.transform.position;skills.TryGround(requested,out var ground);enemy.transform.position=ground+Vector3.up*.1f;Physics.SyncTransforms();yield return new WaitForSeconds(.1f);Check(enemy.IsRooted&&Vector3.Distance(enemy.transform.position,ground)<.3f,"Lumen roots an enemy without knockback");enemy.transform.position=old;}}
            if(seed==2&&planted){actor.DamagePlayer(8);float wounded=actor.Health;yield return new WaitForSeconds(.65f);Check(actor.Health>wounded,"lotus seed restores health in its radius");}
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"seed_"+(seed+1)+".png"));
        }
        skills.RestoreState(baseline);skills.SelectSeed(0);Check(!skills.TryPlantAt(start+Vector3.forward*30),"out-of-range planting rejected");
        checkpoint.ActivateCheckpoint("Kiểm tra khôi phục");float savedHp=actor.Health,savedMana=skills.Mana;int savedKills=actor.Kills;Vector3 savedPosition=actor.transform.position;
        var target=enemies.FirstOrDefault(e=>e.gameObject.activeInHierarchy&&e.HealthRatio>0);float ratio=target?target.HealthRatio:0;
        if(target){if(target.Body)target.Body.isKinematic=false;target.TakeDamage(target.maxHealth*3,0,start);}yield return new WaitForSeconds(.65f);actor.DamagePlayer(12);skills.TryPlantAt(start+Vector3.forward);
        body.enabled=false;actor.transform.position=start+Vector3.right*5;body.enabled=true;
        Check(checkpoint.RestartCheckpoint(),"checkpoint replay accepted");yield return new WaitForFixedUpdate();yield return null;
        Check(Vector3.Distance(actor.transform.position,savedPosition)<.2f&&Mathf.Abs(actor.Health-savedHp)<.01f&&actor.Kills==savedKills&&Mathf.Abs(skills.Mana-savedMana)<.2f,"checkpoint restores position health kills and seed resources");
        Check(!target||(target.gameObject.activeInHierarchy&&Mathf.Abs(target.HealthRatio-ratio)<.01f),"checkpoint restores defeated enemy state");
        yield return CombatChecks(actor,enemies,checkpoint);
        float master=LegendAudioMix.Master,music=LegendAudioMix.Music,effects=LegendAudioMix.Effects;
        LegendAudioMix.SetMix(0,music,effects);yield return new WaitForEndOfFrame();Check(audio.GetComponents<AudioSource>().All(s=>s.volume<.001f),"master volume slider mutes real audio sources");LegendAudioMix.SetMix(master,music,effects);
        skills.ClearPlants();
    }
    IEnumerator CombatChecks(ThanhGiongCampaignController actor,ThanhGiongEnemy[] enemies,LegendCheckpoint checkpoint)
    {
        var target=enemies.FirstOrDefault(e=>!e.isBoss&&e.HealthRatio>0&&e.gameObject.activeInHierarchy);if(!target)yield break;
        foreach(var enemy in enemies){enemy.StopAllCoroutines();enemy.enabled=false;}
        float originalMax=target.maxHealth;target.maxHealth=1000;
        target.RestoreCheckpoint(actor,actor.transform,actor.transform.position+actor.transform.forward*2,actor.transform.rotation,400,true);target.StopAllCoroutines();target.enabled=false;
        float hp=target.HealthRatio;Check(actor.TryMeleeAttack()&&actor.AttackPending&&Mathf.Approximately(target.HealthRatio,hp),"melee attack has windup before damage");
        Time.timeScale=0;yield return new WaitForSecondsRealtime(.18f);Check(actor.AttackPending&&Mathf.Approximately(hp,target.HealthRatio),"pause freezes melee windup");Time.timeScale=1;
        yield return new WaitForSeconds(.25f);Check(!actor.AttackPending&&target.HealthRatio<hp,"melee resolves damage after windup");
        for(int beat=2;beat<=3;beat++){
            yield return new WaitForSeconds(.18f);target.StopAllCoroutines();target.transform.position=actor.transform.position+actor.transform.forward*2;if(target.Body){target.Body.isKinematic=false;target.Body.linearVelocity=Vector3.zero;target.Body.angularVelocity=Vector3.zero;target.Body.position=target.transform.position;}Physics.SyncTransforms();
            Check(actor.TryMeleeAttack()&&actor.ComboBeat==beat,"melee combo advances to beat "+beat);yield return new WaitForSeconds(.25f);
        }
        target.maxHealth=originalMax;checkpoint.RestartCheckpoint();
        var audio=actor.GetComponent<ThanhGiongCampaignAudio>();var mix=actor.GetComponent<LegendAudioMix>();float master=LegendAudioMix.Master,music=LegendAudioMix.Music,effects=LegendAudioMix.Effects;
        foreach(var voice in audio.GetComponents<AudioSource>())if(!voice.loop)voice.Stop();LegendAudioMix.SetMix(1,1,1);yield return new WaitForSeconds(.5f);float before=mix.DuckGain;
        audio.PlayCue("hit_metal",1);yield return new WaitForSeconds(.08f);Check(mix.DuckGain<before,"event sound smoothly ducks regional music");LegendAudioMix.SetMix(master,music,effects);
    }
}
