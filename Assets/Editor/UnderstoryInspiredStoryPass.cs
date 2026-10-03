using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnderstoryInspiredStoryPass
{
    private const string SceneRoot = "Assets/Scenes/ThanhGiongWorld/";
    private const string PassRoot = "UNDERSTORY INSPIRED - THANH GIONG STORY PASS";
    private const string AutoKey = "ThanhGiong.UnderstoryStoryPass.2026-10-02-v1";

    private struct SceneSpec
    {
        public string scene, chapter, purpose;
        public Color fog, groundAccent, magic;
        public SceneSpec(string s, string c, string p, Color f, Color g, Color m)
        { scene=s; chapter=c; purpose=p; fog=f; groundAccent=g; magic=m; }
    }

    private static readonly SceneSpec[] Specs =
    {
        new("LangGiongTienTuyen", "Màn 1 — Tiếng Rao Dưới Mái Tranh", "Thu lương thực, lớn lên và rời làng.", Hex("9FBFB4"), Hex("6A8C4A"), Hex("FFD27A")),
        new("KinhThanhRenThep", "Màn 2 — Rèn Thép & Xuất Quân", "Nhận giáp, gươm và đánh thức Ngựa Sắt.", Hex("8E9A92"), Hex("9A6A3A"), Hex("FF8A32")),
        new("PhaoDaiNgamQuanAn", "Màn 3A — Pháo Đài Ngầm", "Xuyên qua bóng tối và phá kho binh khí giặc Ân.", Hex("1E333A"), Hex("2A2433"), Hex("BFF4FF")),
        new("ThungLungVuotSong", "Màn 3B — Vượt Sông", "Vượt cầu, phá vòng vây và mở đường lên Núi Sóc.", Hex("9FBFB4"), Hex("3B6A72"), Hex("E4F1E6")),
        new("TranTuyenNuiSoc", "Màn 3C — Trận Tuyến Núi Sóc", "Gươm gãy, nhổ Tre Ngà và quét sạch quân Ân.", Hex("6E5A50"), Hex("A84A2B"), Hex("FFB43B")),
        new("DinhSocHoaThanh", "Màn 4 — Đỉnh Sóc Hóa Thánh", "Cởi giáp, cưỡi Ngựa Sắt xuyên mây về trời.", Hex("B9D3C7"), Hex("EFE9D6"), Hex("FFD27A"))
    };

    [InitializeOnLoadMethod]
    private static void QueueOnce()
    {
        if (EditorPrefs.GetBool(AutoKey, false)) return;
        EditorApplication.delayCall += TryRun;
    }

    private static void TryRun()
    {
        if (Application.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += TryRun; return; }
        EditorPrefs.SetBool(AutoKey, true);
        ApplyAll();
    }

    [MenuItem("Tools/Thanh Giong/World/Apply Living Painted Story Pass")]
    public static void ApplyAll()
    {
        if (!EditorSceneManager.SaveOpenScenes()) return;
        string original = SceneManager.GetActiveScene().path;
        foreach (SceneSpec spec in Specs) Apply(spec);
        AssetDatabase.SaveAssets();
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log("[Thanh Giong] Applied Living Painted Story Pass to 6 campaign maps.");
    }

    private static void Apply(SceneSpec spec)
    {
        string path = SceneRoot + spec.scene + ".unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) return;
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        GameObject old = scene.GetRootGameObjects().FirstOrDefault(x => x.name == PassRoot);
        if (old != null) UnityEngine.Object.DestroyImmediate(old);
        GameObject root = new GameObject(PassRoot);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = spec.fog;
        RenderSettings.fogDensity = spec.scene == "PhaoDaiNgamQuanAn" ? .018f : .0065f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(spec.fog, Color.white, .35f);
        RenderSettings.ambientEquatorColor = spec.groundAccent;
        RenderSettings.ambientGroundColor = Hex("15201C");

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        if (camera != null)
        {
            camera.fieldOfView = 34f;
            IsometricCameraFollow follow = camera.GetComponent<IsometricCameraFollow>();
            if (follow != null) { follow.fieldOfView = 34f; follow.pitch = 52f; follow.lookAhead = 3.5f; }
        }

        AddHeroAccent(scene, spec.magic);
        BuildStoryPath(root.transform, spec);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildStoryPath(Transform root, SceneSpec spec)
    {
        Vector3[] points = { new(0,.08f,-28), new(-12,.08f,-10), new(10,.08f,8), new(0,.08f,27) };
        string[] names = { "Khởi hành", "Thử thách môi trường", "Điểm nhìn toàn cảnh", "Đấu trường cao trào" };
        IsometricCameraFollow.Shot[] shots = { IsometricCameraFollow.Shot.Explore, IsometricCameraFollow.Shot.Combat, IsometricCameraFollow.Shot.Vista, IsometricCameraFollow.Shot.Boss };
        for (int i=0;i<points.Length;i++)
        {
            GameObject beat = new GameObject($"{i+1}. {names[i]} — {spec.chapter}");
            beat.transform.SetParent(root); beat.transform.position = points[i];
            BoxCollider trigger = beat.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(9,4,9);
            ThanhGiongStoryBeatZone zone = beat.AddComponent<ThanhGiongStoryBeatZone>();
            zone.beatTitle = names[i]; zone.storyPurpose = spec.purpose; zone.cameraShot = shots[i];
            CreateBeacon(beat.transform, spec.magic, i == 2 ? 3.5f : 1.6f);
        }

        if (spec.scene == "LangGiongTienTuyen" || spec.scene == "ThungLungVuotSong") BuildBoardwalk(root, spec.groundAccent);
        else if (spec.scene == "KinhThanhRenThep" || spec.scene == "TranTuyenNuiSoc") BuildEmberTrail(root, spec.magic);
        else if (spec.scene == "PhaoDaiNgamQuanAn") BuildRootdeep(root, spec.groundAccent);
        else BuildCloudStair(root, spec.magic);
    }

    private static void BuildBoardwalk(Transform root, Color color)
    {
        Material mat = Material("Story_Boardwalk", Color.Lerp(color, Hex("5B351E"), .7f));
        for (int i=0;i<13;i++) Primitive(PrimitiveType.Cube, "Cầu ván " + (i+1), root, new Vector3(Mathf.Sin(i*.55f)*4f,.12f,-24+i*4f), new Vector3(5.2f,.22f,2.5f), mat, Quaternion.Euler(0, Mathf.Sin(i*.55f)*12f, 0));
    }

    private static void BuildEmberTrail(Transform root, Color color)
    {
        Material mat = Material("Story_Ember", color, true);
        for (int i=0;i<18;i++) Primitive(PrimitiveType.Sphere, "Than hồng " + i, root, new Vector3(Mathf.Sin(i*2.1f)*10f,.22f,-22+i*2.8f), Vector3.one*(.18f+(i%3)*.08f), mat, Quaternion.identity);
    }

    private static void BuildRootdeep(Transform root, Color color)
    {
        Material mat = Material("Story_InkRoot", Color.Lerp(color, Hex("15201C"), .65f));
        for (int i=0;i<10;i++) Primitive(PrimitiveType.Cylinder, "Rễ cổ thụ " + i, root, new Vector3((i%2==0?-1:1)*(11+i%3*3),2f,-22+i*5f), new Vector3(1.2f,4f,1.2f), mat, Quaternion.Euler(8,0,(i%2==0?1:-1)*18));
    }

    private static void BuildCloudStair(Transform root, Color color)
    {
        Material mat = Material("Story_CloudStone", Color.Lerp(Hex("EFE9D6"), color, .18f));
        for (int i=0;i<12;i++) Primitive(PrimitiveType.Cube, "Bậc lên trời " + i, root, new Vector3(-12+i*2.2f,.2f+i*.32f,-20+i*3.4f), new Vector3(5f,.45f,3f), mat, Quaternion.Euler(0,i*2f,0));
    }

    private static void AddHeroAccent(Scene scene, Color glow)
    {
        Transform player = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).FirstOrDefault(x=>x.CompareTag("Player"));
        if (player == null || player.Find("Khăn Đỏ — Điểm Nhấn Anh Hùng") != null) return;
        GameObject scarf = Primitive(PrimitiveType.Cube, "Khăn Đỏ — Điểm Nhấn Anh Hùng", player, Vector3.zero, new Vector3(.65f,.12f,.16f), Material("Hero_Scarf_Red", Hex("E5452C"), true), Quaternion.Euler(0,0,-12));
        scarf.transform.localPosition = new Vector3(0f,1.45f,-.12f);
    }

    private static void CreateBeacon(Transform parent, Color color, float height)
    {
        GameObject beacon = Primitive(PrimitiveType.Cylinder, "Đèn dẫn truyện", parent, parent.position + Vector3.up*height*.5f, new Vector3(.14f,height*.5f,.14f), Material("Story_Lumen", color, true), Quaternion.identity);
        Collider collider = beacon.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Quaternion rotation)
    {
        GameObject go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent); go.transform.position=position; go.transform.rotation=rotation; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; return go;
    }

    private static Material Material(string name, Color color, bool emission=false)
    {
        string folder="Assets/Materials/World/StoryPass"; if(!AssetDatabase.IsValidFolder(folder)){System.IO.Directory.CreateDirectory(folder);AssetDatabase.Refresh();}
        string path=folder+"/"+name+".mat"; Material mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){Shader shader=Shader.Find("Universal Render Pipeline/Lit");mat=new Material(shader){name=name};AssetDatabase.CreateAsset(mat,path);}
        mat.color=color; if(emission&&mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.8f);} EditorUtility.SetDirty(mat); return mat;
    }

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value, out Color color); return color; }
}
