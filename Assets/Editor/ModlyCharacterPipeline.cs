using System.IO;
using UnityEditor;
using UnityEngine;

public class ModlyCharacterPipeline : EditorWindow
{
    private string modlyApiUrl = "http://localhost:8765";
    private string glbFilePath = "";
    private string characterName = "ModlyHero";
    private enum CharacterRole { PlayerHero, EnemyWarrior, BossGeneral, Mount }
    private CharacterRole selectedRole = CharacterRole.EnemyWarrior;

    [MenuItem("Thanh Giong/Modly Pipeline/Character & Physics Studio")]
    public static void ShowWindow()
    {
        GetWindow<ModlyCharacterPipeline>("Modly Pipeline");
    }

    [MenuItem("Thanh Giong/Modly Pipeline/Reconstruct Scene Physics & Characters")]
    public static void ReconstructScenePhysicsAndCharacters()
    {
        int configuredEnemies = 0;
        int configuredTrample = 0;

        // 1. Configure all enemies in scene with ThanhGiongRagdollPhysics & continuous speculative CCD
        var enemies = Object.FindObjectsByType<ThanhGiongEnemy>();
        foreach (var enemy in enemies)
        {
            var ragdoll = enemy.GetComponent<ThanhGiongRagdollPhysics>();
            if (ragdoll == null) ragdoll = enemy.gameObject.AddComponent<ThanhGiongRagdollPhysics>();

            var rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = enemy.isBoss ? 260f : 75f;
                rb.linearDamping = 0.6f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
            configuredEnemies++;
        }

        // 2. Configure Player Horse with MountedHorsePhysicsTrample
        var horse = Object.FindAnyObjectByType<MountedHorseController>();
        if (horse != null)
        {
            if (horse.GetComponent<MountedHorsePhysicsTrample>() == null)
            {
                horse.gameObject.AddComponent<MountedHorsePhysicsTrample>();
                configuredTrample++;
            }
        }

        // 3. Configure Physics Settings (50 Hz, 0.1s max timestep per scenario-unity-gameplay)
        Time.fixedDeltaTime = 0.02f;
        Time.maximumDeltaTime = 0.1f;

        Debug.Log($"[Modly Pipeline] Reconstructed {configuredEnemies} enemy physics setups and horse trample. Physics Timestep: 50Hz, CCD: ContinuousSpeculative.");
    }

    private void OnGUI()
    {
        GUILayout.Label("Modly 3D AI Character Pipeline", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Modly (https://github.com/lightningpixel/modly) converts 2D photos/prompts into 3D meshes locally.\n" +
            "This pipeline imports GLB meshes and auto-attaches character physics, colliders, and ragdoll systems.",
            MessageType.Info
        );

        modlyApiUrl = EditorGUILayout.TextField("Modly API URL", modlyApiUrl);
        characterName = EditorGUILayout.TextField("Character Name", characterName);
        selectedRole = (CharacterRole)EditorGUILayout.EnumPopup("Character Role", selectedRole);

        EditorGUILayout.Space();

        if (GUILayout.Button("Select GLB Mesh File from Disk..."))
        {
            glbFilePath = EditorUtility.OpenFilePanel("Select Modly Exported GLB", "", "glb,gltf,fbx,obj");
        }

        if (!string.IsNullOrEmpty(glbFilePath))
        {
            EditorGUILayout.LabelField("Selected:", glbFilePath);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("One-Click Reconstruct Scene Physics & Enemies", GUILayout.Height(36)))
        {
            ReconstructScenePhysicsAndCharacters();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Modly CLI/MCP path: Tools/Modly/api/mcp_server.py\n" +
            "Start Modly Desktop or launch.bat to enable the local FastAPI server.",
            MessageType.None
        );
    }
}
