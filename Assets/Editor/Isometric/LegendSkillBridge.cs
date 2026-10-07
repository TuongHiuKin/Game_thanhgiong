using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LegendSkillBridge
{
    [MenuItem("Tools/Thanh Giong/Skills/Start UnitySkills Server")]
    public static void StartServer()
    {
        Type server=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnitySkills.SkillsHttpServer")).FirstOrDefault(t=>t!=null);
        if(server==null)throw new InvalidOperationException("UnitySkills package is not loaded.");
        server.GetMethod("Start",new[]{typeof(int),typeof(bool)}).Invoke(null,new object[]{8090,true});
        Debug.Log("UNITY_SKILLS_BRIDGE started; verify localhost health and expected project before calls.");
    }
}
