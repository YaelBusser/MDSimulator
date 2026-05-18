using UnityEngine;

namespace SchoolGame
{
    // Trousse de secours : rend de la vie au joueur (système Invector).
    public class HealthPickup : MonoBehaviour
    {
        public float healAmount = 40f;

        [Header("Modèle (prefab Synty)")]
        public GameObject modelPrefab;
        public float modelScale = 1f;
        public Vector3 modelEuler = Vector3.zero;

        [Header("Animation")]
        public float spinSpeed = 60f;
        public float bobAmplitude = 0.15f;
        public float bobSpeed = 2f;

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
                    new Color(0.3f, 1f, 0.4f));

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

            var gm = GameManager.Instance;
            if (gm == null || !gm.CanHealPlayer()) return;

            gm.HealPlayer(healAmount);
            gm.ShowMessage($"+{Mathf.RoundToInt(healAmount)} PV");
            Destroy(gameObject);
        }
    }
}
