# Orasyon: Spirits of the Archipelago — Implementation Spec
### Missing parts + Bookworm Adventures–style battle improvements

This document is a companion to the Project Documentation and the GDD (`game-criteria.txt`). It lists what is **missing or mismatched** between the two, then specifies what to build, in what order, with data models, C# sketches, balance numbers and acceptance checks.

Conventions: new scripts go in `Assets/Scripts/Battle` (namespace `Orasyon.Battle`) unless stated. Builders stay in `Assets/Editor`. Remember the builder warning (docs section 11): put permanent values in the builder scripts, not only in the Inspector.

---

## Table of Contents

1. [Gap Analysis (GDD vs. current build)](#1-gap-analysis-gdd-vs-current-build)
2. [Decisions to Lock First](#2-decisions-to-lock-first)
3. [Battle Mechanic Improvements (Bookworm style)](#3-battle-mechanic-improvements-bookworm-style)
4. [Special Tiles and Bad Tiles](#4-special-tiles-and-bad-tiles)
5. [Creature Grid Mechanics (GDD)](#5-creature-grid-mechanics-gdd)
6. [New Chapters 3–5](#6-new-chapters-35)
7. [Progression, Scoring and Save Data](#7-progression-scoring-and-save-data)
8. [Audio](#8-audio)
9. [UI/UX Gaps](#9-uiux-gaps)
10. [Balance Tables](#10-balance-tables)
11. [Implementation Order and Checklists](#11-implementation-order-and-checklists)
12. [GDD Updates to Make](#12-gdd-updates-to-make)

---

## 1. Gap Analysis (GDD vs. current build)

| GDD requirement | Current state | Status | Section |
|---|---|---|---|
| Select **adjacent** letters | Any tile, any order (Bookworm rules) | Mismatch (intentional, see §2) | 2, 12 |
| Special tiles (bonus damage, healing, protection) | None | Missing | 4 |
| Duwende removes valuable tiles | Not built | Missing | 5.1 |
| Kapre hides tiles with smoke | Not built | Missing | 5.2 |
| Manananggal changes defensive state | Chapter doesn't exist | Missing | 5.3, 6 |
| Tiyanak scrambles the grid | Not built | Missing | 5.4, 6 |
| Bakunawa consumes tiles | Chapter doesn't exist | Missing | 5.5, 6 |
| Chapters 3–5 (rooftops, deep forest, cosmic ocean) | Only Ch1–2 | Missing | 6 |
| Completing a chapter **unlocks** the next | Linear flow only, no chapter select | Missing | 7.1 |
| Stars / chapter ratings | None | Missing | 7.2 |
| Tile feedback: select / submit / **reject** | Select and submit only (no reject feedback, no audio) | Partial | 3.8, 8 |
| Turn indicator | Not listed | Missing | 9 |
| Damage state per creature | Mushroom and Albularyo are flash-only | Partial | 9 |
| Audio (music + SFX) | None | Missing | 8 |
| Keyboard support (optional) | Menu/story only | Partial | 9 |
| Pacing: later encounters need more planning | Enemies pick random attacks | Needs telegraphing | 3.2 |
| Offensive word filtering (quality) | No blocklist | Missing | 3.9 |
| Filipino words (future enhancement) | Small custom list only | Optional | 3.10 |
| Pause / settings menu | None | Missing | 9 |
| 60 FPS target | Not verified | Needs profiling | 11 |

---

## 2. Decisions to Lock First

These affect everything else. Recommended answers are given.

1. **Selection rule:** keep **free selection** (any tile, any order). It is what makes it feel like Bookworm Adventures, and the docs already build around it. Update the GDD wording (§12).
2. **Refill rule:** add a setting `RefillMode` on `LetterGrid`.
   - `AfterEachWord` (**recommended default, Bookworm-faithful**): used tiles are replaced with new letters immediately; surviving tiles stay put.
   - `OnlyWhenStuck` (current behavior): keep as "Hard / Classic" mode.
   Why: special tiles, bad tiles and creature mechanics all need a board that stays full and alive. With gaps, the board shrinks and mechanics like "steal a tile" have little meaning.
3. **Scramble:** keep it free, but limit it to **3 uses per enemy** so it cannot be spammed for a perfect board. Show the remaining count on the button.
4. **Hints:** keep 3 per chapter. Change the hint to prefer the longest *everyday* word **up to 6 letters** so it does not hand out a free Blue potion every time. Add an `allowLongHint` flag if you want the old behavior.
5. **Potions earned:** keep the 5/6/7+ letter rule. Add a per-battle cap of 2 Blue potions so the Power potion cannot trivialize bosses.

---

## 3. Battle Mechanic Improvements (Bookworm style)

### 3.1 Turn loop (target version)

```
Start of player turn
  ├─ tick player statuses (poison, burning tiles)
  ├─ if stunned → skip, enemy acts again
  ▼
Player builds word  ──► [Clear] [Scramble (x/3)] [Hint] [Potion] (free actions)
  ▼
Attack
  ├─ validate word (reject feedback if invalid)
  ├─ resolve tile effects (gold, heal, silver, shield…)
  ├─ play attack tier animation → apply damage
  ├─ award potion / charge Orasyon meter
  ├─ clean up tiles (refill per RefillMode)
  ▼
Enemy defeated? → death anim → next enemy / victory
  ▼
Enemy turn
  ├─ execute telegraphed intent (attack or grid mechanic)
  ├─ choose and **display next intent**
  ▼
Back to start of player turn
```

### 3.2 Enemy intent telegraph (high impact, low cost)

Bookworm players always know what is coming. Show the enemy's **next action** above its head and in the info panel.

- Choose the next attack at the **end** of the enemy's turn (not at the start), store it in `BattleManager.nextEnemyAttack`.
- Show an icon + name + damage range ("Staff Smash 3–4"), or the grid effect icon ("Smoke!").
- Add `Sprite intentIcon` and `string intentHint` to `EnemyAttack`.
- Bosses get a **Signature move** every 3rd turn (fixed cycle, always telegraphed), with the other turns weighted-random.

```csharp
// EnemyAttack.cs additions
public Sprite intentIcon;
public bool isSignature;          // only used on the signature cycle
public GridEffectType gridEffect; // None by default (see section 5)
public int gridEffectCount;       // how many tiles / how strong
public int gridEffectTurns;       // duration where relevant
```

```csharp
// BattleManager.cs (sketch)
EnemyAttack PickNextAttack(CharacterDefinition enemy, int enemyTurnIndex)
{
    if (enemy.isBoss && enemy.signatureEvery > 0 && (enemyTurnIndex + 1) % enemy.signatureEvery == 0)
        return enemy.signatureAttack;
    return WeightedRandom(enemy.attacks, lastAttack);   // avoid same attack 3x in a row
}
```

Rules for `WeightedRandom`: never repeat the same attack three times in a row; never pick a second Stun immediately after a Stun (already a rule).

### 3.3 Orasyon Meter (the "book" mechanic)

A small, readable power-up that rewards good play, matching the albularyo theme.

- Each valid word adds charge: `charge += wordLength` (cap 30).
- When full, the **ORASYON** button lights up. Using it (free action) casts the Book's prayer: **deals fixed 10 damage ignoring Guard, cures poison/weaken, and clears all bad tiles** (smoke, locked, burning, hex).
- Resets to 0 at the start of each enemy. Carries over between turns.
- Why: gives the player an answer to creature grid mechanics without removing the challenge.

New script: `OrasyonMeter` (UI bar + `Charge(int)`, `TryUse()`).

### 3.4 Updated damage formula

```
letterSum = Σ letterValue × (silverTile ? 2 : 1)
base      = round(letterSum × (1 + 0.25 × max(0, len − 3)) × 1.5), min 1

multiplier = (1 + treasureBonus + 0.20 × goldTiles)      // additive group
           × weakness (1.5)
           × power potion (2)
           × (1 − weaken%)
           × (1 − enemyGuard%)
           × stateModifier                                // e.g. Manananggal airborne/grounded

damage = max(1, round(base × multiplier))
```

Display breakdown in the preview hint when you hover the Attack button (e.g. `9 base  ×1.5 weakness  ×1.2 gold`). It teaches the system and helps debugging.

### 3.5 Player words that inflict status on the enemy

Bookworm Adventures has "status" words. Add data-driven lists so it stays cheap:

```
Assets/Resources/Words/status_words.txt
# format:  WORD,EFFECT,TURNS,POWER
VENOM,Poison,3,2
TOXIN,Poison,3,2
SLEEP,Stun,1,0
FREEZE,Stun,1,0
CHAIN,Stun,1,0
BLAZE,Burn,3,2
FLAME,Burn,3,2
SHIELD,Heal,0,5
```

- Effects reuse the existing status system but applied to the **enemy** (`Poison`, `Stun`, plus new `Burn` = poison variant with orange tick).
- **Bosses are immune to Stun** (they take half duration of Poison/Burn).
- Pre-load the file in `WordDictionary` into a `Dictionary<string, WordEffect>`.
- Show "Word of Power!" popup and tint the preview tiles gold when the current word has an effect.

### 3.6 Enemy resistances (variety without new art)

Add `resistWords` / `resistLetters` to `CharacterDefinition`.

- Example: Kapre is **resistant to Short words** (3 letters deal −30%), Manananggal handled in 5.3.
- Show in the info panel under Weaknesses as "Resists".

### 3.7 Combo / streak (optional, small)

Bookworm tracks nothing like this, but a light streak works well for scoring: +5% damage for each **consecutive word of 5+ letters**, max +25%, reset on a 3–4 letter word or taking damage. Skip if the Orasyon Meter already feels sufficient.

### 3.8 Invalid word feedback (GDD 4e)

Currently only valid/green exists. Add:

- Not a word / too short: preview tiles flash **red**, shake 0.15s, message "Not a word" / "Need 3+ letters". Selection stays.
- Already-used word in this battle: allowed, but show a subtle tip. (Optional: −10% damage for repeats to encourage variety, as in Bookworm; set `repeatPenalty = 0.1f`, keep a `HashSet<string>` per battle.)
- Rejected attack must not consume the turn.

### 3.9 Offensive word blocklist

- Create `Assets/Resources/Words/blocked_words.txt` (one per line).
- In `WordDictionary` load it after `words.txt` and remove those words from the sorted array (or keep a `HashSet` checked in `IsWord` and `IsHintWord`).
- Do not include the blocked words in this repository's docs, to keep documentation clean. Use a maintained "bad words" list and check its license.

### 3.10 Auto-refill when only obscure words remain

Problem in docs: boards can have only words like OBE or OBI.

Fix in `LetterGrid`:

```csharp
bool HasViableWord()
{
    // viable = at least one COMMON word of length >= 3 can be made
    return BoardGenerator.FindBest(tiles, WordDictionary.IsCommon, minLen: 3) != null;
}

void EnsureViableBoard()
{
    if (HasViableWord()) return;
    // 1) try up to 5 scrambles silently, 2) otherwise replace the 4 worst tiles
    // (e.g. rarest consonants) with vowels/common letters, play a soft "shimmer".
}
```

Call it after every player attack and after any enemy grid effect.

Also guarantee on generation: **vowel ratio between 30% and 45%** and at most 2 of any rare letter (J, X, Z, K) per board.

### 3.11 Filipino / Tagalog words (optional)

Per GDD this is a future enhancement. Cheap approach: add a few hundred folklore and everyday Tagalog words to `custom_words.txt` (BAHAY, BUWAN, LOLA, ANITO, ANTING, BULAN…) and mark them as common. Keep English as the validation base.

---

## 4. Special Tiles and Bad Tiles

### 4.1 Data model

Extend `LetterTileData`:

```csharp
public enum TileKind { Normal, Gold, Heal, Silver, Shield,        // good
                       Smoked, Locked, Burning, Hex, Void }       // bad / environmental

public class LetterTileData
{
    public char letter;
    public bool selected, highlighted, empty;
    public TileKind kind = TileKind.Normal;
    public int turnsLeft;          // for Smoked / Locked / stolen timers
}
```

`LetterTile` (the view) swaps its border/overlay sprite and shows a small corner icon by `kind`. Add one `Image overlay` child to `TilePrefab`.

### 4.2 Good tiles (GDD: bonus damage, healing, protection)

| Tile | Name (folklore) | Look | Effect when included in a word |
|---|---|---|---|
| Gold | **Ginto** | Gold border | +20% damage per Gold tile in the word (additive) |
| Heal | **Dahon** (leaf) | Green leaf icon | Heal 5 HP (half a heart) when the word is played |
| Silver | **Pilak** | Silver border, "x2" | That letter's value is doubled |
| Shield | **Agimat** | Blue amulet | Grants a 1-hit block (same as Warding block) |

Spawn rules:

- Fresh board: Ch1 = 1 Gold (tutorial), Ch2 = 1–2 (Gold/Heal), Ch3+ = 1–2 random, max 3 specials on board.
- Words of **5+ letters** drop 1 special into a random refilled slot (50% for 5–6, 100% for 7+).
- Specials persist until used. Scramble keeps their kind attached to the tile, not the position.
- Tutorial add-on (Ch1): after WORD/attack steps, one extra step highlights the Gold tile and explains it.

### 4.3 Bad tiles (enemy-placed)

| Tile | Name | Behavior | How to remove |
|---|---|---|---|
| Smoked | **Usok** | Letter shows as `?`; still selectable, so the player must remember it | Use it in a word, wait 3 turns, Warding potion, or Orasyon |
| Locked | **Gapos** | Cannot be selected | Expires after N turns, or Orasyon |
| Burning | **Alab** | At the end of each enemy turn on the board: player takes 2 damage (stacks per tile). Cannot be removed by waiting | Use it in a word, or Orasyon |
| Hex | **Sumpa** | Using it in a word costs the player 5 HP | Do not use it; waits N turns, or Orasyon |
| Void | **Wala** | Permanent empty slot (Bakunawa only). No refill | Cleared only when the battle ends |

Notes:

- Bad tiles are never placed on a tile that is currently selected.
- A word containing a Hex tile still counts and deals damage; the self-damage is applied after the attack.
- Cap bad tiles on board at 5 so the board stays playable.
- Show a duration pip (1–3 dots) on tiles with `turnsLeft`.
- Warding (green) potion: extend to **also clear Smoked and Locked tiles**.

### 4.4 Grid manager API

```csharp
// LetterGrid.cs additions
public List<int> PickTiles(int count, Func<LetterTileData, bool> filter);
public void SetKind(int index, TileKind kind, int turns = 0);
public void StealTiles(IEnumerable<int> indices, int returnAfterTurns);
public void ScrambleAll(bool includeSpecials = true, bool clearSelection = true);
public void TickTileTimers();           // call at the end of each enemy turn
public void ClearBadTiles();            // Orasyon / Warding
public IEnumerable<LetterTileData> SelectedTiles { get; }
```

---

## 5. Creature Grid Mechanics (GDD)

Implement them as small **ScriptableObject effects** so adding a creature does not mean editing `BattleManager`:

```csharp
public enum GridEffectType { None, StealTiles, SmokeTiles, LockTiles, BurnTiles, HexTiles,
                             ScrambleAll, ConsumeTiles, ToggleState }

public abstract class GridEffect : ScriptableObject
{
    public abstract IEnumerator Apply(BattleManager battle, LetterGrid grid, EnemyAttack source);
}
```

Concrete assets: `StealTilesEffect`, `SmokeTilesEffect`, `ScrambleAllEffect`, `ConsumeTilesEffect`, etc. `EnemyAttack.gridEffect` points to one (use a direct `GridEffect` reference instead of the enum if you prefer; the enum above is only for display).

Execution order in the enemy turn: **play the attack animation → apply direct damage (if any) → run grid effect coroutine → tick timers → choose next intent**.

### 5.1 Duwende — "Pilfer" (steal valuable tiles)

- **Effect:** removes the **N highest-value** tiles (ties random) and holds them "in his pocket" (show a little sack icon over the enemy plus the stolen letters in the info panel).
- **Return:** stolen tiles return after 2 turns **or immediately when the Duwende takes 10+ damage from one word** ("shaken loose"). Returned tiles come back in the same slot if free, otherwise a random free slot.
- **Counter-play:** weakness words (MOUND, GOLD…) deal more damage so the player can shake the loot back.
- **Minion:** steals 1 tile. **King (boss):** steals 2, plus once at half HP he steals a special tile if one exists.
- Attack data: `Pilfer`, 1 damage, 100% effect, weight 2 (Minion) / weight 1 + signature on 3rd turn (King).

### 5.2 Kapre — "Choking Smoke" (hide tiles)

- **Effect:** marks 4 random unselected tiles as **Smoked** for 3 turns (6 for the boss signature "Great Cloud": 6 tiles).
- Replace the current Kapre moveset entry `Choking Smoke` (poison) with this plus a smaller poison, or keep poison and add a new `Smoke Veil` attack. Suggested weights: Smoke Slam 3, Smoke Veil 2, Ground Stomp 1, Long Drag 1.
- Cigar-smoke VFX: a gray particle puff over each affected tile.
- **Counter-play:** memorize the letters before they are smoked (the preview is shown while the telegraph is active), cure with Warding / Orasyon.

### 5.3 Manananggal — "Split" (defensive state toggle)

- Two states: **Airborne** (torso flying) and **Grounded** (lower half left behind, exposed).
- **Airborne:** takes −60% damage from words under 5 letters; words of 5+ letters deal normal damage. She attacks harder (+1 damage).
- **Grounded:** takes **+25%** damage from any word and cannot use her signature attack. Lasts 1 enemy turn.
- **Toggle rule:** starts Airborne; every 2nd enemy turn she **toggles** (telegraphed as "Splits apart!" / "Reunites!"). If the player lands a **7+ letter** word while Airborne, she is forced to Grounded for one turn (the "salt" hit).
- UI: state badge next to her HP bar, swap the sprite tint or reuse the Idle clip (no new art required); add "Airborne" / "Exposed" text popups.
- Implementation hook: `ComputeDamage` multiplies by `enemyState.DamageModifier(wordLength)`.

```csharp
public interface IEnemyState { float DamageModifier(int wordLength); string Label { get; } }
```

### 5.4 Tiyanak — "Cry" (scramble the grid)

- **Wail:** scrambles **all** tiles (including specials), clears the current selection, and displays "The baby cries!" with a screen shake. Because scrambling is a new layout, the player loses planned words.
- **Lullaby (optional second attack):** locks 3 tiles for 2 turns (Gapos).
- **Fake Smile:** hexes 2 tiles (Sumpa) for 3 turns — punishes careless play.
- The Tiyanak is a small, low-HP, fast enemy: HP lower than others but it **attacks twice** with small damage (two `EnemyAttack` entries chained in one turn: add `int hits` to `EnemyAttack`).
- Cap: Wail is not chosen two turns in a row.

### 5.5 Bakunawa — "Eclipse" (consume the board)

Final boss, three phases keyed to HP (100% → 66% → 33%).

- **Eclipse Meter** (0–7, shown as the moon being swallowed): +1 at the end of each enemy turn.
- **Each enemy turn:** consumes N random tiles as **Void** (N = 1 in phase 1, 2 in phase 2, 3 in phase 3). Void tiles are not refilled.
- **Total Eclipse** (meter reaches 7): massive attack (6 damage, ignores block once), meter resets to 0, **all Void tiles return** (the moon is released).
- **Counter-play (folklore):** the people banged pots and gongs to scare the Bakunawa. **Words of 6+ letters "ring the gong": meter −2** (min 0). Words of 8+ letters: −3.
- Phase 2 adds Burning tiles; phase 3 adds Smoked tiles ("the sky darkens").
- Background: darken the vignette as the meter rises (use the existing generated vignette sprite and tween its alpha).
- Boss intro uses the story system (Interlude) with a short eclipse flash.

---

## 6. New Chapters 3–5

Use existing `Terrains` packs for backgrounds (assign the three that best fit: rooftops / forest / ocean-sky; check licenses, docs section 15). Add each as a `ChapterSpec` in `Level1BattleBuilder` plus a `[MenuItem]` as in docs section 12. **All enemy art is required from your artist**; names below are proposals.

### 6.1 Chapter 3 — Village Rooftops (Manananggal)

| # | Enemy | HP | Weak to | Moveset |
|---|---|---|---|---|
| 1 | Wak-Wak (bird-bat) | 35 | WING, FEATHER, SKY, ROOST | Beak Peck 2–3 (15% crit, w3); Screech 1 + 40% stun (w1) |
| 2 | Mangkukulam's Doll | 35 | PIN, CLOTH, THREAD, DOLL | Pin Prick 2 + 60% poison (1/turn, 3) (w2); **Hex Stitch**: hexes 2 tiles (w2); Cursed Hug 3 (w1) |
| 3 | **Manananggal (boss)** | 60 | SALT, GARLIC, DAWN, SUN, ROOF | Claw Swipe 3–4 (20% crit, w3); Blood Drain 2 + heal 100% of damage (w2); **Split** (state toggle, signature); Night Wail 2 + 40% weaken −30% (w1) |

Mechanic introduced: **Hex tiles** (Doll) and **state toggle** (boss). Treasure: **Agimat ng Bubong** — start each battle with 1 Warding potion (slot 2).

### 6.2 Chapter 4 — Deep Forest (Tiyanak)

| # | Enemy | HP | Weak to | Moveset |
|---|---|---|---|---|
| 1 | Tikbalang Colt | 40 | HORSE, MANE, HOOF, GALLOP | Hoof Kick 2–4 (20% crit, w3); Charge 3 (w1, **charged**: telegraphed 1 turn earlier) |
| 2 | Sigbin | 40 | SHADOW, HEEL, NIGHT, TAIL | Shadow Bite 2–3 + 40% poison (w3); **Gapos**: locks 3 tiles for 2 turns (w2) |
| 3 | **Tiyanak (boss)** | 60 | LULLABY, CRADLE, BABY, FOREST | Cry (scramble all, signature, w2); Fake Smile (hex 2, w2); Scratch ×2 hits 1–2 (w3); Mimic Cry: guard −40% for 2 words (w1) |

Mechanic introduced: **scramble** and **Locked tiles**. Treasure: **Kampana ng Gubat** — +2 hints per chapter (slot 3).

### 6.3 Chapter 5 — Cosmic Ocean / Eclipse (Bakunawa)

| # | Enemy | HP | Weak to | Moveset |
|---|---|---|---|---|
| 1 | Sirena | 45 | WAVE, SONG, PEARL, TIDE | Tidal Splash 3–4 (w3); Siren Song: smokes 3 tiles (w2) |
| 2 | Bakunawa Hatchling | 45 | EGG, SCALE, MOON, TAIL | Tail Whip 3–5 (20% crit, w3); Nibble: consumes 1 tile as Void for the battle (w2) |
| 3 | **Bakunawa (final boss)** | 100 | MOON, ECLIPSE, GONG, SERPENT, BULAN | Phase moveset in §5.5; Dragon Bite 4–6 (20% crit, w3); Moon Devour (signature) |

Treasure: **Mata ng Buwan** — gem frequency ×2 and +1 heart (slot 4). Add a final **Ending Story** (`Epilogue`) and credits scene instead of "Chapter Complete".

### 6.4 New story assets (lightweight, per GDD §3d)

| Asset | Plays | Content |
|---|---|---|
| `Interlude_Chapter2` | After Kapre | Lola warns about creatures that fly at night; the book glows brighter |
| `Interlude_Chapter3` | After Manananggal | The forest whispers; a baby cries somewhere |
| `Interlude_Chapter4` | After Tiyanak | The sky darkens; eclipse begins; Lola tells the pot-banging tradition |
| `Epilogue` | After Bakunawa | Moon returns; the Albularyo closes the book |

Add each through `StoryBuilder` so they are part of the build, and set each chapter spec's `nextStoryAsset`.

---

## 7. Progression, Scoring and Save Data

### 7.1 Chapter unlock and select screen (GDD 2e)

- Add a **Chapter Select** scene (or a panel in MainMenu): 5 chapter cards with name, background thumbnail, best stars, locked/unlocked state.
- Save in `PlayerPrefs` key `orasyon.progress` as JSON:

```csharp
[Serializable] public class SaveData
{
    public int highestChapterUnlocked = 1;
    public Dictionary<int,int> bestStars;       // use two arrays if JsonUtility is used
    public Dictionary<int,int> bestDamageWord;  // optional
    public List<string> treasures;
    public bool musicOn = true, sfxOn = true;
    public float musicVolume = 0.7f, sfxVolume = 0.9f;
}
```

(`JsonUtility` does not serialize dictionaries; use parallel lists or a small wrapper class.)

- **Start Game** → new run (clears treasures, keeps best stars) **or** **Continue** → load highest chapter. **Chapter Select** allows replay of unlocked chapters. Replaying a chapter does not duplicate treasures.
- Main menu: add **Continue**, **Chapter Select**, **Settings**.

### 7.2 Stars and end-of-chapter scoring

Compute at victory (all values tracked in `BattleManager`):

| Stat | Tracked as |
|---|---|
| Longest word | string + length |
| Total damage dealt | int |
| Hearts lost | int |
| Turns taken | int |
| Hints / potions used | int |

Star rules (suggested, tune after playtests):

- ⭐ Complete the chapter.
- ⭐⭐ Lose **≤ 50%** of max HP overall, or finish with a 7+ letter word.
- ⭐⭐⭐ Lose **≤ 25%** of max HP **and** use no more than 1 hint.

Victory screen shows the stars, longest word and total damage; stars fill with a short animation.

### 7.3 Difficulty setting (optional)

`Easy / Normal / Hard` multiplier applied to enemy damage (×0.75 / ×1 / ×1.25) and `RefillMode` default. Saves in `SaveData`. Helpful for students and casual players (GDD target audience).

---

## 8. Audio

Create `AudioManager` (singleton, `DontDestroyOnLoad`) with an `AudioSource` for music and a pool of 6 sources for SFX. `Play(SfxId)` / `PlayMusic(MusicId, fade)`.

| Category | Sound events |
|---|---|
| Tiles | select, deselect, reject (low buzz), valid-word chime, tile shatter / refill whoosh |
| Combat | staff swing, spell cast (green), brilliant cast (gold), impact, crit, enemy attack, player hurt, heal, poison tick, stun, guard |
| Items | potion use, treasure drop, special tile collected, Orasyon activate |
| UI | button hover, button click, popup, chapter complete jingle, defeat sting |
| Creature | one signature sound per grid mechanic (steal, smoke puff, baby cry, moon devour) |
| Music | Main menu, Story (calm), Chapters 1–5 (one loop each), Boss variant (faster layer), Victory, Defeat |

Style target: kulintang / kubing / gandingan and bamboo-flute flavor mixed with light fantasy percussion. Source from free or self-made assets and **record the license in docs section 15** (e.g. Kenney, OpenGameArt, Freesound with CC0/CC-BY, or self-composed). Do not use unlicensed commercial tracks.

Wiring checklist:
- Add `AudioManager` to MainMenu (persists).
- Call `Play(...)` from: `LetterTile` (select), `WordPreview` (valid/invalid), `BattleManager` (attacks, damage, status), `BuffInventory`, `MenuButtonFX`, story typewriter (soft tick, optional).
- Settings panel with music/SFX sliders, saved in `SaveData`.

---

## 9. UI/UX Gaps

| Item | Spec |
|---|---|
| **Turn indicator** (GDD 5d) | Banner/icon top-center: "Your Turn" / "Enemy Turn" with a slide animation; disable tiles and buttons during the enemy turn |
| **Pause menu** | Esc opens Pause: Resume, Restart Chapter, Settings, Main Menu. Pause with `Time.timeScale = 0` but use unscaled time for UI tweens |
| **Keyboard play** | Type letters to select matching tiles (first free match), **Backspace** removes last, **Enter** attacks, **Tab** scramble, **Space** hint, **1/2/3** potions, **O** Orasyon. Use the new Input System only |
| **Damage state art** | Mushroom and Albularyo currently flash only: add Hurt clips later; keep the flash as fallback |
| **Enemy HP bar** | Show numeric HP and an animated "ghost" bar for recent damage |
| **Status icons** | Small icons with turn counters instead of text only; tooltip on hover |
| **Combat log (small)** | Last 3 events in the message area (e.g. "Pilfer: 2 tiles stolen") |
| **Word preview** | Show gold/silver/hex icons inside preview tiles so the player sees effects before attacking |
| **Accessibility** | Letter tiles at least 64×64 px at 1080p, high contrast, colorblind-friendly tile overlays (shapes + color), UI scale slider |
| **Tutorial extension** | Mini-tutorials (one panel, first encounter only) for: special tiles, bad tiles, intents, Orasyon, each creature mechanic. Store `seenTutorials` in `SaveData` |
| **Defeat screen** | Add "Last word", "Hearts lost" and a tip line from a small tip list |
| **Level load** | Avoid unnecessary loading between encounters (GDD 8b): keep enemies in the same scene, as now |

---

## 10. Balance Tables

### 10.1 Reference numbers

- Average 4-letter common word: ~7–9 damage. Average 5-letter: ~11–14. 6-letter: ~16–19. 7-letter: ~22–28.
- Target **kill time:** regular enemy = 3–4 words, boss = 6–8 words, final boss = 10–12 words.
- Player: 3 hearts base, up to 7 with all treasures (+1 Kalasag, +1 Mata ng Buwan, plus the rest as damage/utility).

### 10.2 Enemy HP and damage budget per chapter

| Chapter | Enemy HP (regular / boss) | Enemy damage per hit (regular / boss) | Special tiles | Bad-tile cap |
|---|---|---|---|---|
| 1 | 30 / 50 | 1–3 / 3–4 | 1 | 0 |
| 2 | 30 / 50 | 1–3 / 3–5 | 1–2 | 3 (smoke) |
| 3 | 35 / 60 | 2–3 / 3–4 | 1–2 | 4 (hex, state) |
| 4 | 40 / 60 | 2–4 / 2×(1–2) | 1–2 | 5 (lock, scramble) |
| 5 | 45 / 100 | 3–5 / 4–6 | 1–2 | 5 (void, fire, smoke) |

Rules of thumb: boss total damage potential over its fight should be about **1.5× the player's max HP** if the player plays no healing; the player is expected to heal through potions and Dahon tiles. Playtest and adjust with the telemetry below.

### 10.3 Quick telemetry (log to console, then CSV)

At the end of each enemy log: turns, words used, average word length, damage taken, potions used, scrambles used. Use this to tune HP numbers; target 70–85% win rate for a first-time player on Normal.

---

## 11. Implementation Order and Checklists

Ordered by impact and dependency. Each phase ends with a playable build.

### Phase A — Core feel (do first)
- [ ] `RefillMode` toggle and `AfterEachWord` refill in `LetterGrid` (§2)
- [ ] Scramble limit 3/enemy and counter on button (§2)
- [ ] Invalid word feedback + red shake, no turn consumed (§3.8)
- [ ] `EnsureViableBoard` and vowel-ratio guarantees (§3.10)
- [ ] Blocklist loading (§3.9)
- [ ] Enemy intent telegraph (§3.2) + `EnemyAttack` fields
- [ ] Turn indicator + disable input during enemy turn (§9)
- [ ] Updated damage formula and hover breakdown (§3.4)
- **Done when:** a full Chapter 1–2 playthrough works with the new refill and telegraphs, no regression in tutorial.

### Phase B — Tiles
- [ ] `TileKind`, `LetterTileData` changes, overlay on `TilePrefab` (§4.1)
- [ ] Gold / Heal / Silver / Shield spawn and effects (§4.2)
- [ ] Bad tiles: Smoked, Locked, Burning, Hex, Void (§4.3)
- [ ] Grid manager API (§4.4)
- [ ] Orasyon Meter and button (§3.3)
- [ ] Warding potion also clears Smoked/Locked
- **Done when:** a debug menu can place every tile kind and each one behaves per spec.

### Phase C — Creature mechanics (Ch1–2 first)
- [ ] `GridEffect` base and assets (§5)
- [ ] Duwende Pilfer (Ch1 boss, Ch2 minion) (§5.1)
- [ ] Kapre smoke (§5.2)
- [ ] Update `Level1BattleBuilder.UpdateCharacterData` with new moves and rerun builders
- [ ] Mini-tutorial panels for new mechanics
- **Done when:** Ch1–2 use the creature mechanics from the GDD and can be won with the Orasyon counter.

### Phase D — Systems
- [ ] `SaveData`, Continue, Chapter Select, unlock logic (§7.1)
- [ ] Stars + victory/defeat stats (§7.2)
- [ ] `AudioManager`, settings panel, first SFX pass (§8)
- [ ] Pause menu + keyboard play (§9)
- [ ] Status words (`status_words.txt`) (§3.5)
- **Done when:** a fresh player can launch, continue later, choose chapters, hear sound, and pause.

### Phase E — New chapters
- [ ] Ch3 content, Manananggal state, Hex tiles (§5.3, 6.1)
- [ ] Ch4 content, Tiyanak scramble, multi-hit attacks, charged attack (§5.4, 6.2)
- [ ] Ch5 content, Bakunawa Eclipse meter and phases (§5.5, 6.3)
- [ ] New story assets and Epilogue/credits (§6.4)
- [ ] Treasures for slots 2–4 (docs section 12 "Add a treasure")
- **Done when:** the game can be completed end to end.

### Phase F — Polish and release
- [ ] Music pass, boss layers, victory/defeat jingles
- [ ] Hurt animations for Mushroom / Albularyo (art dependent)
- [ ] Lola sprite
- [ ] Balance playtests (§10) and telemetry
- [ ] Profile for 60 FPS (use Unity Profiler; pool tiles/popups/damage numbers, avoid per-frame `Find` and string allocations in `Update`)
- [ ] License verification for terrains, music, hearts (docs section 15)
- [ ] Update the Project Documentation with the new systems

### Suggested new scripts

| Script | Purpose |
|---|---|
| `TileKind` / extended `LetterTileData` | Tile types and timers |
| `GridEffect` (+ assets) | Creature grid mechanics |
| `OrasyonMeter` | Charge bar and special cast |
| `EnemyIntentUI` | Telegraph icon and text |
| `EnemyStateController` | Manananggal state, Bakunawa phases and meter |
| `StatusWordDatabase` | Parses `status_words.txt` |
| `SaveManager` / `SaveData` | Progress, settings, stars, seen tutorials |
| `ChapterSelectController` | Chapter cards, locks and stars |
| `AudioManager` / `SfxId` / `MusicId` | Sound |
| `PauseMenu` | Pause and settings |
| `KeyboardInputHandler` | Keyboard play (Input System) |
| `BattleStats` | Per-chapter tracking for stars and telemetry |

### Testing checklist (per phase)

- Word accepted/rejected correctly; no turn is lost on rejection.
- A refilled board always has at least one common word.
- Scrambling preserves tile kinds and timers.
- Stunned and poisoned states still tick correctly with the new bad tiles.
- Orasyon, Warding and Hint do not end the turn and cannot be used during the enemy turn.
- Stolen tiles always return, including if the enemy is defeated mid-effect (return them or discard on enemy change).
- Reloading a scene clears all timers (no coroutine leaks): stop coroutines on `OnDestroy`.
- No use of the old `UnityEngine.Input` class anywhere.

---

## 12. GDD Updates to Make

So that the documentation matches the game:

1. **§2a / 2c / 4a "adjacent letters"** → change to: *"The player selects letter tiles in any order, as in Bookworm Adventures. Adjacency is not required."* (or restore adjacency as an optional Hard mode, but free selection is already implemented).
2. **§2d Key Mechanics** → add: special tiles (Ginto, Dahon, Pilak, Agimat), bad tiles (Usok, Gapos, Alab, Sumpa, Wala), Orasyon Meter, enemy intent telegraph, status words.
3. **§4c Economy** → document potions, treasures and stars as the reward systems.
4. **§4d Progression** → note that bosses drop **permanent treasures** (small equipment system), a deliberate extension of "no large equipment system".
5. **§3e / 6b** → keep the five chapters and name the added minor enemies (Wak-Wak, Doll, Tikbalang Colt, Sigbin, Sirena, Hatchling).
6. **§5b Audio** → list the final audio sources and licenses.
7. **§9d Timeline** → mark which weeks the phases above correspond to (A–B in prototype/core, C–D in content/integration, E in content/testing, F in finalization).