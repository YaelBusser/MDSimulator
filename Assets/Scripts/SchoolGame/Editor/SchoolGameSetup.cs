using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SchoolGame.EditorTools
{
    // Outils de câblage automatique : menu "Tools > School Game".
    public static class SchoolGameSetup
    {
        private const string ControllerPath = "Assets/Scripts/SchoolGame/EnemyAnimator.controller";

        private const string FbxFreeMove =
            "Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Animations/Basic_FreeMovement.fbx";
        private const string FbxActions =
            "Assets/Invector-3rdPersonController/Basic Locomotion/3D Models/Animations/Basic_Actions.fbx";
        private const string FbxMelee =
            "Assets/Invector-3rdPersonController/Melee Combat/3DModels/Animations/Melee_CombatSet.fbx";

        private static readonly string[] DefaultEnemyPrefabs =
        {
            "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Boss_Male_01.prefab",
            "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Business_Male_01.prefab",
            "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Security_Male_01.prefab",
            "Assets/Synty/PolygonOffice/Prefabs/Characters/SM_Chr_Cleaner_Male_01.prefab",
        };

        private const string PropBroom =
            "Assets/Synty/PolygonOffice/Prefabs/Props/Misc/SM_Prop_Broom_01.prefab";
        private const string PropGolf =
            "Assets/Synty/PolygonOffice/Prefabs/Props/Misc/SM_Prop_Minigolf_Club_01.prefab";
        private const string PropExtinguisher =
            "Assets/Synty/PolygonOffice/Prefabs/Props/Wall Props/SM_Prop_Fire_Extinguisher_01.prefab";
        private const string PropMedkit =
            "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Medkit_01.prefab";

        // Vrais zombies (PolygonBossZombies). Humanoid + Animator/Avatar déjà présents.
        private const string ZombieBig =
            "Assets/PolygonBossZombies/Prefabs/SM_Chr_ZombieBoss_Brute_01.prefab";
        private const string ZombieSmallA =
            "Assets/PolygonBossZombies/Prefabs/SM_Chr_ZombieBoss_Wretch_01.prefab";
        private const string ZombieSmallB =
            "Assets/PolygonBossZombies/Prefabs/SM_Chr_ZombieBoss_Slobber_01.prefab";
        private const string ZombieSmallC =
            "Assets/PolygonBossZombies/Prefabs/SM_Chr_ZombieBoss_Blobber_01.prefab";
        private const string PropRebar =
            "Assets/PolygonBossZombies/Prefabs/Props/SM_Prop_RebarClub_01.prefab";

        // ---------------------------------------------------------------- Setup

        [MenuItem("Tools/School Game/1. Configurer Joueur + Manager")]
        public static void SetupPlayerAndManager()
        {
            GameObject player = FindPlayer();
            if (player == null)
            {
                EditorUtility.DisplayDialog("School Game",
                    "Joueur introuvable.\n\nOuvre la scène Demo avec ton personnage Invector puis relance.",
                    "OK");
                return;
            }

            // Nettoyage : on n'utilise plus de vie custom sur le joueur,
            // c'est la vie/barre d'Invector qui sert.
            var leftover = player.GetComponent<Health>();
            if (leftover != null) Object.DestroyImmediate(leftover);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);

            if (player.GetComponent<PlayerCombat>() == null) player.AddComponent<PlayerCombat>();

            var invHealth = player.GetComponentInChildren<Invector.vHealthController>();
            if (invHealth == null)
            {
                EditorUtility.DisplayDialog("School Game",
                    "Attention : aucun système de vie Invector (vHealthController) trouvé sur le joueur.\n" +
                    "Les ennemis ne pourront pas blesser le joueur. Vérifie que ton perso utilise bien le contrôleur Invector.",
                    "Compris");
            }

            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                var gm = new GameObject("_SchoolGame_Manager");
                gm.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
            }

            EnsureEnemyController();
            MarkSceneDirty();
            EditorUtility.DisplayDialog("School Game",
                $"OK : joueur configuré -> {player.name}\n\n" +
                "Maintenant utilise :\n" +
                "- Pièce 1 (facile)\n- Pièce 2 (moyen)\n- Pièce 3 (boss)\n" +
                "pour créer chaque salle (ennemis + arme).",
                "Super");
            Selection.activeGameObject = player;
        }

        // ---------------------------------------------------------------- Rooms

        [MenuItem("Tools/School Game/Pièce 1 - Facile (devant la vue)")]
        public static void CreateRoom1() => CreateRoom(1);

        [MenuItem("Tools/School Game/Pièce 2 - Moyen (devant la vue)")]
        public static void CreateRoom2() => CreateRoom(2);

        [MenuItem("Tools/School Game/Pièce 3 - Boss (devant la vue)")]
        public static void CreateRoom3() => CreateRoom(3);

        private static void CreateRoom(int tier)
        {
            Vector3 center = SceneViewPoint();
            var spawners = new System.Collections.Generic.List<GameObject>();

            string weaponName, weaponModel;
            float weaponDmg, weaponRange, healAmount;
            Vector3 euler = new Vector3(0f, 0f, 35f);

            if (tier == 1)
            {
                var s = MakeSpawner($"Spawner_Piece1", center, DefaultEnemyPrefabs, null);
                s.totalToSpawn = 4; s.maxAlive = 2; s.spawnInterval = 2.5f;
                s.enemyHealth = 35; s.enemyDamage = 6; s.enemyMoveSpeed = 2.3f;
                s.enemyAttackInterval = 1.6f; s.activationDistance = 12f;
                spawners.Add(s.gameObject);
                weaponName = "Balai"; weaponModel = PropBroom;
                weaponDmg = 16f; weaponRange = 2.7f; healAmount = 35f;
            }
            else if (tier == 2)
            {
                var s = MakeSpawner($"Spawner_Piece2", center, DefaultEnemyPrefabs, null);
                s.totalToSpawn = 6; s.maxAlive = 3; s.spawnInterval = 2f;
                s.enemyHealth = 60; s.enemyDamage = 10; s.enemyMoveSpeed = 2.8f;
                s.enemyAttackInterval = 1.4f; s.activationDistance = 13f;
                spawners.Add(s.gameObject);
                weaponName = "Club de golf"; weaponModel = PropGolf;
                weaponDmg = 32f; weaponRange = 2.5f; healAmount = 45f;
            }
            else
            {
                // Pièce 3 : 1 gros zombie (le gros de la puissance) + 3 petits zombies.
                // Budget vie ~= ancien niveau 3 (8 x 110 = 880).
                var boss = MakeSpawner("Spawner_Piece3_Boss", center,
                    new[] { ZombieBig }, null);
                boss.totalToSpawn = 1; boss.maxAlive = 1; boss.spawnInterval = 1f;
                boss.enemyHealth = 420; boss.enemyDamage = 36; boss.enemyMoveSpeed = 2.4f;
                boss.enemyAttackInterval = 1.7f; boss.enemyAttackRange = 2.6f;
                boss.activationDistance = 16f; boss.enemyScale = 0.85f;
                boss.showHealthBar = true;
                spawners.Add(boss.gameObject);

                var mobs = MakeSpawner("Spawner_Piece3_Minions", center,
                    new[] { ZombieSmallA, ZombieSmallB, ZombieSmallC }, null);
                mobs.totalToSpawn = 3; mobs.maxAlive = 3; mobs.spawnInterval = 1.5f;
                mobs.enemyHealth = 90; mobs.enemyDamage = 8; mobs.enemyMoveSpeed = 3.4f;
                mobs.enemyAttackInterval = 1.1f; mobs.activationDistance = 16f;
                mobs.enemyScale = 0.45f;
                spawners.Add(mobs.gameObject);

                weaponName = "Barre de fer"; weaponModel = PropRebar;
                weaponDmg = 55f; weaponRange = 2.4f; euler = new Vector3(0f, 0f, 40f);
                healAmount = 60f;
            }

            var weapon = CreateWeaponPickup(center + new Vector3(2f, 1f, 0f),
                weaponName, weaponDmg, weaponRange, weaponModel, euler);
            var medkit = CreateMedkitPickup(center + new Vector3(-2f, 1f, 0f), healAmount);

            foreach (var sp in spawners) Undo.RegisterCreatedObjectUndo(sp, "Create Room");
            Undo.RegisterCreatedObjectUndo(weapon, "Create Room Weapon");
            Undo.RegisterCreatedObjectUndo(medkit, "Create Room Medkit");
            Selection.activeGameObject = spawners[0];
            MarkSceneDirty();

            string spawnerList = tier == 3
                ? "- Spawner_Piece3_Boss (1 gros monstre)\n- Spawner_Piece3_Minions (3 petits)"
                : $"- Spawner_Piece{tier}";

            EditorUtility.DisplayDialog("School Game",
                $"Pièce {tier} créée :\n{spawnerList}\n- Arme '{weaponName}'\n- Trousse de soin\n\n" +
                "Place tout au sol/à hauteur de poitrine dans la pièce. " +
                (tier == 3 ? "Les 2 spawners peuvent rester au même endroit." : ""),
                "OK");
        }

        private static EnemySpawner MakeSpawner(string name, Vector3 pos,
            string[] prefabPaths, Avatar avatar)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var s = go.AddComponent<EnemySpawner>();
            s.mode = EnemySpawner.SpawnMode.WhenPlayerNear;
            s.enemyAnimator = EnsureEnemyController();
            s.enemyAvatar = avatar;
            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) s.enemyPrefabs.Add(prefab);
            }
            return s;
        }

        // ------------------------------------------------------- Outils unitaires

        [MenuItem("Tools/School Game/Avancé - Spawner vide")]
        public static void CreateSpawner()
        {
            var go = new GameObject("Spawner_Ennemis");
            go.transform.position = SceneViewPoint();
            var s = go.AddComponent<EnemySpawner>();
            s.enemyAnimator = EnsureEnemyController();
            foreach (var path in DefaultEnemyPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) s.enemyPrefabs.Add(prefab);
            }
            Undo.RegisterCreatedObjectUndo(go, "Create Spawner");
            Selection.activeGameObject = go;
            MarkSceneDirty();
        }

        [MenuItem("Tools/School Game/Avancé - Arme (balai)")]
        public static void CreateItemMenu()
        {
            var go = CreateWeaponPickup(SceneViewPoint(), "Balai", 16f, 2.7f,
                PropBroom, new Vector3(0f, 0f, 35f));
            Undo.RegisterCreatedObjectUndo(go, "Create Weapon Pickup");
            Selection.activeGameObject = go;
            MarkSceneDirty();
        }

        [MenuItem("Tools/School Game/Avancé - Trousse de soin")]
        public static void CreateMedkitMenu()
        {
            var go = CreateMedkitPickup(SceneViewPoint(), 45f);
            Undo.RegisterCreatedObjectUndo(go, "Create Medkit");
            Selection.activeGameObject = go;
            MarkSceneDirty();
        }

        private static GameObject CreateWeaponPickup(Vector3 pos, string weaponName,
            float damage, float range, string modelPath, Vector3 euler)
        {
            var go = new GameObject("Arme_" + weaponName.Replace(" ", ""));
            go.transform.position = pos;

            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var pickup = go.AddComponent<WeaponPickup>();
            pickup.modelPrefab = model;
            pickup.modelEuler = euler;
            pickup.weapon = new WeaponData
            {
                weaponName = weaponName,
                damage = damage,
                range = range,
                radius = 1.3f,
                cooldown = 0.5f
            };

            PickupVisuals.Build(go, model, 1f, euler, new Color(1f, 0.85f, 0.2f));
            return go;
        }

        private static GameObject CreateMedkitPickup(Vector3 pos, float heal)
        {
            var go = new GameObject("Soin_Trousse");
            go.transform.position = pos;

            var trigger = go.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.4f;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(PropMedkit);
            var pickup = go.AddComponent<HealthPickup>();
            pickup.modelPrefab = model;
            pickup.modelScale = 1.3f;
            pickup.healAmount = heal;

            PickupVisuals.Build(go, model, 1.3f, Vector3.zero, new Color(0.3f, 1f, 0.4f));
            return go;
        }

        // ----------------------------------------------- AnimatorController auto

        private static RuntimeAnimatorController EnsureEnemyController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (existing != null) return existing;

            AnimationClip idle = LoadClip(FbxFreeMove, "Idle");
            AnimationClip walk = LoadClip(FbxFreeMove, "Walk");
            AnimationClip attack = LoadClip(FbxMelee, "WeakAttack_UnarmedA");
            AnimationClip death = LoadClip(FbxActions, "Death");

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            var sm = ctrl.layers[0].stateMachine;
            var sIdle = sm.AddState("Idle");
            var sWalk = sm.AddState("Walk");
            var sAtk = sm.AddState("Attack");
            var sDie = sm.AddState("Death");

            sIdle.motion = idle;
            sWalk.motion = walk;
            sAtk.motion = attack;
            sDie.motion = death;
            sm.defaultState = sIdle;

            var iw = sIdle.AddTransition(sWalk);
            iw.hasExitTime = false; iw.duration = 0.15f;
            iw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

            var wi = sWalk.AddTransition(sIdle);
            wi.hasExitTime = false; wi.duration = 0.15f;
            wi.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

            foreach (var st in new[] { sIdle, sWalk })
            {
                var a = st.AddTransition(sAtk);
                a.hasExitTime = false; a.duration = 0.05f;
                a.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            }

            var back = sAtk.AddTransition(sIdle);
            back.hasExitTime = true; back.exitTime = 0.7f; back.duration = 0.15f;

            var dieT = sm.AddAnyStateTransition(sDie);
            dieT.hasExitTime = false; dieT.duration = 0.05f;
            dieT.canTransitionToSelf = false;
            dieT.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return ctrl;
        }

        private static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            AnimationClip fallback = null;
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__"))
                {
                    if (c.name == clipName) return c;
                    fallback ??= c;
                }
            }
            if (fallback == null)
                Debug.LogWarning($"[School Game] Clip '{clipName}' introuvable dans {fbxPath}");
            return fallback;
        }

        // ------------------------------------------------------------- Helpers

        private static GameObject FindPlayer()
        {
            var combat = Object.FindFirstObjectByType<PlayerCombat>();
            if (combat != null) return combat.gameObject;

            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name.Contains("vThirdPersonController"))
                    return mb.gameObject;

            foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
                if (go.GetComponent<Collider>() != null) return go;

            return null;
        }

        private static Vector3 SceneViewPoint()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) return sv.pivot;
            var player = FindPlayer();
            return player != null ? player.transform.position + Vector3.forward * 3f : Vector3.zero;
        }

        private static void MarkSceneDirty()
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
    }
}
