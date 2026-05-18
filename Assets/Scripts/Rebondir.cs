using UnityEngine;

public class Rebondir : MonoBehaviour
{
    private Renderer sphereRenderer;

    void Start()
    {
        sphereRenderer = GetComponent<Renderer>();
    }

    void OnCollisionEnter(Collision collision) // Détectte une collision
    {
        sphereRenderer.material.color = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f);
    }
}
