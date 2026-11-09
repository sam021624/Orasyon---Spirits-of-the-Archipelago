# Orasyon: Spirits of the Archipelago — Project Documentation

A 2D word-puzzle battle game inspired by *Bookworm Adventures*, set in Philippine folklore. The player is a young albularyo who spells words from a letter grid to fight mythological creatures.

- **Engine:** Unity 6 (6000.5.7f1), C#, Unity UI + TextMeshPro
- **Input:** new Input System package (the old `UnityEngine.Input` class must not be used)
- **Platform:** Windows PC, mouse + optional keyboard
- **Design reference:** `game-criteria.txt` (the game design document) in the project root

---

## Table of Contents

1. [Running the Game](#1-running-the-game)
2. [Game Flow](#2-game-flow)
3. [Folder Structure](#3-folder-structure)
4. [Core Gameplay Systems](#4-core-gameplay-systems)
5. [Combat Rules](#5-combat-rules)
6. [Enemy Attacks and Status Effects](#6-enemy-attacks-and-status-effects)
7. [Potions, Treasures and Hints](#7-potions-treasures-and-hints)
8. [Story System](#8-story-system)
9. [Content Reference](#9-content-reference)
10. [Script Reference](#10-script-reference)
11. [Editor Tools (Builders)](#11-editor-tools-builders)
12. [How To: Common Tasks](#12-how-to-common-tasks)
13. [Known Limitations and Next Steps](#13-known-limitations-and-next-steps)
14. [Troubleshooting](#14-troubleshooting)
15. [Asset Sources and Licenses](#15-asset-sources-and-licenses)

---

## 1. Running the Game

1. Open the project in Unity 6.
2. Open `Assets/Levels/MainMenu.unity`.
3. Press **Play** and click **Start Game**.

You can also open any scene directly for testing (e.g. `Level2.unity`). The Story scene plays its default script (the Prologue) when opened directly.

**Build order** (File › Build Profiles / Build Settings):

| Index | Scene | Purpose |
|---|---|---|
| 0 | `MainMenu` | Title screen |
| 1 | `Story` | Dialogue player (prologue, interludes) |
| 2 | `Level1` | Chapter 1: The Backyard Mound (includes tutorial) |
| 3 | `Level2` | Chapter 2: The Old Balete |

---

## 2. Game Flow

```
MainMenu ──Start Game──► Story (Prologue) ──► Level1 (Chapter 1)
                                                   │ boss defeated → treasure drop → CONTINUE
                                                   ▼
                          Story (Interlude: "Voices in the Pages") ──► Level2 (Chapter 2)
                                                                           │ boss defeated
                                                                           ▼
                                                                  Chapter Complete (Play Again / Main Menu)
```

- **Start Game** resets collected treasures (a new adventure).
- **Chapter battles:** the player fights 3 enemies in a row; the last is the boss. Between enemies, the Albularyo walks forward while the background scrolls and the next enemy approaches.
- **Defeat** shows a "Defeated" screen with **Try Again** (restart the chapter) and **Main Menu**.
- **Victory** shows "Chapter Complete!", the boss's treasure, and **Continue** (if the chapter has a follow-up story) or **Play Again**.

---

## 3. Folder Structure

```
Assets/
├── Data/
│   ├── Characters/        CharacterDefinition assets (player + all enemies)
│   └── Story/             StoryScript assets (Prologue, Interlude_Chapter1)
├── Editor/                Builder scripts that generate scenes and data (Tools › Orasyon menu)
├── Levels/                Scenes: MainMenu, Story, Level1, Level2
├── Resources/
│   ├── Treasures/         TreasureDefinition assets (loaded at runtime by PlayerProgress)
│   ├── Words/             words.txt, common_words.txt, custom_words.txt
│   └── TilePrefab.prefab  Letter tile used by the grid
├── Scripts/
│   ├── Battle/            Grid, dictionary, combat, enemies, potions, treasures, tutorial
│   ├── Story/             Dialogue player and data
│   └── UI/                Title screen and decorative effects
└── Sprites/
    ├── Characters/        Character animation frames (one folder per character)
    ├── Icons/             Hearts, Potions (generated), Treasures (generated)
    ├── Terrains/          Background packs 1–8 (parallax layers)
    └── UI/                Generated glow / vignette sprites
```

### Character sprite layout

Each character folder follows the same structure (frames are 256×256 PNGs):

```
Assets/Sprites/Characters/<Character>/Idle/
├── animations/<ClipName_description>/<direction>/frame_000.png …
└── rotations/south.png, east.png, …   (still poses)
```

Clips are matched by folder-name prefix (e.g. `Idle`, `Attack`, `Hurt`, `Death`, `Cast`, `Walking`).

---

## 4. Core Gameplay Systems

### 4.1 Letter grid (`LetterGrid`)

- 4×4 board of letter tiles.
- **Bookworm rules:** click **any** tiles in **any** order. Tiles do not need to touch.
- Clicking a selected tile removes it and every letter after it.
- **Clear** deselects everything.
- **Attack** is only enabled when the selected letters form a valid word of 3+ letters.
- Tiles used in an attack are **removed** and leave empty gaps (no gravity, no refill).
- **Scramble** shuffles the letters of the remaining tiles. Gaps stay in place. It is free (does not cost a turn).
- **Refill:** gaps are refilled with new letters only when no valid word can be made from the remaining tiles.
- **New board:** each new enemy gets a freshly generated board.

### 4.2 Word dictionary (`WordDictionary`)

| File | Contents | Use |
|---|---|---|
| `Resources/Words/words.txt` | ~170,000 English words, 3–16 letters (ENABLE list) | Word validation |
| `Resources/Words/common_words.txt` | ~8,000 everyday English words | Choosing hint words |
| `Resources/Words/custom_words.txt` | Folklore and project words (DUWENDE, KAPRE, BALETE, ORASYON…) | Added to both lists |

Words are stored in a sorted array, so word and prefix checks are binary searches. Minimum word length is 3.

### 4.3 Board generation (`BoardGenerator`)

- Letters are drawn using English letter frequency (Q is excluded).
- One of the current enemy's **theme words** is planted on each new board.
- Boards are regenerated (up to 40 attempts) until they contain at least `minWordsOnBoard` (default 8) makeable words.
- Chapter 1's first board is fixed (`WORDATESLINGBUKA`) so the tutorial can highlight W-O-R-D.

### 4.4 Word preview (`WordPreview`)

Shows the current word as tiles above the battle. Tiles turn green for a valid word. The hint text below shows the expected damage, plus "Spell!" (5–6 letters) or "Brilliant!" (7+).

---

## 5. Combat Rules

### 5.1 Hearts

- 1 heart = 10 HP.
- Player HP carries over between enemies within a chapter and resets at the start of each chapter.

### 5.2 Damage formula (`LetterScoring.Damage` + `BattleManager.ComputeDamage`)

```
base   = (sum of letter values) × (1 + 0.25 × max(0, length − 3)) × 1.5     (rounded, min 1)
damage = base × treasure bonus × weakness (1.5) × Power potion (2) × weaken (−%) × enemy guard (−%)
```

**Letter values**

| Value | Letters |
|---|---|
| 1 | A E I L N O R S T U |
| 1.25 | D G |
| 1.5 | B C M P |
| 1.75 | F H V W Y |
| 2 | K |
| 2.5 | J X |
| 3 | Q Z |

Examples: CAT = 5, WORD = 9, MOUND = 13, STRING = 16, JUKEBOX = 34.

**Weakness:** spelling one of the enemy's theme words deals ×1.5 damage.

### 5.3 Attack tiers (by word length)

| Length | Animation | Effects |
|---|---|---|
| 3–4 letters | Walks up to the enemy and swings the staff, then walks back | Small impact burst |
| 5–6 letters | Casts from his spot; a green spell orb arcs to the enemy | Sparks trail, larger green burst |
| 7+ letters | Casts a bigger gold orb | Gold burst, screen shake, "BRILLIANT!" popup |

Damage numbers grow with the tier.

### 5.4 Turn order

1. Player spells a word and presses **Attack**.
2. Player attack plays; damage is applied; long words may award a potion.
3. If the enemy is defeated → death animation → next enemy (or victory).
4. Otherwise the enemy takes its turn (see section 6).
5. Start of the player's turn: poison ticks; if stunned, the turn is skipped and the enemy acts again.

Potions and Scramble can be used at any time during the player's turn and do not end the turn.

---

## 6. Enemy Attacks and Status Effects

Each enemy has a list of attacks (`EnemyAttack`). Every turn it picks one at random, weighted by the attack's `weight`.

**Attack fields**

| Field | Meaning |
|---|---|
| `name` | Shown in the feedback text and the info panel |
| `minDamage` / `maxDamage` | Damage range. `0 / 0` = a self-buff performed in place (no approach) |
| `critChance` | Chance (0–1) of double damage |
| `weight` | How often it is chosen relative to the others |
| `effect`, `effectChance`, `effectTurns`, `effectPower` | Status effect (below) |

**Status effects**

| Effect | On | What it does |
|---|---|---|
| Poison | Player | Loses `effectPower` HP at the start of each turn for `effectTurns` turns |
| Stun | Player | Loses the next turn; the enemy acts again. Cannot chain two stuns in a row |
| Weaken | Player | Next `effectTurns` words deal `effectPower`% less damage |
| Guard | Enemy | Takes `effectPower`% less damage from the next `effectTurns` words |
| Leech | Enemy | Heals `effectPower`% of the damage dealt, or `effectPower` HP if it's a self-buff |

Notes:
- Critical hits show red "CRIT" text and shake the screen.
- Active effects are shown under the player's name (left) and the enemy's name (right).
- All effects are cleared when a new enemy appears.
- The **Warding (green) potion** cures poison and weaken.
- The Warding potion's block absorbs one attack completely, including its effect.

The right-hand info panel lists every attack with damage range, crit chance and effect.

---

## 7. Potions, Treasures and Hints

### 7.1 Potions (left panel, top shelf)

Earned by spelling long words. Click a potion to use it; it does not end the turn. Max stack: 9.

| Potion | Earned by | Effect |
|---|---|---|
| Red — Healing | 5-letter word | +1 heart (10 HP) |
| Green — Warding | 6-letter word | Cures poison/weaken and blocks the next attack |
| Blue — Power | 7+ letter word | Next word deals ×2 damage |

A word awards the best potion it qualifies for (only one per word).

### 7.2 Treasures (left panel, 5 slots below the potions)

Permanent items dropped by chapter bosses, like Bookworm's treasures. One slot per final boss (5 total).

| Treasure | Dropped by | Bonus |
|---|---|---|
| Kalasag ng Punso (shield) | Duwende King, Chapter 1 | +1 max heart |
| Pana ng Balete (bow) | Kapre, Chapter 2 | +20% damage on every word |

- On boss defeat the treasure pops out where the boss fell and flies to its slot.
- The victory screen shows "Treasure found" with the item's bonus.
- Clicking an owned treasure shows its description.
- Bonuses stack and apply from the next battle onward.
- Treasures are saved in `PlayerPrefs` (key `orasyon.treasures`) and survive between scenes and sessions. **Start Game** clears them. Winning a chapter again does not grant duplicates.

### 7.3 Hints

- **HINT** button left of Attack. 3 hints per chapter (`hintsPerChapter` on BattleManager).
- Lights up, in green, the tiles of the **longest everyday word** that can be made. Falls back to the longest valid word if no everyday word exists.
- The hint word appears in the message area ("Lola whispers through the book…").
- If no word can be made at all, pressing Hint refills the gaps instead and does not use a hint.

---

## 8. Story System

### 8.1 How it works

- `StoryScript` (ScriptableObject) holds one dialogue scene: title card, background, a list of lines, and the scene to load afterwards.
- `StoryRouter.Play(script)` loads the `Story` scene and passes it the script.
- `StoryPlayer` shows a title card, then each line with a typewriter effect.

**Controls:** click, Space or Enter to advance (the first press finishes the current line). **Skip** or Esc jumps to the next scene.

**Line options**

| Field | Meaning |
|---|---|
| `speaker` | Narrator, Albularyo or Lola |
| `text` | The line |
| `flash` | White flash before the line (magic moments) |
| `changeBackground` | Background shown after the flash |
| `lolaOnStage` | Lola is visible. Off = she speaks "through the book" |

The current speaker stays bright and the listener dims.

### 8.2 Current story

| Asset | Plays | Summary |
|---|---|---|
| `Prologue` | After Start Game, before Level1 | At dusk Lola Isidra gives her grandson the Book of Orasyon. The book pulls him into the story world. She sends him to face the Duwende King. |
| `Interlude_Chapter1` | After the Duwende King, before Level2 | Lola congratulates him through the book and warns him about the Kapre in the oldest balete. |

**Characters**
- **Albularyo:** the young player character.
- **Lola Isidra:** his grandmother, an old albularya and Keeper of the Book (the game's equivalent of Bookworm's librarian). She currently has no sprite and appears as a glowing spirit-light placeholder.

---

## 9. Content Reference

### 9.1 Chapters

| Chapter | Scene | Background | Enemies (last = boss) | Tutorial | Treasure | Next |
|---|---|---|---|---|---|---|
| 1: The Backyard Mound | Level1 | Terrains/1 | Forest Bat → Balete Tree Minion → Duwende King | Yes | Kalasag ng Punso | Interlude → Level2 |
| 2: The Old Balete | Level2 | Terrains/6 | Enchanted Mushroom → Duwende Minion → Kapre | No | Pana ng Balete | — (end) |

### 9.2 Characters

| Character | Asset | HP (hearts) | Weak to |
|---|---|---|---|
| Albularyo | `Albularyo` | 30 (3) | — |
| Forest Bat | `01_ForestBat` | 30 (3) | BAT, WING, NIGHT, CAVE, FANG |
| Balete Tree Minion | `02_BaleteTreeMinion` | 30 (3) | ROOT, TREE, LEAF, BARK, VINE |
| Duwende King (boss) | `03_DuwendeKing` | 50 (5) | MOUND, GOLD, KING, DIRT, GNOME |
| Enchanted Mushroom | `04_EnchantedMushroom` | 30 (3) | SPORE, MOSS, DEW, CAP, GLOW |
| Duwende Minion | `05_DuwendeMinion` | 30 (3) | CLUB, ELF, IMP, BEARD, MOUND |
| Kapre (boss) | `06_Kapre` | 50 (5) | SMOKE, CIGAR, GIANT, BALETE, ASH |

### 9.3 Enemy movesets

| Enemy | Attack | Damage | Extra | Weight |
|---|---|---|---|---|
| Forest Bat | Wing Gust | 1–2 | 10% crit | 3 |
| | Fang Bite | 1 | 60% poison (1/turn, 2 turns) | 2 |
| Balete Tree Minion | Root Lash | 2–3 | 10% crit | 3 |
| | Entangle | 1 | 50% stun | 1 |
| Duwende King | Staff Smash | 3–4 | 20% crit | 3 |
| | Mischief Hex | 1–2 | weaken −40% (2 words) | 2 |
| | Mound Shelter | — | guard −50% (2 words) | 1 |
| Enchanted Mushroom | Spore Burst | 1–2 | 60% poison (1/turn, 3 turns) | 3 |
| | Sleepy Spores | 1 | 50% stun | 1 |
| Duwende Minion | Club Bonk | 2–3 | 25% crit | 3 |
| | Dirt Toss | 1 | 80% weaken −30% (2 words) | 1 |
| Kapre | Smoke Slam | 3–5 | 15% crit | 3 |
| | Choking Smoke | 2 | 80% poison (2/turn, 2 turns) | 2 |
| | Ground Stomp | 2–3 | 50% stun | 1 |
| | Long Drag | — | heals 6 HP | 1 |

### 9.4 Animation availability

| Character | Idle | Walk | Attack | Cast | Hurt | Death |
|---|---|---|---|---|---|---|
| Albularyo | ✓ | ✓ (faces right, mirrored for left) | ✓ | ✓ | flash only | ✓ |
| Forest Bat | ✓ | hops | ✓ | — | ✓ | ✓ |
| Balete Tree Minion | ✓ | hops | ✓ | — | ✓ | ✓ |
| Duwende King | ✓ | hops | ✓ | ✓ | ✓ | fade out |
| Enchanted Mushroom | still image | hops | ✓ | — | flash only | fade out |
| Duwende Minion | ✓ | hops | ✓ | — | ✓ | ✓ |
| Kapre | ✓ | hops | ✓ | — | ✓ | ✓ |

Enemies only have front-facing art, so they keep their idle animation and hop when moving. Flying enemies (`hoverHeight > 0`) bob more gently.

---

## 10. Script Reference

### Battle (`Assets/Scripts/Battle`, namespace `Orasyon.Battle`)

| Script | Responsibility |
|---|---|
| `BattleManager` | Runs a chapter: turns, movement, damage, enemy attacks, status effects, potions, hints, treasure drop, victory/defeat, visual effects |
| `LetterGrid` | 4×4 board: selection, consuming tiles, scramble, refill, hints, tutorial highlights |
| `LetterTile` / `LetterTileData` | One clickable tile (view) and its state (letter, selected, highlighted, empty) |
| `BoardGenerator` | Builds boards, plants theme words, counts/finds makeable words |
| `WordDictionary` | Loads word lists; `IsWord`, `IsPrefix`, `IsCommon` |
| `LetterScoring` | Letter frequencies and damage values |
| `WordPreview` | Current-word tiles and damage hint above the battle |
| `CharacterDefinition` | ScriptableObject: stats, attacks, info text, animation frames |
| `EnemyAttack` | One enemy move (damage range, crit, weight, status effect) |
| `EnemyInfoPanel` | Right panel: portrait, weaknesses, attack list, description |
| `HeartBar` | Row of hearts that drain partially |
| `BuffInventory` | Potion shelf: awarding, counts, using |
| `TreasureDefinition` | ScriptableObject: a permanent boss drop and its bonuses |
| `TreasureShelf` | The 5 treasure slots |
| `PlayerProgress` | Saved treasures and their total bonuses |
| `TutorialManager` | Chapter 1 tutorial steps (welcome → grid → spell WORD → attack → tips) |
| `UIFrameAnimator` | Plays texture-frame animations on a UI RawImage |
| `ParallaxBackground` | Layered scrolling background (auto-scroll or on demand) |

### Story (`Assets/Scripts/Story`, namespace `Orasyon.Story`)

| Script | Responsibility |
|---|---|
| `StoryScript` | ScriptableObject: title, background, lines, next scene |
| `StoryPlayer` | Plays a StoryScript in the Story scene |
| `StoryRouter` | Passes the chosen story into the Story scene |

### UI (`Assets/Scripts/UI`, namespace `Orasyon.UI`)

| Script | Responsibility |
|---|---|
| `MainMenuController` | Title screen: intro fade, title bob, How to Play panel, Start (resets treasures), Quit |
| `MenuButtonFX` | Button hover glow / scale |
| `FloatingLetterTile` | Bobbing decorative tiles |
| `FireflyDrift` | Drifting, twinkling motes |
| `GlowPulse` | Pulsing glow |

---

## 11. Editor Tools (Builders)

Scenes and most data are generated by builder scripts in `Assets/Editor`, available under **Tools › Orasyon**.

| Menu item | Script | What it does |
|---|---|---|
| Rebuild Level 1 Battle UI | `Level1BattleBuilder.Build` | Rebuilds `BattleCanvas` in Level1 and updates character data |
| Rebuild Level 2 Battle UI | `Level1BattleBuilder.BuildLevel2` | Same for Level2 |
| Rebuild Main Menu | `MainMenuBuilder.Build` | Rebuilds the whole MainMenu scene |
| Rebuild Story Scene + Dialogue | `StoryBuilder.Build` | Rebuilds the Story scene, the story assets and build settings |
| Reset Collected Treasures | `TreasureAssets.ResetCollected` | Clears saved treasures (useful when testing) |

> **Warning — what rebuilding overwrites**
> - Level builders replace `BattleCanvas`, and **reset** character info (descriptions, weak words, attack names, sizes) and **movesets** to the values in `Level1BattleBuilder.UpdateCharacterData`. HP of existing enemies is kept.
> - The Main Menu builder replaces everything in the MainMenu scene.
> - The Story builder **overwrites the dialogue lines** in `Prologue` and `Interlude_Chapter1`.
> - Treasure assets are only created if missing; Inspector edits are kept.
>
> To make permanent changes to generated values, edit them in the builder scripts, or stop running the builder after editing in the Inspector.

Chapter setup lives in `Level1BattleBuilder` as `ChapterSpec` entries (`Chapter1`, `Chapter2`): scene, title, background pack, enemy list, tutorial on/off, first board, follow-up story, treasure and victory message.

---

## 12. How To: Common Tasks

### Tune an enemy
Select its asset in `Assets/Data/Characters/` and edit HP, attacks, description or theme words in the Inspector. Remember that rebuilding a level resets info and movesets (section 11); for permanent changes edit `UpdateCharacterData` in `Level1BattleBuilder.cs`.

### Add an enemy
1. Add its sprite folder under `Assets/Sprites/Characters/` using the standard layout.
2. In `Level1BattleBuilder.UpdateCharacterData`, add a `Creature(...)` call (asset name, folder, display name, clip prefixes) and set its attacks, theme words and description.
3. Add its asset name to a chapter's `enemyAssets`.
4. Run the level's rebuild menu item.

### Add a chapter
1. Create a scene in `Assets/Levels/` (e.g. `Level3.unity`) with a Main Camera.
2. Add a `ChapterSpec` in `Level1BattleBuilder` and a `[MenuItem]` method that calls `BuildChapter` with it.
3. Add the scene to `StoryBuilder.UpdateBuildSettings` (or Build Settings).
4. Point the previous chapter's `nextStoryAsset` to an interlude whose `nextScene` is the new level.

### Add or edit dialogue
Edit lines in `Assets/Data/Story/*.asset`, or create a new one via **Create › Orasyon › Story Script**. Set `nextScene`. To play it after a chapter, assign it to that chapter's BattleManager **Next Story** (or the `nextStoryAsset` in the chapter spec).

### Add a treasure
Create one via **Create › Orasyon › Treasure** inside `Assets/Resources/Treasures/`. Give it a unique `id`, a `slotIndex` (2–4 are free), an icon and bonuses. Set it as that chapter's `rewardAsset` (or BattleManager **Boss Reward**).

### Add folklore words
Add them (one per line, uppercase) to `Assets/Resources/Words/custom_words.txt`. They become valid words and count as everyday words for hints.

### Give Lola a sprite
Select the `StoryPlayer` object in the Story scene and set **Lola Art**. The glowing placeholder hides automatically.

---

## 13. Known Limitations and Next Steps

**Current limitations**
- Lola Isidra has no sprite (glowing placeholder).
- The Enchanted Mushroom has only an Attack animation (still image otherwise).
- Enemies have no walk animation (they hop).
- The Albularyo has one cast animation; spell tiers are distinguished by effects, not unique animations.
- Potion and treasure icons are generated placeholder pixel art.
- No sound or music yet.
- Creature-specific grid mechanics from the GDD (Duwende steals tiles, Kapre's smoke hides tiles, Tiyanak scrambles) are not built yet.
- The ENABLE word list includes some offensive words; no blocklist yet.
- Boards can contain only obscure words (e.g. OBE, OBI); they don't refill automatically in that case.
- The game ends after Chapter 2.
- Hints pick the longest word, which can be generous (often a potion-earning spell).

**Recommended next steps** (rough order of impact)
1. Special / gem tiles (bonus damage, healing, x2) and enemy-placed bad tiles (locked, burning).
2. Creature grid mechanics from the GDD.
3. Sound effects and music.
4. Player words that inflict status (e.g. weakness words stun the enemy).
5. End-of-chapter scoring (longest word, total damage, stars).
6. Chapters 3–5 (Manananggal, Tiyanak, Bakunawa) and their treasures.
7. Offensive-word blocklist; auto-refill when only obscure words remain.

---

## 14. Troubleshooting

| Problem | Cause / Fix |
|---|---|
| "You are trying to read Input using the UnityEngine.Input class…" | Something uses the old input API or `StandaloneInputModule`. Use `UnityEngine.InputSystem` and `InputSystemUIInputModule`. |
| "The project path contains spaces…" (AI Game Developer plugin) | Harmless warning from the MCP plugin. Moving the project to a path without spaces removes it. |
| Buttons do nothing after reloading a scene | Listeners added with `AddListener` in edit mode aren't saved. Builders use `UnityEventTools.AddPersistentListener`. |
| A word you expect isn't accepted | It's not in `words.txt`. Add it to `custom_words.txt`. |
| Treasures from a previous test are still owned | Use **Tools › Orasyon › Reset Collected Treasures**, or start from the Main Menu. |
| Inspector edits to enemies were lost | A level rebuild reset them (section 11). |
| New UI objects have no RectTransform | `new GameObject()` under a Canvas doesn't add one automatically; add `typeof(RectTransform)`. |

---

## 15. Asset Sources and Licenses

| Asset | Source | Notes |
|---|---|---|
| `words.txt` | ENABLE word list (via github.com/dolph/dictionary) | Public domain |
| `common_words.txt` | "google-10000-english" (no-swears list, via github.com/first20hours) filtered against ENABLE | Check the repository's license before publishing |
| Character sprites | Provided by the project team | — |
| Terrains backgrounds (packs 1–8) | Already in the project; original source unknown | **Verify the license before publishing** |
| Heart icons | Already in the project (`Sprites/Icons/Hearts`) | Check `Hearts/PNG/health_bar/info.txt` for terms |
| Potion and treasure icons, glow/vignette sprites | Generated by the builder scripts | Project-owned placeholders |
