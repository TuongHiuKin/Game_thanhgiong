using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LegendIsometricVerification
{
    const string Flag="ThanhGiong.Isometric.Verify";
    [InitializeOnLoadMethod]static void Init(){EditorApplication.playModeStateChanged-=OnPlay;EditorApplication.playModeStateChanged+=OnPlay;}
    [MenuItem("Tools/Thanh Giong/Isometric/Verify All")]
    static void Verify(){LegendVerificationPreferences.Capture();SessionState.SetBool(Flag,true);EditorSceneManager.OpenScene("Assets/Scenes/ThanhGiongWorld/LangGiongTienTuyen.unity");EditorApplication.isPlaying=true;}
    static void OnPlay(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)LegendVerificationPreferences.Restore();if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Flag,false)){SessionState.SetBool(Flag,false);new GameObject("Campaign Regression Verification").AddComponent<GodotPortRuntimeVerifier>();new GameObject("Isometric Runtime Verification").AddComponent<LegendIsometricRuntimeVerifier>();}}
}
