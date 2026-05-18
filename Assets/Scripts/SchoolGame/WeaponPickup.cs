using UnityEngine;

namespace SchoolGame
{
    // Item à ramasser : à placer dans une pièce. Donne une nouvelle arme au joueur.
    public class WeaponPickup : MonoBehaviour
    {
        public WeaponData weapon = new WeaponData { weaponName = "Balai", damage = 16f };

        [Header("Modèle (prefab Synty)")]
        public GameObject modelPrefab;
        public float modelScale = 1f;
        public Vector3 modelEuler = new Vector3(0f, 0f, 35f);

        [Header("Animation")]
        public float spinSpeed = 80f;
        public float bobAmplitude = 0.18f;
        public float bobSpeed = 2.2f;

        private Transform modelRoot;
        private float modelBaseY;

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 1.4f;
                sphere.isTrigger = true;
            }
            else col.isTrigger = true;

            if (transform.Find("Model") == null && transform.Find("Glow") == null)
                PickupVisuals.Build(gameObject, modelPrefab, modelScale, modelEuler,
                    new Color(1f, 0.85f, 0.2f));

            modelRoot = transform.Find("Model");
            if (modelRoot != null) modelBaseY = modelRoot.localPosition.y;
        }

        private void Update()
        {
            if (modelRoot == null) return;

            modelRoot.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            Vector3 lp = modelRoot.localPosition;
            lp.y = modelBaseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            modelRoot.localPosition = lp;
        }

        private void OnTriggerEnter(Collider other)
        {
            var combat = other.GetComponentInParent<PlayerCombat>();
            if (combat == null) return;

            combat.EquipWeapon(weapon);
            Destroy(gameObject);
        }
    }
}
