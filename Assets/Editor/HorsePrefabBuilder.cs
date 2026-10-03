using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ThanhGiong.EditorTools
{
    public static class HorsePrefabBuilder
    {
        [MenuItem("Tools/Mounts/Build All Horse Prefabs")]
        public static void BuildAll()
        {
            BuildStandardHorse();
            BuildIronHorse();
        }

        [MenuItem("Tools/Mounts/Build Standard KayKit Horse")]
        public static void BuildStandardHorse()
        {
            // Load Materials
            Material matBody = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Body.mat");
            Material matSnout = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Snout.mat");
            Material matMane = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_ManeTail.mat");
            Material matHoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Hoof.mat");
            Material matSaddle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Saddle.mat");
            Material matBlanket = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Blanket.mat");
            Material matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Metal.mat");
            Material matIron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Iron.mat");
            Material matEye = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Eye.mat");

            // Root GameObject
            GameObject root = new GameObject("KayKit_Horse_Modular");
            var animator = root.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Horse/Horse_Controller.controller");
            animator.runtimeAnimatorController = controller;

            var horseCtrl = root.AddComponent<ThanhGiong.Mounts.HorseController>();

            var mainCol = root.AddComponent<BoxCollider>();
            mainCol.center = new Vector3(0f, 0.8f, 0f);
            mainCol.size = new Vector3(0.9f, 1.6f, 1.8f);

            GameObject hip = new GameObject("Root_Hip");
            hip.transform.SetParent(root.transform, false);
            hip.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            CreatePrimitivePart("Horse_Body", PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.68f, 0.65f, 1.25f), matBody, hip.transform);

            GameObject neckHeadJoint = new GameObject("Neck_Head_Joint");
            neckHeadJoint.transform.SetParent(hip.transform, false);
            neckHeadJoint.transform.localPosition = new Vector3(0f, 0.22f, 0.55f);

            CreatePrimitivePart("Horse_Neck", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0.16f), new Vector3(32f, 0f, 0f), new Vector3(0.38f, 0.65f, 0.45f), matBody, neckHeadJoint.transform);
            CreatePrimitivePart("Horse_Head", PrimitiveType.Cube, new Vector3(0f, 0.60f, 0.34f), new Vector3(12f, 0f, 0f), new Vector3(0.42f, 0.46f, 0.58f), matBody, neckHeadJoint.transform);
            CreatePrimitivePart("Horse_Snout", PrimitiveType.Cube, new Vector3(0f, 0.50f, 0.64f), new Vector3(12f, 0f, 0f), new Vector3(0.36f, 0.30f, 0.28f), matSnout, neckHeadJoint.transform);

            CreatePrimitivePart("Eye_L", PrimitiveType.Sphere, new Vector3(-0.22f, 0.64f, 0.42f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.06f), matEye, neckHeadJoint.transform);
            CreatePrimitivePart("Eye_R", PrimitiveType.Sphere, new Vector3(0.22f, 0.64f, 0.42f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.06f), matEye, neckHeadJoint.transform);

            CreatePrimitivePart("Ear_L", PrimitiveType.Cube, new Vector3(-0.16f, 0.88f, 0.18f), new Vector3(-15f, 0f, -12f), new Vector3(0.10f, 0.22f, 0.12f), matBody, neckHeadJoint.transform);
            CreatePrimitivePart("Ear_R", PrimitiveType.Cube, new Vector3(0.16f, 0.88f, 0.18f), new Vector3(-15f, 0f, 12f), new Vector3(0.10f, 0.22f, 0.12f), matBody, neckHeadJoint.transform);

            CreatePrimitivePart("Horse_Mane", PrimitiveType.Cube, new Vector3(0f, 0.46f, 0.0f), new Vector3(32f, 0f, 0f), new Vector3(0.18f, 0.72f, 0.24f), matMane, neckHeadJoint.transform);

            GameObject bridleGroup = new GameObject("Bridle_Reins_Group");
            bridleGroup.transform.SetParent(neckHeadJoint.transform, false);
            CreatePrimitivePart("Horse_Bridle", PrimitiveType.Cube, new Vector3(0f, 0.53f, 0.46f), new Vector3(12f, 0f, 0f), new Vector3(0.44f, 0.38f, 0.14f), matSaddle, bridleGroup.transform);
            CreatePrimitivePart("Horse_Reins", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0.15f), new Vector3(-25f, 0f, 0f), new Vector3(0.46f, 0.06f, 0.70f), matSaddle, bridleGroup.transform);

            GameObject armorHeadGroup = new GameObject("Armor_Head_Group");
            armorHeadGroup.transform.SetParent(neckHeadJoint.transform, false);
            CreatePrimitivePart("Horse_Armor_Head", PrimitiveType.Cube, new Vector3(0f, 0.64f, 0.42f), new Vector3(12f, 0f, 0f), new Vector3(0.44f, 0.36f, 0.40f), matIron, armorHeadGroup.transform);
            armorHeadGroup.SetActive(false);

            GameObject saddleGroup = new GameObject("Saddle_Group");
            saddleGroup.transform.SetParent(hip.transform, false);
            CreatePrimitivePart("Horse_Blanket", PrimitiveType.Cube, new Vector3(0f, 0.34f, -0.05f), Vector3.zero, new Vector3(0.74f, 0.08f, 0.76f), matBlanket, saddleGroup.transform);
            CreatePrimitivePart("Horse_Saddle", PrimitiveType.Cube, new Vector3(0f, 0.41f, -0.05f), Vector3.zero, new Vector3(0.58f, 0.16f, 0.52f), matSaddle, saddleGroup.transform);
            CreatePrimitivePart("Stirrup_L", PrimitiveType.Cube, new Vector3(-0.38f, 0.05f, -0.05f), Vector3.zero, new Vector3(0.06f, 0.50f, 0.08f), matMetal, saddleGroup.transform);
            CreatePrimitivePart("Stirrup_R", PrimitiveType.Cube, new Vector3(0.38f, 0.05f, -0.05f), Vector3.zero, new Vector3(0.06f, 0.50f, 0.08f), matMetal, saddleGroup.transform);

            GameObject armorChestGroup = new GameObject("Armor_Chest_Group");
            armorChestGroup.transform.SetParent(hip.transform, false);
            CreatePrimitivePart("Horse_Armor_Chest", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.64f), new Vector3(-12f, 0f, 0f), new Vector3(0.72f, 0.52f, 0.16f), matIron, armorChestGroup.transform);
            armorChestGroup.SetActive(false);

            GameObject tailJoint = new GameObject("Tail_Joint");
            tailJoint.transform.SetParent(hip.transform, false);
            tailJoint.transform.localPosition = new Vector3(0f, 0.18f, -0.62f);
            CreatePrimitivePart("Horse_Tail", PrimitiveType.Cube, new Vector3(0f, -0.28f, -0.16f), new Vector3(-25f, 0f, 0f), new Vector3(0.18f, 0.60f, 0.22f), matMane, tailJoint.transform);

            CreateLeg("Leg_FL_Joint", new Vector3(-0.24f, -0.15f, 0.42f), hip.transform, matBody, matHoof);
            CreateLeg("Leg_FR_Joint", new Vector3(0.24f, -0.15f, 0.42f), hip.transform, matBody, matHoof);
            CreateLeg("Leg_BL_Joint", new Vector3(-0.24f, -0.15f, -0.42f), hip.transform, matBody, matHoof);
            CreateLeg("Leg_BR_Joint", new Vector3(0.24f, -0.15f, -0.42f), hip.transform, matBody, matHoof);

            GameObject vfxRoot = new GameObject("VFX_Effects");
            vfxRoot.transform.SetParent(root.transform, false);

            var dustPS = CreateDustVFX(vfxRoot.transform);
            var auraPS = CreateAuraVFX(vfxRoot.transform);

            SerializedObject so = new SerializedObject(horseCtrl);
            so.FindProperty("saddleGroup").objectReferenceValue = saddleGroup;
            so.FindProperty("bridleGroup").objectReferenceValue = bridleGroup;
            so.FindProperty("armorHeadGroup").objectReferenceValue = armorHeadGroup;
            so.FindProperty("armorChestGroup").objectReferenceValue = armorChestGroup;
            so.FindProperty("hoofDustVFX").objectReferenceValue = dustPS;
            so.FindProperty("ironAuraVFX").objectReferenceValue = auraPS;
            so.FindProperty("standardBodyMat").objectReferenceValue = matBody;
            so.FindProperty("ironHorseBodyMat").objectReferenceValue = matIron;

            var bodyRends = new List<Renderer>();
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterial == matBody || mr.sharedMaterial == matSnout)
                {
                    bodyRends.Add(mr);
                }
            }
            var rendArray = so.FindProperty("bodyRenderers");
            rendArray.arraySize = bodyRends.Count;
            for (int i = 0; i < bodyRends.Count; i++)
            {
                rendArray.GetArrayElementAtIndex(i).objectReferenceValue = bodyRends[i];
            }
            so.ApplyModifiedProperties();

            string prefabPath = "Assets/Prefabs/Mounts/KayKit_Horse_Modular.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[HorsePrefabBuilder] Built standard mount: " + prefabPath + "</color>");
        }

        [MenuItem("Tools/Mounts/Build KayKit Iron Horse (Thanh Giong)")]
        public static void BuildIronHorse()
        {
            // Load Specific Iron Horse Materials
            Material matObsidian = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_Obsidian.mat");
            Material matMagma = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_MagmaCore.mat");
            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_GoldCrest.mat");
            Material matSaddle = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_CrimsonSaddle.mat");
            Material matEyeGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_EyeGlow.mat");
            Material matHoof = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Hoof.mat");
            Material matMetal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_Horse_Metal.mat");

            GameObject root = new GameObject("KayKit_IronHorse_ThanhGiong");
            var animator = root.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/Horse/Horse_Controller.controller");
            animator.runtimeAnimatorController = controller;

            var horseCtrl = root.AddComponent<ThanhGiong.Mounts.HorseController>();

            var mainCol = root.AddComponent<BoxCollider>();
            mainCol.center = new Vector3(0f, 0.8f, 0f);
            mainCol.size = new Vector3(0.9f, 1.6f, 1.8f);

            GameObject hip = new GameObject("Root_Hip");
            hip.transform.SetParent(root.transform, false);
            hip.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            // 1. Obsidian Torso
            CreatePrimitivePart("Horse_Body", PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.70f, 0.68f, 1.28f), matObsidian, hip.transform);

            // Magma Vents on Body
            CreatePrimitivePart("Magma_Vent_L", PrimitiveType.Cube, new Vector3(-0.36f, 0.05f, 0.0f), Vector3.zero, new Vector3(0.02f, 0.08f, 0.70f), matMagma, hip.transform);
            CreatePrimitivePart("Magma_Vent_R", PrimitiveType.Cube, new Vector3(0.36f, 0.05f, 0.0f), Vector3.zero, new Vector3(0.02f, 0.08f, 0.70f), matMagma, hip.transform);

            // 2. Neck & Head Joint
            GameObject neckHeadJoint = new GameObject("Neck_Head_Joint");
            neckHeadJoint.transform.SetParent(hip.transform, false);
            neckHeadJoint.transform.localPosition = new Vector3(0f, 0.22f, 0.55f);

            CreatePrimitivePart("Horse_Neck", PrimitiveType.Cube, new Vector3(0f, 0.32f, 0.16f), new Vector3(32f, 0f, 0f), new Vector3(0.40f, 0.68f, 0.48f), matObsidian, neckHeadJoint.transform);
            CreatePrimitivePart("Magma_NeckVein_1", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.24f), new Vector3(32f, 0f, 0f), new Vector3(0.42f, 0.06f, 0.38f), matMagma, neckHeadJoint.transform);
            CreatePrimitivePart("Magma_NeckVein_2", PrimitiveType.Cube, new Vector3(0f, 0.44f, 0.15f), new Vector3(32f, 0f, 0f), new Vector3(0.42f, 0.06f, 0.38f), matMagma, neckHeadJoint.transform);

            CreatePrimitivePart("Horse_Head", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.36f), new Vector3(12f, 0f, 0f), new Vector3(0.44f, 0.48f, 0.60f), matObsidian, neckHeadJoint.transform);
            CreatePrimitivePart("Horse_Snout", PrimitiveType.Cube, new Vector3(0f, 0.52f, 0.68f), new Vector3(12f, 0f, 0f), new Vector3(0.38f, 0.32f, 0.30f), matObsidian, neckHeadJoint.transform);

            // Glowing Fiery Eyes
            CreatePrimitivePart("Eye_L", PrimitiveType.Sphere, new Vector3(-0.23f, 0.66f, 0.44f), Vector3.zero, new Vector3(0.10f, 0.10f, 0.08f), matEyeGlow, neckHeadJoint.transform);
            CreatePrimitivePart("Eye_R", PrimitiveType.Sphere, new Vector3(0.23f, 0.66f, 0.44f), Vector3.zero, new Vector3(0.10f, 0.10f, 0.08f), matEyeGlow, neckHeadJoint.transform);

            // Ears
            CreatePrimitivePart("Ear_L", PrimitiveType.Cube, new Vector3(-0.16f, 0.90f, 0.20f), new Vector3(-15f, 0f, -12f), new Vector3(0.10f, 0.24f, 0.12f), matObsidian, neckHeadJoint.transform);
            CreatePrimitivePart("Ear_R", PrimitiveType.Cube, new Vector3(0.16f, 0.90f, 0.20f), new Vector3(-15f, 0f, 12f), new Vector3(0.10f, 0.24f, 0.12f), matObsidian, neckHeadJoint.transform);

            // Mane (Obsidian Plates)
            CreatePrimitivePart("Horse_Mane", PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.0f), new Vector3(32f, 0f, 0f), new Vector3(0.18f, 0.74f, 0.26f), matObsidian, neckHeadJoint.transform);

            // Golden Dragon/Lion Chest Crest
            GameObject chestCrestGroup = new GameObject("Chest_DragonLion_Crest");
            chestCrestGroup.transform.SetParent(hip.transform, false);
            chestCrestGroup.transform.localPosition = new Vector3(0f, 0.08f, 0.66f);
            CreatePrimitivePart("Golden_Lion_Head", PrimitiveType.Cube, Vector3.zero, new Vector3(-10f, 0f, 0f), new Vector3(0.45f, 0.42f, 0.18f), matGold, chestCrestGroup.transform);
            CreatePrimitivePart("Golden_Wing_L", PrimitiveType.Cube, new Vector3(-0.30f, 0.05f, -0.08f), new Vector3(-10f, -25f, 0f), new Vector3(0.25f, 0.28f, 0.10f), matGold, chestCrestGroup.transform);
            CreatePrimitivePart("Golden_Wing_R", PrimitiveType.Cube, new Vector3(0.30f, 0.05f, -0.08f), new Vector3(-10f, 25f, 0f), new Vector3(0.25f, 0.28f, 0.10f), matGold, chestCrestGroup.transform);

            // Crimson & Gold Adventurer Saddle
            GameObject saddleGroup = new GameObject("Saddle_Group");
            saddleGroup.transform.SetParent(hip.transform, false);
            CreatePrimitivePart("Horse_Blanket", PrimitiveType.Cube, new Vector3(0f, 0.36f, -0.05f), Vector3.zero, new Vector3(0.76f, 0.08f, 0.78f), matGold, saddleGroup.transform);
            CreatePrimitivePart("Horse_Saddle", PrimitiveType.Cube, new Vector3(0f, 0.44f, -0.05f), Vector3.zero, new Vector3(0.60f, 0.18f, 0.54f), matSaddle, saddleGroup.transform);
            CreatePrimitivePart("Stirrup_L", PrimitiveType.Cube, new Vector3(-0.40f, 0.05f, -0.05f), Vector3.zero, new Vector3(0.06f, 0.52f, 0.08f), matMetal, saddleGroup.transform);
            CreatePrimitivePart("Stirrup_R", PrimitiveType.Cube, new Vector3(0.40f, 0.05f, -0.05f), Vector3.zero, new Vector3(0.06f, 0.52f, 0.08f), matMetal, saddleGroup.transform);

            // Tail
            GameObject tailJoint = new GameObject("Tail_Joint");
            tailJoint.transform.SetParent(hip.transform, false);
            tailJoint.transform.localPosition = new Vector3(0f, 0.18f, -0.64f);
            CreatePrimitivePart("Horse_Tail", PrimitiveType.Cube, new Vector3(0f, -0.30f, -0.16f), new Vector3(-25f, 0f, 0f), new Vector3(0.18f, 0.65f, 0.24f), matObsidian, tailJoint.transform);

            // 4 Legs with Mechanical Energy Cores
            CreateIronLeg("Leg_FL_Joint", new Vector3(-0.25f, -0.15f, 0.44f), hip.transform, matObsidian, matHoof, matMagma, matMetal);
            CreateIronLeg("Leg_FR_Joint", new Vector3(0.25f, -0.15f, 0.44f), hip.transform, matObsidian, matHoof, matMagma, matMetal);
            CreateIronLeg("Leg_BL_Joint", new Vector3(-0.25f, -0.15f, -0.44f), hip.transform, matObsidian, matHoof, matMagma, matMetal);
            CreateIronLeg("Leg_BR_Joint", new Vector3(0.25f, -0.15f, -0.44f), hip.transform, matObsidian, matHoof, matMagma, matMetal);

            // VFX Root
            GameObject vfxRoot = new GameObject("VFX_Effects");
            vfxRoot.transform.SetParent(root.transform, false);

            var dustPS = CreateDustVFX(vfxRoot.transform);
            var auraPS = CreateAuraVFX(vfxRoot.transform);
            var firePS = CreateFireBreathVFX(neckHeadJoint.transform);

            // Wire Serialized Properties
            SerializedObject so = new SerializedObject(horseCtrl);
            so.FindProperty("saddleGroup").objectReferenceValue = saddleGroup;
            so.FindProperty("bridleGroup").objectReferenceValue = null;
            so.FindProperty("armorHeadGroup").objectReferenceValue = null;
            so.FindProperty("armorChestGroup").objectReferenceValue = chestCrestGroup;
            so.FindProperty("hoofDustVFX").objectReferenceValue = dustPS;
            so.FindProperty("ironAuraVFX").objectReferenceValue = auraPS;
            so.FindProperty("fireBreathVFX").objectReferenceValue = firePS;
            so.FindProperty("standardBodyMat").objectReferenceValue = matObsidian;
            so.FindProperty("ironHorseBodyMat").objectReferenceValue = matObsidian;
            so.FindProperty("isIronHorse").boolValue = true;
            so.FindProperty("hasArmor").boolValue = true;
            so.FindProperty("hasSaddle").boolValue = true;

            var bodyRends = new List<Renderer>();
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.sharedMaterial == matObsidian)
                {
                    bodyRends.Add(mr);
                }
            }
            var rendArray = so.FindProperty("bodyRenderers");
            rendArray.arraySize = bodyRends.Count;
            for (int i = 0; i < bodyRends.Count; i++)
            {
                rendArray.GetArrayElementAtIndex(i).objectReferenceValue = bodyRends[i];
            }
            so.ApplyModifiedProperties();

            string prefabPath = "Assets/Prefabs/Mounts/KayKit_IronHorse_ThanhGiong.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[HorsePrefabBuilder] Built legendary Iron Horse: " + prefabPath + "</color>");
        }

        private static GameObject CreatePrimitivePart(string name, PrimitiveType type, Vector3 pos, Vector3 rot, Vector3 scale, Material mat, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = rot;
            go.transform.localScale = scale;
            if (mat != null)
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = mat;
            }
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return go;
        }

        private static void CreateLeg(string name, Vector3 offset, Transform parent, Material bodyMat, Material hoofMat)
        {
            GameObject legJoint = new GameObject(name);
            legJoint.transform.SetParent(parent, false);
            legJoint.transform.localPosition = offset;

            CreatePrimitivePart(name + "_Upper", PrimitiveType.Cube, new Vector3(0f, -0.32f, 0f), Vector3.zero, new Vector3(0.20f, 0.58f, 0.22f), bodyMat, legJoint.transform);
            CreatePrimitivePart(name + "_Hoof", PrimitiveType.Cube, new Vector3(0f, -0.68f, 0f), Vector3.zero, new Vector3(0.22f, 0.16f, 0.25f), hoofMat, legJoint.transform);
        }

        private static void CreateIronLeg(string name, Vector3 offset, Transform parent, Material bodyMat, Material hoofMat, Material magmaMat, Material rimMat)
        {
            GameObject legJoint = new GameObject(name);
            legJoint.transform.SetParent(parent, false);
            legJoint.transform.localPosition = offset;

            CreatePrimitivePart(name + "_Upper", PrimitiveType.Cube, new Vector3(0f, -0.32f, 0f), Vector3.zero, new Vector3(0.22f, 0.58f, 0.24f), bodyMat, legJoint.transform);

            // Glowing Energy Core on Joint
            float sideSign = offset.x < 0 ? -1f : 1f;
            CreatePrimitivePart(name + "_CoreRim", PrimitiveType.Cylinder, new Vector3(0.12f * sideSign, -0.15f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.22f, 0.03f, 0.22f), rimMat, legJoint.transform);
            CreatePrimitivePart(name + "_EnergyCore", PrimitiveType.Sphere, new Vector3(0.13f * sideSign, -0.15f, 0f), Vector3.zero, new Vector3(0.16f, 0.16f, 0.16f), magmaMat, legJoint.transform);

            // Heavy Iron Hoof
            CreatePrimitivePart(name + "_Hoof", PrimitiveType.Cube, new Vector3(0f, -0.68f, 0f), Vector3.zero, new Vector3(0.24f, 0.18f, 0.27f), hoofMat, legJoint.transform);
        }

        private static ParticleSystem CreateDustVFX(Transform parent)
        {
            GameObject dustGO = new GameObject("HoofDust_VFX");
            dustGO.transform.SetParent(parent, false);
            dustGO.transform.localPosition = new Vector3(0f, 0.05f, -0.4f);
            var ps = dustGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 1.2f;
            main.startSize = 0.35f;
            main.startColor = new Color(0.8f, 0.75f, 0.65f, 0.6f);
            main.loop = true;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;
            return ps;
        }

        private static ParticleSystem CreateAuraVFX(Transform parent)
        {
            GameObject auraGO = new GameObject("IronHorse_Aura_VFX");
            auraGO.transform.SetParent(parent, false);
            auraGO.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            var ps = auraGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 2.0f;
            main.startSize = 0.15f;
            main.startColor = new Color(1.0f, 0.65f, 0.15f, 0.9f);
            main.loop = true;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = 25f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.8f, 0.8f, 1.4f);
            return ps;
        }

        private static ParticleSystem CreateFireBreathVFX(Transform parent)
        {
            GameObject fireGO = new GameObject("FireBreath_VFX");
            fireGO.transform.SetParent(parent, false);
            fireGO.transform.localPosition = new Vector3(0f, 0.48f, 0.90f);
            fireGO.transform.localEulerAngles = new Vector3(10f, 0f, 0f);

            var ps = fireGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.55f;
            main.startSpeed = 4.2f;
            main.startSize = 0.40f;
            main.startColor = new Color(1.0f, 0.35f, 0.05f, 0.95f);
            main.loop = true;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;

            return ps;
        }
    }
}
