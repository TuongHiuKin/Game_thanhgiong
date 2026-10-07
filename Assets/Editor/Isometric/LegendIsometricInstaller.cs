using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class LegendIsometricInstaller
{
    const string ProfilePath="Assets/Settings/LegendPaintedProfile.asset";
    const string BackupPath="Backups/Isometric_20261007";
    public static readonly string[] Maps=GodotPortRuntimeVerifier.Maps;
    static string ScenePath(string map)=>map=="AlbionForestMap"?"Assets/Scenes/AlbionForestMap.unity":"Assets/Scenes/ThanhGiongWorld/"+map+".unity";

    [MenuItem("Tools/Thanh Giong/Isometric/Apply To All Maps")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before applying art direction.");
        EditorSceneManager.SaveOpenScenes();Directory.CreateDirectory(BackupPath);
        foreach(string map in Maps){string scenePath=ScenePath(map);string backup=Path.Combine(BackupPath,map+".unity");if(!File.Exists(backup))File.Copy(scenePath,backup);}
        PreserveShaders();var profile=Profile();
        for(int i=0;i<Maps.Length;i++) {
            var scene=EditorSceneManager.OpenScene(ScenePath(Maps[i]));
            var actor=UnityEngine.Object.FindFirstObjectByType<ThanhGiongCampaignController>();
            if(actor==null)throw new InvalidOperationException("Missing actor in "+Maps[i]);
            Add<LegendSeedSkills>(actor.gameObject);Add<LegendCheckpoint>(actor.gameObject);Add<LegendAudioMix>(actor.gameObject);Add<LegendActorArt>(actor.gameObject);
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<ThanhGiongEnemy>(FindObjectsInactive.Include,FindObjectsSortMode.None))LegendEnemyArt.Apply(enemy);
            var camera=UnityEngine.Object.FindFirstObjectByType<IsometricCameraFollow>();
            if(camera==null)throw new InvalidOperationException("Missing camera in "+Maps[i]);
            camera.useOrthographicIsometric=true;camera.isometricPitch=35.26439f;camera.isometricYaw=45;camera.exploreSize=13;camera.combatSize=11;camera.vistaSize=27;camera.bossSize=17;camera.closeSize=8;
            camera.target=actor.transform;camera.GetComponent<Camera>().orthographic=true;camera.SnapToTarget();
            Add<UniversalAdditionalCameraData>(camera.gameObject).renderPostProcessing=true;
            foreach(var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            var root=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Legend Isometric Presentation");if(root==null)root=new GameObject("Legend Isometric Presentation");
            var global=Add<Volume>(root);global.enabled=true;global.isGlobal=true;global.priority=20;global.sharedProfile=profile;
            Add<LegendWorldPresentation>(root).player=actor.transform;
            Add<LegendAtmosphere>(root);
            if(i==6)foreach(var world in scene.GetRootGameObjects()){
                if(world==root||world.GetComponentInChildren<MountedHorseController>(true)||world.GetComponentInChildren<ThanhGiongEnemy>(true)||world.GetComponentInChildren<ThanhGiongCollectible>(true)||world.GetComponentsInChildren<Renderer>(true).Length==0)continue;
                Add<GodotEnvironmentMotion>(world);
            }
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>())if(light.type==LightType.Directional){light.color=new Color(1,.88f,.67f);light.intensity=.85f;light.transform.rotation=Quaternion.Euler(42,135,0);light.shadows=LightShadows.Soft;light.shadowStrength=.65f;}
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.57f,.56f);RenderSettings.ambientEquatorColor=new Color(.37f,.43f,.34f);RenderSettings.ambientGroundColor=new Color(.24f,.28f,.22f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.54f,.63f,.59f);RenderSettings.fogStartDistance=55;RenderSettings.fogEndDistance=115;
            Physics.SyncTransforms();
            if(root.transform.Find("Checkpoint Route")==null){var beacons=new GameObject("Checkpoint Route");beacons.transform.SetParent(root.transform,false);InstallBeacons(beacons.transform,actor,i);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("ISOMETRIC_INSTALL "+Maps[i]+" orthographic / painted / skills / checkpoints");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(ScenePath(Maps[0]));Debug.Log("ISOMETRIC_INSTALL PASS seven maps");
    }
    static T Add<T>(GameObject host)where T:Component{var c=host.GetComponent<T>();return c?c:host.AddComponent<T>();}
    static void PreserveShaders()
    {
        // Runtime materials use Shader.Find; Resources references keep both shaders in player builds.
        if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
        if(!AssetDatabase.IsValidFolder("Assets/Resources/LegendMaterials"))AssetDatabase.CreateFolder("Assets/Resources","LegendMaterials");
        foreach(string name in new[]{"LegendSurface","LegendAtmosphere"}){
            string path="Assets/Resources/LegendMaterials/"+name+".mat";
            var shader=Shader.Find("ThanhGiong/"+name);if(!shader)throw new InvalidOperationException("Missing shader "+name);
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material)AssetDatabase.CreateAsset(new Material(shader){name=name},path);
            else if(material.shader!=shader){material.shader=shader;EditorUtility.SetDirty(material);}
        }
    }
    static VolumeProfile Profile()
    {
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);if(profile!=null)return profile;
        profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);
        var colors=profile.Add<ColorAdjustments>(true);colors.contrast.value=-8;colors.saturation.value=-12;colors.colorFilter.value=new Color(.94f,.98f,.95f);
        var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.2f;bloom.threshold.value=1.1f;bloom.scatter.value=.45f;
        var grain=profile.Add<FilmGrain>(true);grain.intensity.value=.10f;grain.response.value=.8f;
        var vignette=profile.Add<Vignette>(true);vignette.intensity.value=.12f;vignette.smoothness.value=.55f;vignette.color.value=new Color(.13f,.19f,.16f);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);return profile;
    }
    static void InstallBeacons(Transform root,ThanhGiongCampaignController actor,int chapter)
    {
        Vector3 start=actor.transform.position;
        // Side of the route at spawn, then a supported open position farther into the map.
        Vector3[] desired={start+new Vector3(3,0,2),start+new Vector3(0,0,10)};
        if(chapter==3)desired[1]=new Vector3(0,start.y,-7);
        for(int i=0;i<desired.Length;i++){
            if(!Ground(desired[i],actor.transform,out var p))continue;
            var beacon=new GameObject(i==0?"Village Route Checkpoint":"Forward Route Checkpoint");beacon.transform.SetParent(root,false);beacon.transform.position=p;
            Add<LegendCheckpointBeacon>(beacon).checkpointName=i==0?"Bến hành quân":"Đường tiền tuyến";
        }
    }
    static bool Ground(Vector3 point,Transform actor,out Vector3 position)
    {
        foreach(var hit in Physics.RaycastAll(point+Vector3.up*35,Vector3.down,70,~0,QueryTriggerInteraction.Ignore).OrderBy(x=>x.distance))if(!hit.transform.IsChildOf(actor)&&hit.normal.y>.75f&&hit.point.y<actor.position.y+1f){position=hit.point+Vector3.up*.06f;return true;}
        position=point;return false;
    }
}
