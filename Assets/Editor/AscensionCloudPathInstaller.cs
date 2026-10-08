using UnityEditor;

/// <summary>Legacy menu path now installs the runtime cloud trail.</summary>
public static class AscensionCloudPathInstaller
{
    [MenuItem("Thanh Giong/Install Ascension Cloud Stairway")]
    public static void Install() => AscensionCloudAnimationInstaller.Install();
}
