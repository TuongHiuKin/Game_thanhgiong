using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ArenaAimReview
{
    public static string PrepareAimHud()
    {
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.BeginCombat();
        var follow = Camera.main.GetComponent<IsometricCameraFollow>(); follow.enabled = true;
        follow.arenaYaw=0; follow.SetArenaCameraMode(IsometricCameraFollow.ArenaCameraMode.ThirdPersonClose);
        flow.transform.rotation=Quaternion.identity; follow.SnapToTarget();
        return PositionEnemies(flow);
    }
    public static string TurnedAimHud()
    {
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.transform.rotation=Quaternion.Euler(0,90,0);
        Camera.main.GetComponent<IsometricCameraFollow>().SnapToTarget();
        return PositionEnemies(flow);
    }
    static string PositionEnemies(ThanhGiongArenaFlow flow)
    {
        int i=0;
        foreach (var e in flow.waveManager.LivingEnemies) {
            e.enabled=false; var body=e.GetComponent<Rigidbody>();
            body.position=flow.transform.position+flow.transform.forward*(i==0?3:6)+flow.transform.right*(i++==0?0:3);
            body.linearVelocity=Vector3.zero;
        }
        Physics.SyncTransforms(); return "Enemies positioned for centered aim";
    }
    public static string CaptureAimHud() => Capture("center-reticle-forward.png");
    public static string CaptureTurnedHud() => Capture("center-reticle-turned.png");
    static string Capture(string name)
    {
        var follow=Camera.main.GetComponent<IsometricCameraFollow>();
        Vector3 p=Camera.main.WorldToScreenPoint(follow.ArenaAimPoint);
        ScreenCapture.CaptureScreenshot("Artifacts/ArenaValidation/2026-10-13/"+name);
        return Newtonsoft.Json.JsonConvert.SerializeObject(new {name,reticle=ThanhGiongArenaHUD.AimScreenPoint.ToString(),strike=p.ToString(),target=Object.FindAnyObjectByType<ThanhGiongCampaignController>().HasMeleeTarget()});
    }
    public static string SourceHands()
    {
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.GetComponent<MountedHorseController>().enabled = false;
        flow.transform.rotation = Quaternion.identity;
        var skin = flow.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh");
        skin.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_HorseSkin.asset");
        var cam = Camera.main; cam.GetComponent<IsometricCameraFollow>().enabled = false;
        Vector3 point = skin.transform.TransformPoint(new Vector3(.24f, 1.34f, 0));
        cam.fieldOfView = 40;
        cam.transform.position = point + new Vector3(1.3f, .25f, 1.6f);
        cam.transform.LookAt(point);
        return "Unfiltered source hands, camera close-up";
    }
    public static string ArenaHands()
    {
        SourceHands();
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        flow.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh").sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_ArenaSkin.asset");
        return "Arena hands, identical camera";
    }
    public static string FreshArenaHands()
    {
        ArenaHands();
        var skin = Object.FindAnyObjectByType<ThanhGiongArenaFlow>().GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Horse_AnimatedMesh");
        var fresh = Object.Instantiate(skin.sharedMesh); fresh.UploadMeshData(false); skin.sharedMesh=fresh;
        return "Fresh GPU mesh upload";
    }
    public static string UploadedArenaHands()
    {
        ArenaHands();
        Object.FindAnyObjectByType<ThanhGiongArenaFlow>().GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.name=="Horse_AnimatedMesh").sharedMesh.UploadMeshData(false);
        return "Stored mesh GPU upload";
    }
    public static string HighlightPalm()
    {
        SourceHands();
        var flow = Object.FindAnyObjectByType<ThanhGiongArenaFlow>();
        var skin = flow.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "Horse_AnimatedMesh");
        var source = skin.sharedMesh;
        var v = source.vertices;
        int[] parents = Enumerable.Range(0, v.Length).ToArray();
        int Find(int i) => parents[i] == i ? i : (parents[i] = Find(parents[i]));
        var all = source.triangles;
        for (int i=0; i<all.Length; i+=3) { parents[Find(all[i])]=Find(all[i+1]); parents[Find(all[i+1])]=Find(all[i+2]); }
        var low = new System.Collections.Generic.List<int>(); var high = new System.Collections.Generic.List<int>();
        var clone = Object.Instantiate(source); clone.subMeshCount = source.subMeshCount + 2;
        for(int s=0;s<source.subMeshCount;s++) {
            var kept = new System.Collections.Generic.List<int>(); var t=source.GetTriangles(s);
            for(int i=0;i<t.Length;i+=3) {
                var dest = Find(t[i]) == Find(2564) ? (Enumerable.Range(0,3).All(k=>v[t[i+k]].y>=1.10f) ? high : low) : kept;
                dest.Add(t[i]); dest.Add(t[i+1]); dest.Add(t[i+2]);
            }
            clone.SetTriangles(kept,s);
        }
        clone.SetTriangles(low,source.subMeshCount); clone.SetTriangles(high,source.subMeshCount+1);
        var red = new Material(Shader.Find("Universal Render Pipeline/Unlit")); red.color=Color.red;
        var green = new Material(Shader.Find("Universal Render Pipeline/Unlit")); green.color=Color.green;
        skin.sharedMesh=clone; skin.sharedMaterials=skin.sharedMaterials.Concat(new[]{red,green}).ToArray();
        return "Red: mixed palm/shaft triangles rejected; green: restored triangles";
    }
    public static string AuditArms()
    {
        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_HorseSkin.asset");
        var arena = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_ArenaSkin.asset");
        var vertices = source.vertices;
        var arenaVertices = arena.vertices;
        var parents = Enumerable.Range(0, vertices.Length).ToArray();
        int Find(int i) => parents[i] == i ? i : (parents[i] = Find(parents[i]));
        var triangles = source.triangles;
        for (int i = 0; i < triangles.Length; i += 3) {
            parents[Find(triangles[i])] = Find(triangles[i + 1]);
            parents[Find(triangles[i + 1])] = Find(triangles[i + 2]);
        }
        var kept = new System.Collections.Generic.HashSet<int>(arena.triangles);
        var groups = Enumerable.Range(0, vertices.Length).GroupBy(Find).Select(g => {
            var bounds = new Bounds(vertices[g.First()], Vector3.zero);
            foreach (int i in g) bounds.Encapsulate(vertices[i]);
            return new { landmark = g.Min(), count = g.Count(), kept = g.Count(kept.Contains),
                min = new[] {bounds.min.x,bounds.min.y,bounds.min.z}, max = new[] {bounds.max.x,bounds.max.y,bounds.max.z} };
        }).Where(g => g.max[1] > .95f).OrderBy(g => g.min[1]).ToArray();
        string result = Newtonsoft.Json.JsonConvert.SerializeObject(groups, Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Artifacts/ArenaValidation/2026-10-13");
        File.WriteAllText("Artifacts/ArenaValidation/2026-10-13/arm-components.json", result);
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { groups = groups.Length, vertexDelta = vertices.Select((v,i)=>Vector3.Distance(v,arenaVertices[i])).Max(), landmarks = new[] {1284,1341,2564,2041,2042,1903}.Select(i => new {index=i, root=Find(i), vertex=vertices[i].ToString(), arenaVertex=arenaVertices[i].ToString()}) });
    }
}
