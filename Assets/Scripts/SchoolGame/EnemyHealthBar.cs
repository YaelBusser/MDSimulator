using UnityEngine;
using UnityEngine.UI;

namespace SchoolGame
{
    // Barre de vie en world-space au-dessus de la tête de l'ennemi (boss).
    [RequireComponent(typeof(Health))]
    public class EnemyHealthBar : MonoBehaviour
    {
        public float heightOffset = 2.2f;
        public float width = 1.3f;
        public float thickness = 0.16f;

        private Health health;
        private Transform barRoot;
        private Image fill;
        private Camera cam;

        private void Start()
        {
            health = GetComponent<Health>();
            cam = Camera.main;
            if (cam == null)
            {
                var c = FindFirstObjectByType<Camera>();
                cam = c;
            }

            var sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

            var canvasGo = new GameObject("HealthBar");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, heightOffset, 0f);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            var crt = canvas.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(width, thickness);
            canvasGo.transform.localScale = Vector3.one;
            barRoot = canvasGo.transform;

            var bg = new GameObject("BG").AddComponent<Image>();
            bg.transform.SetParent(barRoot, false);
            bg.sprite = sprite;
            bg.color = new Color(0f, 0f, 0f, 0.7f);
            Stretch(bg.rectTransform);

            fill = new GameObject("Fill").AddComponent<Image>();
            fill.transform.SetParent(barRoot, false);
            fill.sprite = sprite;
            fill.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            var fr = fill.rectTransform;
            Stretch(fr);
            fr.offsetMin = new Vector2(0.02f, 0.02f);
            fr.offsetMax = new Vector2(-0.02f, -0.02f);
        }

        private void LateUpdate()
        {
            if (health == null || barRoot == null) return;

            if (health.IsDead)
            {
                barRoot.gameObject.SetActive(false);
                return;
            }

            float ratio = Mathf.Clamp01(health.Current / health.Max);
            fill.fillAmount = ratio;
            fill.color = Color.Lerp(new Color(0.85f, 0.15f, 0.15f),
                new Color(0.3f, 0.85f, 0.3f), ratio);

            if (cam != null)
            {
                barRoot.rotation = Quaternion.LookRotation(
                    barRoot.position - cam.transform.position);
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
