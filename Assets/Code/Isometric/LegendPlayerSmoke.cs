using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Explicit command-line regression runner; normal game sessions never invoke it.
public sealed class LegendPlayerSmoke : MonoBehaviour
{
    int errors;
    string folder;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {
        if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"--verify-polish")<0)return;
        new GameObject("Standalone Verification").AddComponent<LegendPlayerSmoke>();
    }
    void Awake(){DontDestroyOnLoad(gameObject);Application.runInBackground=true;Application.logMessageReceived+=Log;}
    void Log(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors++;}
    IEnumerator Start()
    {
        folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../"));
        LegendVerificationPreferences.Capture();
        // Remove prior evidence so an incomplete run cannot report a stale success.
        foreach(string sub in new[]{"PortVerification","IsometricVerification"}){
            string report=Path.Combine(folder,sub,"runtime_results.txt");if(File.Exists(report))File.Delete(report);
        }
        new GameObject("Campaign Regression").AddComponent<GodotPortRuntimeVerifier>();
        new GameObject("Isometric Regression").AddComponent<LegendIsometricRuntimeVerifier>();
        yield return null;
        float deadline=Time.realtimeSinceStartup+180;
        while((FindFirstObjectByType<GodotPortRuntimeVerifier>()||FindFirstObjectByType<LegendIsometricRuntimeVerifier>())&&Time.realtimeSinceStartup<deadline)yield return null;
        bool pass=errors==0;
        foreach(string sub in new[]{"PortVerification","IsometricVerification"}){
            string report=Path.Combine(folder,sub,"runtime_results.txt");pass&=File.Exists(report)&&File.ReadAllText(report).Contains("VERIFY PASS failures=0");
        }
        LegendVerificationPreferences.Restore();
        string result="STANDALONE_VERIFY "+(pass?"PASS":"FAIL")+" runtime_errors="+errors;
        File.WriteAllText(Path.Combine(folder,"standalone_results.txt"),result);Debug.Log(result);Application.Quit(pass?0:1);
    }
    void OnDestroy(){Application.logMessageReceived-=Log;}
}
