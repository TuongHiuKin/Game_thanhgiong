using System.IO;
using UnityEditor;
using UnityEngine;

public static class ThanhGiongTmpResourceImporter
{
    [MenuItem("Tools/Thanh Giong/Import TMP Essential Resources")]
    public static void ImportTmpEssentialResources()
    {
        string project = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string packageRoot = Path.Combine(project, "Library", "PackageCache");
        string package = FindPackage(packageRoot);
        if (string.IsNullOrEmpty(package))
        {
            package = Path.Combine(EditorApplication.applicationContentsPath, "Resources", "PackageManager", "BuiltInPackages", "com.unity.ugui", "Package Resources", "TMP Essential Resources.unitypackage");
        }

        if (!File.Exists(package))
        {
            Debug.LogError("TMP Essential Resources package was not found.");
            return;
        }

        AssetDatabase.ImportPackage(package, false);
        AssetDatabase.Refresh();
        Debug.Log("TMP Essential Resources imported from " + package);
    }

    static string FindPackage(string packageRoot)
    {
        if (!Directory.Exists(packageRoot))
        {
            return null;
        }

        foreach (string file in Directory.EnumerateFiles(packageRoot, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories))
        {
            return file;
        }

        return null;
    }
}
