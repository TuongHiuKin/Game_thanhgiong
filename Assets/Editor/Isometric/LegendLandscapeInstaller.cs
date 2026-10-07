using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LegendLandscapeInstaller
{
    [MenuItem("Tools/Thanh Giong/Isometric/Apply Landscape To All Maps")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before landscape installation.");
        EditorSceneManager.SaveOpenScenes();string previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        Directory.CreateDirectory("Backups/Landscape_20261007");
        var maps=GodotPortRuntimeVerifier.Maps;
        for(int i=0;i<maps.Length;i++){
            string path=maps[i]=="AlbionForestMap"?"Assets/Scenes/AlbionForestMap.unity":"Assets/Scenes/ThanhGiongWorld/"+maps[i]+".unity";
            string backup="Backups/Landscape_20261007/"+maps[i]+".unity";if(!File.Exists(backup))File.Copy(path,backup);
            var scene=EditorSceneManager.OpenScene(path);var actor=UnityEngine.Object.FindAnyObjectByType<MountedHorseController>();
            if(!actor)throw new InvalidOperationException("Missing actor in "+maps[i]);
            GameObject root=null;foreach(var candidate in scene.GetRootGameObjects())if(candidate.name=="Legend Isometric Presentation"){root=candidate;break;}
            if(!root)root=new GameObject("Legend Isometric Presentation");
            var landscape=root.GetComponent<LegendLandscapePresentation>();if(!landscape)landscape=root.AddComponent<LegendLandscapePresentation>();
            landscape.Configure(i,actor.transform);EditorUtility.SetDirty(landscape);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("LANDSCAPE_INSTALL "+maps[i]+" deterministic clusters / open route / no colliders");
        }
        AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous);
        Debug.Log("LANDSCAPE_INSTALL PASS seven maps");
    }
}
