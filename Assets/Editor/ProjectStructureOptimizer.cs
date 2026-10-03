using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectStructureOptimizer
{
    [MenuItem("ThanhGiong/Optimize Project Structure")]
    public static void OptimizeStructure()
    {
        Debug.Log("===> Bắt đầu tối ưu hóa cấu trúc Project...");

        // 1. Tạo các thư mục chuẩn theo yêu cầu
        CreateFolderSafely("Assets", "_Project");
        CreateFolderSafely("Assets/_Project", "Screenshots");
        CreateFolderSafely("Assets", "Art");
        CreateFolderSafely("Assets/Art", "Models");
        CreateFolderSafely("Assets/Art", "Textures");
        CreateFolderSafely("Assets", "Audio");
        CreateFolderSafely("Assets/Audio", "BGM");
        CreateFolderSafely("Assets/Audio", "SFX");
        CreateFolderSafely("Assets", "Code");
        CreateFolderSafely("Assets/Code", "Player");
        CreateFolderSafely("Assets/Code", "Enemies");
        CreateFolderSafely("Assets/Code", "UI");
        CreateFolderSafely("Assets/Code", "Scripts");
        CreateFolderSafely("Assets", "Materials");
        CreateFolderSafely("Assets/Materials", "Food");
        CreateFolderSafely("Assets/Materials", "World");
        CreateFolderSafely("Assets/Materials", "Characters");
        CreateFolderSafely("Assets/Materials", "Campaign");
        CreateFolderSafely("Assets", "Prefabs");
        CreateFolderSafely("Assets/Prefabs", "Player");
        CreateFolderSafely("Assets/Prefabs", "Environment");
        CreateFolderSafely("Assets", "UI");
        CreateFolderSafely("Assets", "VFX");
        CreateFolderSafely("Assets", "Plugins");
        CreateFolderSafely("Assets", "Resources");
        CreateFolderSafely("Assets", "StreamingAssets");

        // 2. Di chuyển Code theo đúng phân loại
        MoveAssetSafely("Assets/Scripts/MountedHorseController.cs", "Assets/Code/Player/MountedHorseController.cs");
        MoveAssetSafely("Assets/Scripts/AdventurerController.cs", "Assets/Code/Player/AdventurerController.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongCampaignController.cs", "Assets/Code/Player/ThanhGiongCampaignController.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/MountedHorsePhysicsTrample.cs", "Assets/Code/Player/MountedHorsePhysicsTrample.cs");

        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongEnemy.cs", "Assets/Code/Enemies/ThanhGiongEnemy.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongRagdollPhysics.cs", "Assets/Code/Enemies/ThanhGiongRagdollPhysics.cs");

        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongCampaignHUD.cs", "Assets/Code/UI/ThanhGiongCampaignHUD.cs");

        MoveAssetSafely("Assets/Scripts/IsometricCameraFollow.cs", "Assets/Code/Scripts/IsometricCameraFollow.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongCampaignAudio.cs", "Assets/Code/Scripts/ThanhGiongCampaignAudio.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongCampaignVFX.cs", "Assets/Code/Scripts/ThanhGiongCampaignVFX.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongCollectible.cs", "Assets/Code/Scripts/ThanhGiongCollectible.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongImprovisedWeapon.cs", "Assets/Code/Scripts/ThanhGiongImprovisedWeapon.cs");
        MoveAssetSafely("Assets/Scripts/ThanhGiongCampaign/ThanhGiongRoofCollapse.cs", "Assets/Code/Scripts/ThanhGiongRoofCollapse.cs");
        MoveAssetSafely("Assets/Scripts/KayKitWorld/KayKitMapGuide.cs", "Assets/Code/Scripts/KayKitMapGuide.cs");
        MoveAssetSafely("Assets/Scripts/GameAssetAnimationPreview.cs", "Assets/Code/Scripts/GameAssetAnimationPreview.cs");

        // 3. Di chuyển 3rd Party Packages vào Plugins
        MoveAssetSafely("Assets/KayKit_Adventurers_2.0_FREE", "Assets/Plugins/KayKit_Adventurers_2.0_FREE");
        MoveAssetSafely("Assets/KayKit_Dungeon_Pack_1.1_FREE", "Assets/Plugins/KayKit_Dungeon_Pack_1.1_FREE");
        MoveAssetSafely("Assets/KayKit_Forest_Nature_Pack_1.0_FREE", "Assets/Plugins/KayKit_Forest_Nature_Pack_1.0_FREE");
        MoveAssetSafely("Assets/KayKit_Medieval_Hexagon_Pack_1.0_FREE", "Assets/Plugins/KayKit_Medieval_Hexagon_Pack_1.0_FREE");
        MoveAssetSafely("Assets/kenney_mini-forest_1.0", "Assets/Plugins/kenney_mini-forest_1.0");

        // 4. Di chuyển Art (Models & Textures)
        MoveAssetSafely("Assets/base.obj", "Assets/Art/Models/base.obj");
        MoveAssetSafely("Assets/ThanhGiong_Mounted_Albedo.png", "Assets/Art/Textures/ThanhGiong_Mounted_Albedo.png");

        if (Directory.Exists("Assets/Generated/ThanhGiong"))
        {
            foreach (var fbx in Directory.GetFiles("Assets/Generated/ThanhGiong", "*.fbx"))
            {
                string norm = fbx.Replace('\\', '/');
                MoveAssetSafely(norm, "Assets/Art/Models/" + Path.GetFileName(norm));
            }
            foreach (var png in Directory.GetFiles("Assets/Generated/ThanhGiong", "*.png"))
            {
                string norm = png.Replace('\\', '/');
                MoveAssetSafely(norm, "Assets/Art/Textures/" + Path.GetFileName(norm));
            }
        }

        // 5. Di chuyển Prefabs
        MoveAssetSafely("Assets/Generated/ThanhGiongMounted/ThanhGiong_Mounted.prefab", "Assets/Prefabs/Player/ThanhGiong_Mounted.prefab");
        MoveAssetSafely("Assets/Generated/Player/ThanhGiongKnight.prefab", "Assets/Prefabs/Player/ThanhGiongKnight.prefab");
        MoveAssetSafely("Assets/Generated/ThanhGiong/ThanhGiong_Hero.prefab", "Assets/Prefabs/Player/ThanhGiong_Hero.prefab");
        MoveAssetSafely("Assets/Generated/ThanhGiong/ThanhGiong_IronHorse.prefab", "Assets/Prefabs/Player/ThanhGiong_IronHorse.prefab");

        // 6. Di chuyển Animations / Controllers
        MoveAssetSafely("Assets/Generated/Player/ThanhGiongKnight.controller", "Assets/Animations/ThanhGiongKnight.controller");

        // 7. Di chuyển Materials
        MoveFolderContents("Assets/Generated/FoodMaterials", "Assets/Materials/Food", "*.mat");
        MoveFolderContents("Assets/Generated/ThanhGiongCampaign", "Assets/Materials/Campaign", "*.mat");
        MoveFolderContents("Assets/Generated/AlbionForest/Materials", "Assets/Materials/World", "*.mat");
        MoveFolderContents("Assets/Generated/KayKitWorlds/Materials", "Assets/Materials/World", "*.mat");
        MoveFolderContents("Assets/Generated/ThanhGiong", "Assets/Materials/Characters", "*.mat");
        MoveFolderContents("Assets/Generated/ThanhGiongMounted", "Assets/Materials/Characters", "*.mat");
        MoveAssetSafely("Assets/Generated/Player/SelectionRing.mat", "Assets/Materials/Characters/SelectionRing.mat");

        // 8. Di chuyển Tài liệu & Screenshots vào _Project
        MoveAssetSafely("Assets/THANH_GIONG_CAMPAIGN_README.md", "Assets/_Project/THANH_GIONG_CAMPAIGN_README.md");
        if (Directory.Exists("Assets/Screenshots"))
        {
            MoveFolderContents("Assets/Screenshots", "Assets/_Project/Screenshots", "*.*");
        }

        // 9. Xóa các file thừa thãi, rác không dùng
        DeleteAssetSafely("Assets/0000.webp");
        DeleteAssetSafely("Assets/0001.webp");
        DeleteAssetSafely("Assets/0002.webp");
        DeleteAssetSafely("Assets/kenney_mini-forest_1.0.zip");
        DeleteAssetSafely("Assets/Readme.asset");
        DeleteAssetSafely("Assets/TutorialInfo");
        DeleteAssetSafely("Assets/Scripts/CubeController.cs");
        DeleteAssetSafely("Assets/Scenes/SampleScene.unity");

        // Dọn dẹp thư mục rỗng cũ
        DeleteAssetSafely("Assets/Scripts/KayKitWorld");
        DeleteAssetSafely("Assets/Scripts/ThanhGiongCampaign");
        DeleteAssetSafely("Assets/Scripts");
        DeleteAssetSafely("Assets/Screenshots");
        DeleteAssetSafely("Assets/Generated/AlbionForest/Materials");
        DeleteAssetSafely("Assets/Generated/AlbionForest");
        DeleteAssetSafely("Assets/Generated/AnimatedGameAssets");
        DeleteAssetSafely("Assets/Generated/FoodMaterials");
        DeleteAssetSafely("Assets/Generated/KayKitWorlds/Materials");
        DeleteAssetSafely("Assets/Generated/KayKitWorlds");
        DeleteAssetSafely("Assets/Generated/Player");
        DeleteAssetSafely("Assets/Generated/ThanhGiong");
        DeleteAssetSafely("Assets/Generated/ThanhGiongCampaign");
        DeleteAssetSafely("Assets/Generated/ThanhGiongMounted");
        DeleteAssetSafely("Assets/Generated");

        // Tạo tài liệu cấu trúc thư mục _Project/README.md
        CreateProjectDocumentation();

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("===> Tối ưu hóa cấu trúc Project hoàn tất 100%!");
    }

    private static void CreateFolderSafely(string parent, string name)
    {
        string full = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static void MoveAssetSafely(string from, string to)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(from) != null || Directory.Exists(from) || File.Exists(from))
        {
            string err = AssetDatabase.MoveAsset(from, to);
            if (!string.IsNullOrEmpty(err))
            {
                Debug.LogWarning($"Move failed: {from} -> {to}: {err}");
            }
        }
    }

    private static void MoveFolderContents(string sourceFolder, string targetFolder, string searchPattern)
    {
        if (!Directory.Exists(sourceFolder)) return;
        foreach (var file in Directory.GetFiles(sourceFolder, searchPattern))
        {
            string norm = file.Replace('\\', '/');
            string fileName = Path.GetFileName(norm);
            MoveAssetSafely(norm, targetFolder + "/" + fileName);
        }
    }

    private static void DeleteAssetSafely(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null || Directory.Exists(path) || File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void CreateProjectDocumentation()
    {
        string docPath = "Assets/_Project/README.md";
        string content = @"# DỰ ÁN GAME THÁNH GIÓNG (THANH GIONG CAMPAIGN)
## Cấu Trúc Thư Mục Chuẩn Hóa
```
Assets
│── _Project/               # Tài liệu nội bộ, cốt truyện, hướng dẫn, screenshots
│── Animations/             # Animation clips (.anim) và Animator Controllers (.controller)
│── Art/                    # Tài nguyên đồ họa tự tạo (Models, Textures)
│   ├── Models/             # 3D models chính (base mesh, Hero, Ngựa sắt)
│   └── Textures/           # PBR Textures (Albedo, Normal, Metallic, Roughness)
│── Audio/                  # Nhạc nền dân gian ngũ cung & hiệu ứng âm thanh (BGM, SFX)
│── Code/                   # Toàn bộ mã nguồn C# của game
│   ├── Player/             # Điều khiển nhân vật Thánh Gióng, cưỡi ngựa, vươn vai, húc kẻ địch
│   ├── Enemies/            # AI giặc Ân, Tướng giặc Warlord, hiệu ứng vật lý Ragdoll
│   ├── UI/                 # HUD hiển thị tiến độ, mục tiêu, thanh nhiệt độ ngựa sắt
│   └── Scripts/            # Hệ thống chung: Camera, Audio, VFX, Lương thực, Vũ khí môi trường
│── Materials/              # Toàn bộ Material URP Lit và Shader
│   ├── Food/               # Vật liệu Cơm trắng, Cà pháo muối, Cuống cà
│   ├── World/              # Vật liệu địa hình, đường sá, đá, nước, cỏ
│   ├── Characters/         # Vật liệu nhân vật, giáp sắt, áo vàng, ngựa sắt
│   └── Campaign/           # Vật liệu khóm tre ngà, quầng lửa, đồn trại
│── Prefabs/                # Các Prefab tái sử dụng
│   ├── Player/             # Prefab Thánh Gióng cưỡi ngựa, chiến binh, ngựa sắt
│   └── Environment/        # Prefab cảnh quan, đồn trại
│── Scenes/                 # Các màn chơi chính (.unity)
│   ├── AlbionForestMap.unity   # BẢN ĐỒ CHIẾN DỊCH CHÍNH (4 MÀN CHƠI TUẦN TỰ)
│   └── ThanhGiongWorld/        # Các scene phân cảnh phụ
│── UI/                     # Tài nguyên hình ảnh UI, Icon, Font
│── VFX/                    # Hiệu ứng hạt (Particle Systems, Line Renderers)
│── Plugins/                # Gói tài nguyên bên thứ 3 (KayKit Packs, Kenney Forest)
│── Resources/              # Tài nguyên load động lúc runtime
│── StreamingAssets/        # Tệp giữ nguyên không nén
└── Editor/                 # Công cụ tùy biến Unity Editor
```

## Bốn Màn Chơi Chiến Dịch Tuần Tự
1. **Màn 1: Tiếng Rao Dưới Mái Tranh (Làng Phù Đổng)**
   - Thu thập 300/300 lương thực (Cơm gạo, Cà pháo, Bó củi, Mâm thịt) từ dân làng.
   - Gióng vươn vai hóa Khổng Lồ, mái nhà tranh sập đổ.
2. **Màn 2: Rèn Thép & Xuất Quân (Lò Rèn Triều Đình)**
   - Chuỗi QTE 4 bước: [E] Mặc giáp vàng -> [Q] Đội nón sắt -> [E] Lên ngựa sắt -> [F] Phun Hỏa Tuyến kiểm tra vũ khí.
3. **Màn 3: Trận Tuyến Núi Sóc (Càn Quét Giặc Ân)**
   - Đánh gãy gươm sắt ở 7 mạng -> Nhổ khóm tre ngà [E x3] -> Quét sạch 18 quân thù và Tướng giặc.
4. **Màn 4: Hóa Thánh Về Trời (Đỉnh Núi Sóc)**
   - Nghi thức cởi giáp trên đỉnh núi [E -> Q -> E] -> Ngựa sắt bay vút lên trời xanh.
";
        File.WriteAllText(docPath, content);
        AssetDatabase.ImportAsset(docPath);
    }
}
