using UnityEngine;

// Authored scene positions are registered only after the checkpoint host is initialized.
public sealed class LegendCheckpointBeacon : MonoBehaviour
{
    public string checkpointName = "Đường hành quân";
    void Start()
    {
        var checkpoint = FindFirstObjectByType<LegendCheckpoint>();
        if (checkpoint != null) checkpoint.AddBeacon(transform.position, checkpointName);
    }
}
