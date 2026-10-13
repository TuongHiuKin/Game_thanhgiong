using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class ThanhGiongArenaSkinTests
{
    [Test] public void ArenaSkinKeepsRightHandAndRemovesOldSpearAndFloatingLeftFragment()
    {
        var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_HorseSkin.asset");
        var arena = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/ThanhGiong_Mounted_ArenaSkin.asset");
        Assert.NotNull(source); Assert.NotNull(arena);
        Assert.AreEqual(source.vertexCount, arena.vertexCount, "Skin indices and original rig weights must remain stable.");
        var parents = Enumerable.Range(0, source.vertexCount).ToArray();
        int Find(int i) => parents[i] == i ? i : (parents[i] = Find(parents[i]));
        foreach (int s in Enumerable.Range(0, source.subMeshCount)) {
            int[] triangles = source.GetTriangles(s);
            for (int i = 0; i < triangles.Length; i += 3) {
                parents[Find(triangles[i])] = Find(triangles[i + 1]);
                parents[Find(triangles[i + 1])] = Find(triangles[i + 2]);
            }
        }
        var kept = new HashSet<int>(arena.triangles);
        // Landmarks identified on this source mesh: palm, fingers, old shaft,
        // spear blade and the isolated left-side fragment from the rearing pose.
        foreach (int landmark in new[] { 1284, 1341, 26819, 26837 }) {
            int root = Find(landmark);
            int[] vertices = Enumerable.Range(0, source.vertexCount).Where(i => Find(i) == root).ToArray();
            Assert.Greater(vertices.Length, 10, "Hand landmark must be a substantial connected component.");
            Assert.IsTrue(vertices.All(kept.Contains), "Right gauntlet/fingers were cut with the old spear: " + landmark);
        }
        // Landmark 2564 joins the palm to the old lower shaft. They cannot be
        // removed as one component; verify the visible palm and lower shaft separately.
        int palmRoot = Find(2564);
        int[] palm = Enumerable.Range(0, source.vertexCount).Where(i => Find(i) == palmRoot && kept.Contains(i)).ToArray();
        Assert.GreaterOrEqual(palm.Length, 200, "The palm must include its surface, not just the detached finger groups.");
        Assert.IsTrue(kept.Contains(2042), "The visible palm face was removed along with the lower spear shaft.");
        Assert.IsFalse(Enumerable.Range(0, source.vertexCount).Any(i => Find(i) == palmRoot && source.vertices[i].y < 1.10f && kept.Contains(i)), "Lower old shaft survived beneath the hand.");
        Assert.IsFalse(Enumerable.Range(0, source.vertexCount).Any(i => Find(i) == Find(2041) && source.vertices[i].y > 1.235f && kept.Contains(i)), "Upper old shaft survived above the hand.");
        foreach (int landmark in new[] { 133, 1465, 1607, 1693, 1752, 1817, 1896, 27028, 27108, 6850 }) {
            int root = Find(landmark);
            Assert.IsFalse(Enumerable.Range(0, source.vertexCount).Any(i => Find(i) == root && kept.Contains(i)), "Unwanted spear/left fragment survived: " + landmark);
        }
        var handRoots = new HashSet<int>(new[] { 1284, 1341, 26819, 26837 }.Select(Find));
        int[] handVertices = Enumerable.Range(0, source.vertexCount).Where(i => handRoots.Contains(Find(i))).ToArray();
        var gripBounds = new Bounds(source.vertices[handVertices[0]], Vector3.zero);
        foreach (int i in handVertices) gripBounds.Encapsulate(source.vertices[i]);
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ThanhGiongArena/PhuDongArena.unity");
        try {
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var skin = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SkinnedMeshRenderer>(true)).Single(r => r.name == "Horse_AnimatedMesh");
            foreach (string name in new[] { "Weapon_IronSword", "Weapon_GoldenBamboo" }) {
                var weapon = transforms.Single(t => t.name == name);
                Vector3 center = skin.transform.InverseTransformPoint(weapon.position);
                Vector3 axis = skin.transform.InverseTransformDirection(weapon.up).normalized;
                Assert.IsTrue(gripBounds.IntersectRay(new Ray(center - axis * 3, axis)), name + " must pass through the original closed fist.");
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
