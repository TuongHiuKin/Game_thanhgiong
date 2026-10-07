using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class GodotPortVerification {
 const string Flag="ThanhGiong.GodotPort.Verify";
 [InitializeOnLoadMethod]static void Init(){EditorApplication.playModeStateChanged-=OnPlay;EditorApplication.playModeStateChanged+=OnPlay;}
 [MenuItem("Tools/Thanh Giong/Godot Port/Verify Play Mode")]
 static void Verify(){SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/Scenes/ThanhGiongWorld/LangGiongTienTuyen.unity");EditorApplication.isPlaying=true;}
 static void OnPlay(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Godot Port Runtime Verification").AddComponent<GodotPortRuntimeVerifier>();}}
}
