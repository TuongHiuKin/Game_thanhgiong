using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ThanhGiong.EditorTools
{
    public static class ThanhGiongPrefabBuilder
    {
        [MenuItem("Tools/Characters/Build KayKit Thanh Giong Hero Prefab")]
        public static void Build()
        {
            // 1. Load Base KayKit Character FBX (Official KayKit humanoid mesh & rig)
            GameObject knightFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Plugins/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Characters/fbx/Knight.fbx");
            if (knightFBX == null)
            {
                Debug.LogError("Knight.fbx not found!");
                return;
            }

            GameObject hero = Object.Instantiate(knightFBX);
            hero.name = "KayKit_ThanhGiong_Hero";

            // Load Materials
            Material giongMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_ThanhGiong_Hero.mat");
            Material matGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Horse/Mat_IronHorse_GoldCrest.mat");
            Material matBronze = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_Giong_Bronze.mat");
            Material matBamboo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_Giong_BambooGold.mat");
            Material matLeaf = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_Giong_BambooLeaf.mat");
            Material matEyeGlow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_Giong_RubyGem.mat");
            Material matHair = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ThanhGiong/Mat_Giong_Hair.mat");

            // Configure Skinned Mesh Renderers
            GameObject capeObj = null;
            foreach (var smr in hero.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.name.Contains("Helmet"))
                {
                    // Completely disable helmet and visor to reveal the handsome heroic face
                    smr.gameObject.SetActive(false);
                }
                else
                {
                    smr.sharedMaterial = giongMat;
                    if (smr.name == "Knight_Cape")
                    {
                        capeObj = smr.gameObject;
                        smr.gameObject.SetActive(true);
                    }
                }
            }

            // 2. Find Key Rig Bones
            Transform headBone = null;
            Transform handSlotR = null;
            Transform chestBone = null;

            foreach (Transform t in hero.GetComponentsInChildren<Transform>(true))
            {
                string lower = t.name.ToLower();
                if (lower == "head") headBone = t;
                else if (lower == "handslot.r") handSlotR = t;
                else if (lower == "chest") chestBone = t;
            }

            // 3. Attach Vietnamese Hero Hair & Dong Son Headband to Head Bone
            if (headBone != null)
            {
                // Topknot Bun (Búi tóc củ tỏi truyền thống)
                GameObject bun = CreatePart("Topknot_Bun", PrimitiveType.Sphere, new Vector3(0f, 1.05f, -0.06f), Vector3.zero, new Vector3(0.38f, 0.45f, 0.38f), matHair, headBone);

                // Golden Ring Tie at base of Bun
                CreatePart("Topknot_Ring", PrimitiveType.Cylinder, new Vector3(0f, 0.86f, -0.06f), Vector3.zero, new Vector3(0.35f, 0.05f, 0.35f), matGold, headBone);

                // Dong Son Headband (Khăn xếp / Băng đô Đông Sơn)
                CreatePart("DongSon_Headband", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0.02f), Vector3.zero, new Vector3(0.88f, 0.045f, 0.88f), matGold, headBone);

                // Sacred Dong Son Sun Emblem on Forehead
                GameObject emblem = CreatePart("DongSon_Sun_Emblem", PrimitiveType.Cylinder, new Vector3(0f, 0.62f, 0.45f), new Vector3(90f, 0f, 0f), new Vector3(0.18f, 0.025f, 0.18f), matGold, headBone);
                // Central Ruby Boss on Emblem
                CreatePart("Emblem_Gem", PrimitiveType.Sphere, new Vector3(0f, 0.02f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 0.45f), matEyeGlow, emblem.transform);
            }

            // 4. Attach Stylized Golden Dragon Pauldrons to Shoulders (Chest Bone)
            if (chestBone != null)
            {
                BuildDragonPauldron("Dragon_Pauldron_L", chestBone, true, matGold, matBronze, matEyeGlow);
                BuildDragonPauldron("Dragon_Pauldron_R", chestBone, false, matGold, matBronze, matEyeGlow);
            }

            // 5. Attach The Legendary Golden Bamboo Weapon to Hand Slot
            GameObject bambooWeapon = null;
            ParticleSystem bambooAura = null;

            if (handSlotR != null)
            {
                bambooWeapon = new GameObject("Weapon_GoldenBamboo");
                bambooWeapon.transform.SetParent(handSlotR, false);
                bambooWeapon.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                bambooWeapon.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

                // Main Central Bamboo Stalk
                CreateBambooStalk("Stalk_Main", new Vector3(0f, 0.40f, 0f), Vector3.zero, new Vector3(0.048f, 0.62f, 0.048f), matBamboo, matLeaf, bambooWeapon.transform);
                // Side Left Bamboo Stalk
                CreateBambooStalk("Stalk_Left", new Vector3(-0.045f, 0.35f, 0.03f), new Vector3(3f, 0f, -4f), new Vector3(0.042f, 0.54f, 0.042f), matBamboo, matLeaf, bambooWeapon.transform);
                // Side Right Bamboo Stalk
                CreateBambooStalk("Stalk_Right", new Vector3(0.045f, 0.37f, -0.03f), new Vector3(-3f, 0f, 4f), new Vector3(0.044f, 0.56f, 0.044f), matBamboo, matLeaf, bambooWeapon.transform);

                // Radiant Aura VFX
                GameObject auraGO = new GameObject("BambooAura_VFX");
                auraGO.transform.SetParent(bambooWeapon.transform, false);
                auraGO.transform.localPosition = new Vector3(0f, 0.50f, 0f);
                bambooAura = auraGO.AddComponent<ParticleSystem>();
                var main = bambooAura.main;
                main.startLifetime = 0.55f;
                main.startSpeed = 0.45f;
                main.startSize = 0.12f;
                main.startColor = new Color(1.0f, 0.90f, 0.30f, 0.95f);
                main.loop = true;
                main.playOnAwake = true;
                var em = bambooAura.emission;
                em.rateOverTime = 30f;
                var sh = bambooAura.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 15f;
                sh.radius = 0.14f;
            }

            // 6. Impact Ground Slam VFX
            GameObject vfxRoot = new GameObject("VFX_Effects");
            vfxRoot.transform.SetParent(hero.transform, false);

            GameObject slamGO = new GameObject("BambooSlam_VFX");
            slamGO.transform.SetParent(vfxRoot.transform, false);
            slamGO.transform.localPosition = new Vector3(0f, 0.05f, 0.85f);
            var slamPS = slamGO.AddComponent<ParticleSystem>();
            var slamMain = slamPS.main;
            slamMain.startLifetime = 0.65f;
            slamMain.startSpeed = 4.5f;
            slamMain.startSize = 0.35f;
            slamMain.startColor = new Color(1.0f, 0.85f, 0.25f, 0.95f);
            slamMain.loop = false;
            slamMain.playOnAwake = false;
            var slamEm = slamPS.emission;
            slamEm.rateOverTime = 0f;
            slamEm.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
            var slamSh = slamPS.shape;
            slamSh.shapeType = ParticleSystemShapeType.Circle;
            slamSh.radius = 0.6f;

            // 7. Animator & Controller Setup
            var anim = hero.GetComponent<Animator>();
            if (anim == null) anim = hero.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/ThanhGiongKnight.controller");
            if (controller != null) anim.runtimeAnimatorController = controller;

            // Box Collider
            var col = hero.GetComponent<BoxCollider>();
            if (col == null) col = hero.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.75f, 0f);
            col.size = new Vector3(0.6f, 1.5f, 0.5f);

            // 8. Hero Controller Script
            var heroCtrl = hero.AddComponent<ThanhGiong.Characters.ThanhGiongCharacterController>();
            SerializedObject so = new SerializedObject(heroCtrl);
            if (bambooWeapon != null) so.FindProperty("goldenBambooWeapon").objectReferenceValue = bambooWeapon;
            if (capeObj != null) so.FindProperty("capeGroup").objectReferenceValue = capeObj;
            if (bambooAura != null) so.FindProperty("bambooAuraVFX").objectReferenceValue = bambooAura;
            so.FindProperty("bambooImpactVFX").objectReferenceValue = slamPS;
            so.FindProperty("hasCape").boolValue = true;
            so.FindProperty("hasWeapon").boolValue = true;
            so.ApplyModifiedProperties();

            // 9. Save Prefab
            string prefabPath = "Assets/Prefabs/Player/KayKit_ThanhGiong_Hero.prefab";
            PrefabUtility.SaveAsPrefabAsset(hero, prefabPath);
            Object.DestroyImmediate(hero);

            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[ThanhGiongPrefabBuilder] Successfully built redesigned KayKit Thánh Gióng: " + prefabPath + "</color>");
        }

        private static void BuildDragonPauldron(string name, Transform chestParent, bool isLeft, Material matGold, Material matBronze, Material matGlow)
        {
            float sideSign = isLeft ? -1f : 1f;

            GameObject pauldronRoot = new GameObject(name);
            pauldronRoot.transform.SetParent(chestParent, false);
            pauldronRoot.transform.localPosition = new Vector3(0.32f * sideSign, 0.18f, 0.02f);
            pauldronRoot.transform.localRotation = Quaternion.Euler(0f, 25f * sideSign, -12f * sideSign);

            // Main Curved Mantle (vai giáp bảo vệ)
            GameObject mantle = CreatePart("Mantle", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.25f, 0.10f, 0.22f), matGold, pauldronRoot.transform);

            // Dragon Face / Snout
            GameObject dragonHead = CreatePart("Dragon_Head", PrimitiveType.Cube, new Vector3(0.04f * sideSign, 0.02f, 0.10f), new Vector3(15f, 10f * sideSign, 0f), new Vector3(0.18f, 0.14f, 0.16f), matGold, pauldronRoot.transform);

            // Dragon Crest / Horns
            CreatePart("Dragon_Horns", PrimitiveType.Cube, new Vector3(0f, 0.08f, -0.04f), new Vector3(25f, 0f, 0f), new Vector3(0.15f, 0.08f, 0.14f), matBronze, dragonHead.transform);

            // Dragon Eyes (glowing ruby jewels)
            CreatePart("Dragon_Eye_L", PrimitiveType.Sphere, new Vector3(-0.06f, 0.04f, 0.08f), Vector3.zero, new Vector3(0.045f, 0.045f, 0.045f), matGlow, dragonHead.transform);
            CreatePart("Dragon_Eye_R", PrimitiveType.Sphere, new Vector3(0.06f, 0.04f, 0.08f), Vector3.zero, new Vector3(0.045f, 0.045f, 0.045f), matGlow, dragonHead.transform);
        }

        private static void CreateBambooStalk(string name, Vector3 pos, Vector3 rot, Vector3 scale, Material bambooMat, Material leafMat, Transform parent)
        {
            GameObject stalk = new GameObject(name);
            stalk.transform.SetParent(parent, false);
            stalk.transform.localPosition = pos;
            stalk.transform.localRotation = Quaternion.Euler(rot);

            // Main cylinder stalk
            CreatePart("Cylinder", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, scale, bambooMat, stalk.transform);

            // Bamboo Nodes (vòng đốt tre)
            float halfH = scale.y * 0.75f;
            CreatePart("Node_1", PrimitiveType.Cylinder, new Vector3(0f, -halfH * 0.5f, 0f), Vector3.zero, new Vector3(scale.x * 1.35f, 0.018f, scale.z * 1.35f), bambooMat, stalk.transform);
            CreatePart("Node_2", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), Vector3.zero, new Vector3(scale.x * 1.35f, 0.018f, scale.z * 1.35f), bambooMat, stalk.transform);
            CreatePart("Node_3", PrimitiveType.Cylinder, new Vector3(0f, halfH * 0.5f, 0f), Vector3.zero, new Vector3(scale.x * 1.35f, 0.018f, scale.z * 1.35f), bambooMat, stalk.transform);

            // Golden Bamboo Leaves (lá tre dát vàng)
            CreatePart("Leaf_Upper_A", PrimitiveType.Cube, new Vector3(0.08f, halfH * 0.85f, 0f), new Vector3(0f, 0f, 38f), new Vector3(0.12f, 0.012f, 0.045f), leafMat, stalk.transform);
            CreatePart("Leaf_Upper_B", PrimitiveType.Cube, new Vector3(-0.08f, halfH * 0.90f, 0.02f), new Vector3(0f, 0f, -38f), new Vector3(0.12f, 0.012f, 0.045f), leafMat, stalk.transform);
            CreatePart("Leaf_Mid", PrimitiveType.Cube, new Vector3(0.07f, 0.05f, 0.02f), new Vector3(10f, 20f, 40f), new Vector3(0.10f, 0.012f, 0.04f), leafMat, stalk.transform);
        }

        private static GameObject CreatePart(string name, PrimitiveType type, Vector3 pos, Vector3 rot, Vector3 scale, Material mat, Transform parent)
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
    }
}
