using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ThanhGiongStoryBeatZone : MonoBehaviour
{
    public string beatTitle;
    [TextArea] public string storyPurpose;
    public IsometricCameraFollow.Shot cameraShot = IsometricCameraFollow.Shot.Explore;

    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(8f, 4f, 8f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") && other.GetComponentInParent<MountedHorseController>() == null) return;
        IsometricCameraFollow.Instance?.SetShot(cameraShot);
    }
}
