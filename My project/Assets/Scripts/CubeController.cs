using UnityEngine;

public class CubeController : MonoBehaviour
{
    private Renderer cubeRenderer;
    private Color[] colors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow
    };
    private int currentColorIndex = 0;

    void Start()
    {
        // Get the renderer component
        cubeRenderer = GetComponent<Renderer>();
        // Set initial color
        cubeRenderer.material.color = colors[currentColorIndex];
    }

    void OnMouseDown()
    {
        // Change to next color when clicked
        currentColorIndex = (currentColorIndex + 1) % colors.Length;
        cubeRenderer.material.color = colors[currentColorIndex];
    }
}
