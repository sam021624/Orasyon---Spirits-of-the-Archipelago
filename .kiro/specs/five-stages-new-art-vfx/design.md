# Design: Five Stages, New Character Art and VFX

## 1. Asset facts (verified from the files)

### 1.1 Folder layout is the game's content order

`Assets/Sprites/Characters-New/Chapter N/{slot}_{creature}/` where slot 1 and 2 are regular enemies (fought in that order) and slot 3 is the boss.

| Stage | 1st enemy | 2nd enemy | Boss (3rd) |
|---|---|---|---|
| Chapter 1 | Duwende Minion | Enchanted Mushroom | Duwende King |
| Chapter 2 | Bat | Balete Tree | Kapre |
| Chapter 3 | Santelmo | Sigbin | Tiyanak |
| Chapter 4 | Amalanhig | Wak-Wak | Manananggal |
| Chapter 5 | Berberoka | Siyokoy | Bakunawa |

Also: `Hero/hero/` (player) and `Prologue/lola/` (Lola). VFX mirror the same chapter folders under `Assets/Sprites/VFX/Chapter N`, plus `VFX/Hero` and `VFX/Prologue`.

Note: this order differs from the GDD text (which lists Manananggal before Tiyanak). The folders win, so Tiyanak is stage 3 and Manananggal is stage 4. Environments and rewards follow the creature (see section 4).

### 1.2 Sheet facts

| Item | Finding |
|---|---|
| Character sheets | One horizontal strip per clip, 256x256 cells. Width 2048 = 8 frames, 2304 = 9, 2560 = 10, 3584 = 14 (Kapre death), 4096 = 16 (Lola talk) |
| Hero clips | idle (8), walking (8), attack (9), cast (9), hurt (9), death (9), **kidlat (11)**, **flurry (23)** |
| Enemy clips | All have idle, attack, hurt, death. Walking: Mushroom, Santelmo, Sigbin, Tiyanak, Amalanhig, Wak-Wak, Manananggal, Berberoka, Siyokoy, Bakunawa. Cast: King, Tiyanak, Manananggal, Bakunawa |
| Lola clips | idle, talk (16), laugh, give (9), heal (10), walking |
| `*_rotations.png` | 8 directions (2048 wide) or a single image (256). Fallback still only |
| Existing import | `.meta` files auto-sliced into trimmed rects with varying pivots (for example x=50, w=138), so animation would jitter. Re-slice as a grid |
| VFX | Strips with 128 px cells (6 to 10 frames). Verified by viewing: `hero_bolt` is 6 frames of 128, `hero_kidlat_bolt` (1024x256) is **8 frames of 128x256**, a tall bolt that strikes down onto the target. `bakunawa_beam` (1536x96) is assumed 16 frames of 96. Verify when the importer runs |
| Current runtime | `CharacterDefinition` uses `Texture2D[]`, `UIFrameAnimator` sets `RawImage.texture`. `BattleManager` draws effects with code (`Burst`, `Projectile`). Builder is hard-coded to 2 chapters and 6 enemy assets (`01_ForestBat` .. `06_Kapre`) |

## 1b. Always-refill board (Requirement 7)

Current behavior (`LetterGrid`): `RemoveSelected` marks used tiles `IsEmpty`, and `RefillEmpty(minWords)` runs only when no word can be made. That is why late turns have only 2 to 3 tiles.

Change:
- Add `enum RefillMode { AfterEachWord, OnlyWhenStuck }` to `LetterGrid`, default `AfterEachWord`, read from a setting (Classic mode keeps the old path).
- After the attack animation and damage, `BattleManager` calls `grid.RemoveSelected()` followed immediately by `grid.RefillEmpty(minWords: 1)`. Only the freed slots are filled, so remaining tiles do not move.
- `RefillEmpty` uses `BoardGenerator` so that the whole board (old plus new tiles) has at least one common word, with the vowel and rare-letter limits. It tries up to 5 random fills for the freed slots, then falls back to weighted common letters (vowels first).
- `LetterTile` plays a drop-in tween (0.2 s, staggered 0.03 s) for refilled tiles.
- Tile kinds (Phase B): new tiles default to `Normal`. A 5+ letter word may drop one special tile into a refilled slot (IMPLEMENTATION.md 4.2).
- The existing "stuck" refill remains as a safety net and for Void/stolen tiles.
- Tutorial: the fixed first board stays; refill after the tutorial word uses the normal rules.

## 2. Art pipeline

### 2.1 Importer (`Assets/Editor/SpriteSheetImporter.cs`)

- Menu: `Tools/Orasyon/Import New Sprite Sheets`, plus an `AssetPostprocessor` for paths under `Characters-New` and `VFX` (recursive, because of the chapter subfolders).
- For each PNG: `spriteMode = Multiple`, point filter, no mipmaps, no compression, then rebuild `spritesheet` as a grid. Cell sizes: 256x256 for characters, 128x128 for VFX, **128x256 for `hero_kidlat_bolt`**, 96 for `bakunawa_beam`. A small override table in the importer handles the exceptions. Pivot is bottom-center for characters and the kidlat bolt, center for other VFX.
- Frame count is `width / cellWidth`. Sprite names are `{file}_{index}`.
- `*_rotations.png` is imported as a single sprite (fallback still).

### 2.2 Runtime: sprites instead of textures

- Add `Sprite[]` clips next to the old `Texture2D[]` fields in `CharacterDefinition`, plus `Sprite[] kidlat` and `Sprite[] flurry` (hero only, optional), and a `bool UsesSprites` helper.
- Add `SpriteFrameAnimator` (an `Image`-based copy of `UIFrameAnimator`, same public API: `SetIdle`, `PlayOnce`, `PlayLoop`, `Flash`, `FadeTo`, `Duration`). `BattleManager` and `TutorialManager` already use the animator through that API, so the swap is a field type. Both animators implement `IFrameAnimator`.
- Facing: decided per creature with a `faceLeft` bool on `CharacterDefinition`, verified by eye in Task 1.4.

### 2.3 Builder: data-driven chapters

- The builder discovers enemies by scanning `Characters-New/Chapter N/` and ordering the subfolders by their numeric prefix (`1_`, `2_`, `3_`). The last one is the boss. Moving or renaming a folder therefore changes the mob order with no code edit.
- New `Sheet(folder, clip)` returns `Sprite[]` from `{folder}/{creature}_{clip}.png` (null when missing, so the fallbacks in `UIFrameAnimator` still apply).
- Character data assets are named `Ch{N}_{Slot}_{Creature}.asset` (for example `Ch3_1_Santelmo`). The old `01_ForestBat` to `06_Kapre` assets are retired after the migration (Task 1.3), not edited in place.
- `ChapterSpec` is expanded to five entries, with menu items `Rebuild Level 1..5 Battle UI`. Movesets, descriptions and theme words move to a static `ChapterDatabase` so the builder file stays readable.
- Existing Level1 and Level2 scenes are rebuilt (they change enemies). Rebuilding replaces `BattleCanvas`, so every permanent value lives in the builder.

## 3. VFX system

### 3.1 Components

- `VfxDefinition` ScriptableObject: `Sprite[] frames`, `fps`, `size`, `loop`, `additive`, `tint`, `sfxHook` (string placeholder), `anchor` (Center / BottomCenter for tall effects).
- `VfxPlayer` (MonoBehaviour on the battle canvas): `Play(def, anchoredPos, scale)` and `Travel(def, from, to, duration)`. Pool of 12 `Image`s.
- `Resources/Vfx/*.asset` generated by the importer so no scene references are needed.
- VFX layer sits above the characters and below popups. Additive material `Assets/Materials/UIAdditive.mat`.

### 3.2 Hero attack tiers (new)

Today there are three tiers by word length (`tier` in `AttackRoutine`: tier 1 attack, higher tiers use the cast clip). New mapping:

| Word / action | Hero clip | VFX |
|---|---|---|
| 3 to 4 letters | `attack` | `hero_impact` on the enemy |
| 5 to 6 letters | `cast` | `hero_bolt` travels staff to enemy, then `hero_impact` |
| 7+ letters | `kidlat` (11 frames) | `hero_kidlat_bolt` strikes down on the enemy (bottom-center anchor), screen shake, then `hero_impact` |
| Orasyon meter cast | `flurry` (23 frames) | `hero_flurry_hit` on the enemy once per hit beat, 10 fixed damage |
| Heal (Dahon tile / Healing Potion) | `cast` | `lola_heal` on the hero |

The tier thresholds reuse the current tier code (so 3-letter, 5-letter, 7-letter breakpoints may need to move to match; check `BattleManager.TierFor` in Task 1.6). Kidlat and flurry are optional on `CharacterDefinition`; if empty, the code falls back to `cast`.

### 3.3 Enemy hook points

| Moment | Current code | New behavior |
|---|---|---|
| Enemy attack | `Burst` on the enemy | the move's `vfx` plays on the player (or travels from the enemy); `hitVfx` plays on arrival |
| Grid effect | none | per-effect VFX over affected tiles, or board-wide |
| Block / ward | `Burst` | keep, add `lola_heal` tinted blue at low alpha |

### 3.4 VFX mapping (by chapter folder)

| Chapter | Creature | Move | VFX |
|---|---|---|---|
| 1 | Duwende Minion | Club Bonk, Pilfer | `minion_bonk` |
| 1 | Enchanted Mushroom | Spore Burst, Sleepy Spores | `mushroom_spores` |
| 1 | Duwende King | Staff Smash, Mischief Hex, Pilfer | `minion_bonk` (scaled), `king_curse` |
| 2 | Bat | Wing Gust, Fang Bite, Drain | `bat_drain` |
| 2 | Balete Tree | Root Lash, Entangle | `balete_roots` |
| 2 | Kapre | Smoke Slam, Smoke Veil | `kapre_smoke` (over tiles for the veil) |
| 3 | Santelmo | Fire Orb, Flame Wisp, Burn tiles | `santelmo_orb` (travel), `santelmo_fire`, `santelmo_flames` (burning tiles), `santelmo_burst` |
| 3 | Sigbin | Shadow Bite, Gapos (lock) | `sigbin_bite`, `sigbin_shadow` (over locked tiles) |
| 3 | Tiyanak | Scratch x2, Fake Smile, Cry | `tiyanak_slash`, `tiyanak_wail` (full board) |
| 4 | Amalanhig | Grave Grip, Hex | `amalanhig_grip` |
| 4 | Wak-Wak | Talons, Rake, Screech, Feather Storm | `wakwak_talons`, `wakwak_rake`, `wakwak_screech`, `wakwak_feathers`, `wakwak_featherhit` |
| 4 | Manananggal | Claw Swipe, Blood Drain, Split | `manananggal_slash`, `manananggal_mist` |
| 5 | Berberoka | Swallow, Flood | `berberoka_glob`, `berberoka_splash` |
| 5 | Siyokoy | Stab, Splash, Wave, Geyser, Shell | `siyokoy_stab`, `siyokoy_splash`, `siyokoy_wave`, `siyokoy_geyser`, `siyokoy_shell` (guard) |
| 5 | Bakunawa | Dragon Bite, Eclipse, Total Eclipse | `bakunawa_wave`, `bakunawa_moon` (meter), `bakunawa_beam` (Total Eclipse) |

`EnemyAttack` gets `VfxDefinition vfx` and `VfxDefinition hitVfx` (other new fields: IMPLEMENTATION.md 3.2).

## 4. Stage roster and environments

Every slot now has its own art, so no enemy is reused. Background packs are `Terrains/1..8`; the current choice for stages 1 and 2 is kept, the rest are proposals to confirm after looking at the packs.

| Stage | Name | Background | Enemies (in fight order) | Reward (boss drop) |
|---|---|---|---|---|
| 1 | The Backyard Mound | Terrains/1 (existing) | Duwende Minion, Enchanted Mushroom, **Duwende King** | Kalasag ng Punso |
| 2 | The Old Balete | Terrains/6 (existing) | Bat, Balete Tree, **Kapre** | Pana ng Balete |
| 3 | The Deep Forest | Terrains/ forest pack (to pick) | Santelmo, Sigbin, **Tiyanak** | Kampana ng Gubat (+2 hints) |
| 4 | The Village Rooftops | Terrains/ night village pack (to pick) | Amalanhig, Wak-Wak, **Manananggal** | Agimat ng Bubong (start each battle with a Warding potion) |
| 5 | The Cosmic Ocean | Terrains/ sea/sky pack (to pick) | Berberoka, Siyokoy, **Bakunawa** | Mata ng Buwan (+1 heart, gems x2) |

Stages 3 and 4 are swapped relative to IMPLEMENTATION.md 6.1 / 6.2 (Manananggal rooftops and Tiyanak forest). The rewards, difficulty rows (10.2) and mechanic introductions follow the creature, not the number.

Stats and balance use the 10.2 table by stage number (HP 30/50 in stages 1 and 2, 35/60, 40/60, 45/100 for stages 3 to 5). The Tiyanak, now stage 3, has the 40 to 60 HP of the old stage 4 boss row, bumped down to 55 if playtests show a spike. Manananggal in stage 4 uses 60.

## 5. Creature mechanics by stage

Implemented as in IMPLEMENTATION.md section 5 as `GridEffect` ScriptableObjects, introduced one major complication per stage:

| Stage | Creature | Move that touches the board | Effect |
|---|---|---|---|
| 1 | Duwende Minion | Pilfer | steals 1 tile for 2 turns (disabled while the tutorial is active) |
| 1 | Enchanted Mushroom | Spore Burst | poison / stun only (no board effect yet) |
| 1 | Duwende King | Pilfer (signature every 3rd turn) | steals 2 tiles; extra special-tile steal at half HP |
| 2 | Bat | Drain | leech only |
| 2 | Balete Tree | Entangle | stun only |
| 2 | Kapre | Smoke Veil, Great Cloud (signature) | Usok (smoked) 4 tiles / 6 tiles for 3 turns |
| 3 | Santelmo | Will-o-wisp | places Burning (Alab) tiles: 2 damage per tile at the end of each enemy turn |
| 3 | Sigbin | Gapos | locks 3 tiles for 2 turns |
| 3 | Tiyanak | Cry (signature), Fake Smile | scrambles the board; hexes 2 tiles (Sumpa); attacks twice |
| 4 | Amalanhig | Grave Grip | hexes 2 tiles for 3 turns |
| 4 | Wak-Wak | Feather Storm | smokes 3 tiles |
| 4 | Manananggal | Split (signature) | Airborne / Grounded state toggle (damage modifier by word length) |
| 5 | Berberoka | Swallow | consumes 1 tile as Void for the battle |
| 5 | Siyokoy | Sea Mist | smokes 3 tiles |
| 5 | Bakunawa | Eclipse | consumes tiles, three phases, Eclipse meter, Total Eclipse |

New code: `TileKind` and extended `LetterTileData`; `LetterGrid` API (`SetKind`, `StealTiles`, `ScrambleAll`, `TickTileTimers`, `ClearBadTiles`); `GridEffect` base with `StealTilesEffect`, `SmokeTilesEffect`, `LockTilesEffect`, `HexTilesEffect`, `BurnTilesEffect`, `ScrambleAllEffect`, `ConsumeTilesEffect`; `EnemyStateController` + `IEnemyState`; `OrasyonMeter`; `EnemyIntentUI`. `EnemyAttack` additions: `gridEffect`, `gridEffectCount`, `gridEffectTurns`, `hits`, `isSignature`, `intentIcon`, `vfx`, `hitVfx`.

## 6. Stage flow and scenes

- `StoryBuilder` gets `Interlude_Chapter2/3/4`, `Epilogue` and a `Credits` section (scrolling text in the Story scene).
- `UpdateBuildSettings` order: MainMenu, Story, Level1 to Level5.
- A boss win with `nextStory` loads the story; the story's `nextScene` loads the next level. Stage 5's `nextStory` is the Epilogue, which ends in credits then `MainMenu`.
- Unlock: `PlayerProgress` (exists) stores the highest stage. MainMenu "Continue" loads it. Full Chapter Select is optional.

## 7. Story beats (from IMPLEMENTATION.md 6.4, adjusted to the new order)

| Asset | After | Speaker | Beat |
|---|---|---|---|
| Prologue | start | Lola (`lola_give`) | Lola gives the Book of Orasyon |
| Interlude 1 | Duwende King | Lola | Voices in the pages (exists) |
| Interlude 2 | Kapre | Lola | The book glows brighter; strange lights drift in the forest ahead (Santelmo) |
| Interlude 3 | Tiyanak | Lola (`lola_talk`) | A baby's cry fades; the village rooftops show, and something flies there at night |
| Interlude 4 | Manananggal | Lola | The sky darkens, the eclipse begins; Lola teaches the pot-banging tradition (hint for the Eclipse meter) |
| Epilogue | Bakunawa | Lola (`lola_laugh`) + hero | The moon returns; the Albularyo closes the book; credits |

## 8. Risks

- **Facing and pivot**: wrong facing or off-center pivots overlap the UI. Mitigation: per-creature `faceLeft`, `spriteSize` and `hoverHeight` in `ChapterDatabase`, tuned by screenshot (Task 1.4).
- **Hero flurry is 23 frames**: at the default 10 fps it lasts 2.3 s. Use 16 fps for it, and cap the attack time.
- **Texture memory**: about 100 sheets; disable mipmaps and compress.
- **Builder overwrite**: rebuilding replaces `BattleCanvas`; all values live in the builder.
- **Scope**: mechanics are large; Phase 0 (art, VFX, refill) must ship before any mechanic work.
- **No Unity MCP in this session**: scenes are built through the editor builders, which must be run inside the Unity Editor.
