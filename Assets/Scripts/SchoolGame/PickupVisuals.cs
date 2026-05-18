using UnityEngine;

namespace SchoolGame
{
    // Construit un visuel de pickup propre : modèle Synty + halo lumineux au sol.
    public static class PickupVisuals
    {
        public static void Build(GameObject root, GameObject modelPrefab, float modelScale,
            Vector3 modelEuler, Color glowColor)
        {
            if (modelPrefab != null)
            {
                var model = Object.Instantiate(modelPrefab, root.transform);
                model.name = "Model";
                model.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                model.transform.localRotation = Quaternion.Euler(modelEuler);
                model.transform.localScale = Vector3.one * modelScale;

                foreach (var col in model.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
                foreach (var rb in model.GetComponentsInChildren<Rigidbody>())
                    Object.DestroyImmediate(rb);
            }

            // Halo au sol.
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            glow.name = "Glow";
            var glowCol = glow.GetComponent<Collider>();
            if (glowCol != null) Object.DestroyImmediate(glowCol);
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            glow.transform.localScale = new Vector3(0.7f, 0.012f, 0.7f);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = glowColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", glowColor * 2.5f);
            glow.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
