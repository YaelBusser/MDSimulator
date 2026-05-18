# MDSimulator

Petit jeu d'action en 3ᵉ personne réalisé sous **Unity 6** : tu explores une école/bureau, tu ramasses des armes de plus en plus puissantes au fil des pièces et tu affrontes des vagues d'ennemis (profs, puis zombies) jusqu'à un boss final.

---

## Sommaire

- [Aperçu du gameplay](#aperçu-du-gameplay)
- [Prérequis](#prérequis)
- [Assets requis (non inclus dans le repo)](#assets-requis-non-inclus-dans-le-repo)
- [Installation](#installation)
- [Commandes](#commandes)
- [Architecture du code](#architecture-du-code)
- [Créer / éditer un niveau](#créer--éditer-un-niveau)
- [Générer un build](#générer-un-build)

---

## Aperçu du gameplay

- **Progression par pièces** : chaque salle a son spawner d'ennemis et son arme à récupérer.
- **3 niveaux de difficulté croissante** :
  | Pièce | Ennemis | Arme |
  |-------|---------|------|
  | 1 — Facile | Profs (×4) | Balai |
  | 2 — Moyen | Profs (×6) | Club de golf |
  | 3 — Boss | 1 gros zombie + 3 petits | Barre de fer |
- **Système de combat** : attaque au corps à corps, flash + recul des ennemis touchés.
- **Vie** : utilise la barre de vie d'Invector ; vol de vie à chaque ennemi tué + trousses de soin.
- **Barre de vie flottante** au-dessus du boss final.
- **UI runtime** : HUD (arme, ennemis restants), écran victoire/défaite, flash de dégâts.

---

## Prérequis

- **Unity 6000.3.11f1** (Unity 6.3) ou compatible
- Render Pipeline : **URP**
- Input System (package Unity)

---

## Assets requis (non inclus dans le repo)

Les gros assets payants **ne sont pas versionnés** (taille + licence Asset Store). Après avoir cloné le projet, réimporte-les depuis le **Unity Asset Store** :

| Asset | Usage |
|-------|-------|
| **Invector — Third Person Controller** (Basic Locomotion) | Personnage joueur, système de vie, animations |
| **Synty — POLYGON Office** | Décor de l'école + personnages (profs) |
| **Synty — POLYGON City / Generic / Prototype** | Props (armes, trousse de soin) |
| **POLYGON Boss Zombies** | Boss et mini-boss zombies du niveau 3 |
| **Universal Vehicle Controller** | (présent dans le projet d'origine, non requis pour ce jeu) |

> Sans ces assets, le projet **ne compilera pas** (le code référence le namespace `Invector` et des prefabs Synty).

Dossiers ignorés par Git :

```
Assets/Synty/
Assets/Invector-3rdPersonController/
Assets/UniversalVehicleController/
Assets/PolygonStreetRacer/
Assets/PolygonBossZombies/
```

---

## Installation

```bash
git clone https://github.com/YaelBusser/MDSimulator.git
```

1. Ouvre le projet dans **Unity 6000.3.11f1**.
2. Réimporte les [assets requis](#assets-requis-non-inclus-dans-le-repo) depuis l'Asset Store.
3. Ouvre la scène **`Assets/Scenes/Demo.unity`**.
4. Menu **`Tools > School Game > 1. Configurer Joueur + Manager`**.
5. Crée les pièces via **`Tools > School Game > Pièce 1 / 2 / 3`** (place les spawners et items dans les salles).
6. Appuie sur **Play**.

---

## Commandes

| Action | Touche |
|--------|--------|
| Déplacement | ZQSD / flèches |
| Courir | Shift |
| Saut | Espace |
| Attaquer | Clic gauche |
| Recommencer (écran de fin) | R |

---

## Architecture du code

Tout le code spécifique au jeu est dans **`Assets/Scripts/SchoolGame/`** :

| Script | Rôle |
|--------|------|
| `GameManager.cs` | État de partie, score, UI runtime, vie (Invector), restart |
| `EnemySpawner.cs` | Apparition des ennemis par pièce/vagues |
| `EnemyAI.cs` | IA ennemie (poursuite, attaque, flash, recul) |
| `EnemyHealthBar.cs` | Barre de vie flottante du boss |
| `Health.cs` | Système de vie générique (ennemis) |
| `PlayerCombat.cs` | Attaque du joueur selon l'arme équipée |
| `WeaponPickup.cs` / `HealthPickup.cs` | Items à ramasser |
| `PickupVisuals.cs` | Visuel des pickups (modèle + halo) |
| `Editor/SchoolGameSetup.cs` | Outils d'éditeur (`Tools > School Game`) |

---

## Créer / éditer un niveau

Menu **`Tools > School Game`** :

- **1. Configurer Joueur + Manager** : ajoute les composants de combat au joueur Invector et crée le `GameManager`.
- **Pièce 1 / 2 / 3** : crée un spawner pré-réglé + une arme + une trousse de soin.
- **Avancé** : spawner vide, arme seule, trousse seule.

Les réglages (vie, dégâts, taille, distance d'activation) sont modifiables dans l'Inspector du `Spawner_*`.

---

## Générer un build

`File > Build Profiles` → plateforme **Windows** → la scène `Assets/Scenes/Demo.unity` doit être la seule cochée → **Build**.

- **Windows** : génère `.exe` + dossier `_Data` (à distribuer ensemble, zippés).
- **macOS** : génère un `.app` (à zipper ; non signé → clic droit > Ouvrir au 1ᵉʳ lancement).
