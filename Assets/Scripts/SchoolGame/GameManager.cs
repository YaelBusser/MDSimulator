using System.Collections.Generic;
using Invector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SchoolGame
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Transform PlayerTransform { get; private set; }
        public PlayerCombat PlayerCombat { get; private set; }

        private vHealthController invectorHealth;

        public bool IsPlaying { get; private set; } = true;
        public bool Won { get; private set; }

        [Tooltip("Vie rendue au joueur à chaque ennemi tué (vol de vie)")]
        public float healPerKill = 20f;

        private readonly List<EnemySpawner> spawners = new List<EnemySpawner>();
        private int totalEnemies;
        private int killedEnemies;

        private float messageUntil;

        private Text hudText;
        private Text messageText;
        private GameObject endPanel;
        private Text endText;
        private Image damageFlash;
        private float damageFlashAmount;

        private float levelStartTime;
        private bool restarting;
        private vGameController invGC;

        private void Awake()
        {
            levelStartTime = Time.time;
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            ResolvePlayer();
            NeutralizeInvectorGameController();
            BuildUI();
        }

        // Invector vGameController (dontDestroyOnLoad) recharge la scène tout seul
        // à la mort via Invoke("ResetScene", 4s). Comme il survit à nos reloads,
        // son Invoke en attente provoquait un 2e restart fantôme. On le désarme.
        private void NeutralizeInvectorGameController()
        {
            invGC = vGameController.instance;
            if (invGC == null) invGC = FindFirstObjectByType<vGameController>();
            if (invGC != null)
            {
                invGC.CancelInvoke();
                invGC.StopAllCoroutines();
                invGC.enabled = false;
            }
        }

        private void ResolvePlayer()
        {
            var combat = FindFirstObjectByType<PlayerCombat>();
            GameObject playerGo = combat != null ? combat.gameObject : null;

            if (playerGo == null)
            {
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (mb.GetType().Name.Contains("vThirdPersonController"))
                    {
                        playerGo = mb.gameObject;
                        break;
                    }
                }
            }

            if (playerGo == null)
            {
                foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                {
                    if (go.GetComponent<Collider>() != null) { playerGo = go; break; }
                }
            }

            if (playerGo == null) return;

            PlayerTransform = playerGo.transform;
            PlayerCombat = playerGo.GetComponent<PlayerCombat>();

            // On utilise le système de vie d'Invector (et sa barre à l'écran).
            invectorHealth = playerGo.GetComponentInChildren<vHealthController>();
            if (invectorHealth != null)
                invectorHealth.onDead.AddListener(_ => GameOver(false));
        }

        // Inflige des dégâts au joueur via le système Invector.
        public void DamagePlayer(float amount)
        {
            if (invectorHealth == null || invectorHealth.isDead || amount <= 0f) return;
            invectorHealth.TakeDamage(new vDamage(Mathf.CeilToInt(amount)));
            damageFlashAmount = 0.45f;
        }

        public bool CanHealPlayer()
        {
            return invectorHealth != null && !invectorHealth.isDead
                && invectorHealth.currentHealth < invectorHealth.maxHealth;
        }

        public void HealPlayer(float amount)
        {
            if (invectorHealth == null || invectorHealth.isDead || amount <= 0f) return;
            invectorHealth.AddHealth(Mathf.CeilToInt(amount));
        }

        public void RegisterSpawner(EnemySpawner s)
        {
            if (s == null || spawners.Contains(s)) return;
            spawners.Add(s);
            totalEnemies += s.TotalToSpawn;
        }

        public void NotifyEnemySpawned() { }

        public void NotifyEnemyKilled()
        {
            killedEnemies++;
            if (CanHealPlayer()) HealPlayer(healPerKill); // vol de vie
            if (IsPlaying && totalEnemies > 0 && killedEnemies >= totalEnemies)
                GameOver(true);
        }

        public void GameOver(bool won)
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            Won = won;

            if (endPanel != null)
            {
                endPanel.SetActive(true);
                endText.text = (won ? "<color=#5BE36A>VICTOIRE !</color>" : "<color=#E35B5B>PERDU...</color>")
                    + $"\n\nEnnemis vaincus : {killedEnemies} / {totalEnemies}"
                    + "\n\n[ R ]  Recommencer";
            }
        }

        public void ShowMessage(string text, float duration = 2.5f)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageText.gameObject.SetActive(true);
            messageUntil = Time.time + duration;
        }

        private void Update()
        {
            if (IsPlaying)
            {
                if (hudText != null)
                {
                    string weapon = PlayerCombat != null ? PlayerCombat.CurrentWeaponName : "—";
                    hudText.text =
                        $"<b>Arme</b>  {weapon}\n" +
                        $"<b>Ennemis</b>  {killedEnemies} / {totalEnemies}\n" +
                        $"<size=20><color=#9CE0A8>Tue les ennemis pour récupérer de la vie</color></size>";
                }

                if (messageText != null && messageText.gameObject.activeSelf && Time.time > messageUntil)
                    messageText.gameObject.SetActive(false);
            }

            // Tant qu'on n'a pas relancé via R, on empêche Invector de
            // recharger la scène de son côté.
            if (!IsPlaying && invGC != null)
                invGC.CancelInvoke();

            // Restart : seulement sur l'écran de fin, jamais juste après un
            // (re)chargement (sinon le R encore enfoncé relance une 2e fois),
            // et une seule fois.
            if (!IsPlaying && !restarting
                && Time.time - levelStartTime > 0.5f
                && Input.GetKeyDown(KeyCode.R))
            {
                restarting = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (damageFlash != null)
            {
                damageFlashAmount = Mathf.Max(0f, damageFlashAmount - Time.deltaTime * 1.2f);
                var c = damageFlash.color;
                c.a = damageFlashAmount;
                damageFlash.color = c;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- Construction de l'UI au runtime (zéro câblage manuel) ----------

        private void BuildUI()
        {
            var canvasGo = new GameObject("SchoolGame_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Flash rouge plein écran quand on prend des dégâts.
            damageFlash = CreatePanel(canvasGo.transform, "DamageFlash",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.7f, 0f, 0f, 0f));
            var dfr = damageFlash.rectTransform;
            dfr.anchorMin = Vector2.zero; dfr.anchorMax = Vector2.one;
            dfr.offsetMin = Vector2.zero; dfr.offsetMax = Vector2.zero;
            damageFlash.raycastTarget = false;

            // HUD en bas à gauche (loin de la barre d'endurance Invector en haut à gauche).
            var hudBg = CreatePanel(canvasGo.transform, "HUD",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f),
                new Vector2(600f, 150f), new Color(0f, 0f, 0f, 0.55f));
            hudText = CreateText(hudBg.transform, "HudText", 28, TextAnchor.MiddleLeft);
            StretchWithPadding(hudText.rectTransform, 18f);

            // Message temporaire en haut au centre.
            var msgGo = new GameObject("Message");
            msgGo.transform.SetParent(canvasGo.transform, false);
            messageText = msgGo.AddComponent<Text>();
            SetupTextComponent(messageText, 34, TextAnchor.MiddleCenter);
            messageText.color = new Color(1f, 0.85f, 0.2f);
            var mrt = messageText.rectTransform;
            mrt.anchorMin = new Vector2(0.5f, 1f);
            mrt.anchorMax = new Vector2(0.5f, 1f);
            mrt.pivot = new Vector2(0.5f, 1f);
            mrt.anchoredPosition = new Vector2(0f, -40f);
            mrt.sizeDelta = new Vector2(900f, 60f);
            msgGo.SetActive(false);

            // Panneau de fin (victoire / défaite) plein écran centré.
            endPanel = CreatePanel(canvasGo.transform, "EndPanel",
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero,
                Vector2.zero, new Color(0f, 0f, 0f, 0.75f)).gameObject;
            var ert = endPanel.GetComponent<RectTransform>();
            ert.anchorMin = Vector2.zero; ert.anchorMax = Vector2.one;
            ert.offsetMin = Vector2.zero; ert.offsetMax = Vector2.zero;
            endText = CreateText(endPanel.transform, "EndText", 48, TextAnchor.MiddleCenter);
            var etr = endText.rectTransform;
            etr.anchorMin = new Vector2(0.5f, 0.5f);
            etr.anchorMax = new Vector2(0.5f, 0.5f);
            etr.pivot = new Vector2(0.5f, 0.5f);
            etr.anchoredPosition = Vector2.zero;
            etr.sizeDelta = new Vector2(1000f, 500f);
            endPanel.SetActive(false);
        }

        private Image CreatePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(aMin.x, aMin.y);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return img;
        }

        private Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            SetupTextComponent(t, fontSize, anchor);
            return t;
        }

        private void SetupTextComponent(Text t, int fontSize, TextAnchor anchor)
        {
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
        }

        private void StretchWithPadding(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
        }
    }
}
