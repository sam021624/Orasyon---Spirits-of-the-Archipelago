# Implementation Plan: Five Stages, New Character Art and VFX

Run builders from `Tools/Orasyon` in the Unity Editor after each phase. Requirement numbers refer to `requirements.md`. Mob order per chapter comes from the folders in `Assets/Sprites/Characters-New/Chapter N/` (see design 1.1).

Order of work: art, VFX and the refill fix first, then all five stages playable end to end with simple attacks, then the board mechanics stage by stage. The game stays playable after every top-level task.

- [x] 1. Phase 0: New art, VFX and always-refill board
  - [x] 1.1 Import settings for the strips (`SheetImportSettings`): plain textures, bilinear, no mipmaps, max size 8192. No sprite slicing: a `FrameClip` shows one frame by moving the RawImage UV rect, so frames never jitter. Cell sizes are handled in `VfxAssets` (128x256 for `hero_kidlat_bolt`, 96 for `bakunawa_beam`) and `CharacterAssets` (square 256 cells). Frame counts verified.
    - _Requirements: 1.2_
  - [x] 1.2 `CharacterDefinition` now uses `FrameClip` clips (plus optional `kidlat` and `flurry`, and `spriteFacesRight`). `UIFrameAnimator` plays them on the existing RawImage, so no new animator type or scene component swap was needed.
    - _Requirements: 1.1, 1.3_
  - [x] 1.3 Make the builder data-driven: scan `Chapter N/{slot}_{creature}` folders in numeric order (last = boss), add `Sheet(folder, clip)`, create `Ch{N}_{Slot}_{Creature}.asset` for all 15 enemies and the hero, and carry over stats from the old assets (Minion, Mushroom, King, Bat, Tree, Kapre). Keep the old assets until Task 1.9 passes.
    - _Requirements: 1.1, 1.3, 3.1_
  - [x] 1.4 Rebuild Level 1 (Minion, Mushroom, King) and Level 2 (Bat, Balete Tree, Kapre) with the new enemy order. Screenshot each character and set `spriteSize`, `hoverHeight` and `faceLeft` so nothing overlaps the UI. Check hero walk in the travel scene.
    - _Requirements: 1.1, 3.5_
  - [x] 1.5 Create `VfxDefinition`, `VfxPlayer` (pooled), and the generated `Resources/Vfx` assets. (The additive-blend material was left out: effects use normal alpha for now. Add it later if they look flat.)
    - _Requirements: 2.5_
  - [x] 1.6 Hook VFX into `BattleManager`: hero tiers (attack, cast + `hero_bolt`, 7+ letters with `kidlat` + `hero_kidlat_bolt`), `hero_impact`, heal with `lola_heal`, enemy attack VFX per design 3.4, block. Keep `Burst` and `Projectile` as fallbacks.
    - _Requirements: 2.1, 2.2, 2.4, 2.6_
  - [x] 1.7 Lola is animated in the Story scene from `Prologue/lola` (idle, talk, laugh, give, heal). She talks while her line types; a line can pick `Give` / `Laugh` / `Heal` via the new `lolaPose` field. `Tools/Orasyon/Rebuild Story Scene Only` rebuilds the scene without touching dialogue.
    - _Requirements: 1.5_
  - [x] 1.8 Always-refill board: add `RefillMode` to `LetterGrid` (default `AfterEachWord`), call `RefillEmpty` right after `RemoveSelected`, guarantee a common word, vowel ratio and rare-letter cap, add the tile drop-in animation, keep Classic mode as a setting.
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.6, 7.7_
  - [x] 1.9 Checkpoint: play Chapters 1 and 2 end to end. The board has 16 tiles every turn, the tutorial still passes (Duwende Minion is now the tutorial enemy), no console errors. Then remove the old assets `01_` to `06_` after asking you.
    - _Requirements: 5.1, 5.2, 7.1_

- [x] 2. Five playable stages (simple attacks, no new board mechanics yet)
  - [x] 2.1 Backgrounds picked from `Terrains`: stage 3 = pack 5 (moonlit blue forest), stage 4 = pack 2 (dusk silhouette sky), stage 5 = pack 7 (water along the shore). None of the packs has rooftops or an ocean, so 4 and 5 are the closest fits. Change `backgroundPack` in `Level1BattleBuilder` to swap one.
    - _Requirements: 3.3_
  - [x] 2.2 Add `Chapter3`, `Chapter4`, `Chapter5` specs, menu items and scenes `Level3` to `Level5` (Santelmo, Sigbin, Tiyanak / Amalanhig, Wak-Wak, Manananggal / Berberoka, Siyokoy, Bakunawa), with stats from design 4 and basic movesets (damage and the existing Poison/Stun/Weaken/Guard/Leech effects) plus their VFX.
    - _Requirements: 3.1, 2.2_
  - [x] 2.3 Rewards: Kampana ng Gubat (stage 3), Agimat ng Bubong (stage 4), Mata ng Buwan (stage 5) through `TreasureAssets` and the shelf slots.
    - _Requirements: 3.4_
  - [x] 2.4 Story: `Interlude_Chapter2`, `Interlude_Chapter3`, `Interlude_Chapter4`, `Epilogue` and a credits section; update `StoryBuilder.UpdateBuildSettings` (MainMenu, Story, Level1 to Level5) and wire each `nextStory` / `nextScene`.
    - _Requirements: 3.2, 6.1, 6.2_
  - [x] 2.5 Save the highest stage in `PlayerProgress` and add Continue to the MainMenu.
  - [x] 2.6 Checkpoint: a full run from the main menu to the Bakunawa and the credits.
    - _Requirements: 3.1, 3.2_

- [x] 3. Phase A: core feel (IMPLEMENTATION.md section 11)
  - [x] 3.1 Scramble limit 3 per enemy with a counter on the button. Hints nerfed (your request): 2 per chapter, never longer than 5 letters, a random everyday word among the longest allowed; Kampana ng Gubat now gives +1 hint; power potions capped at 2 per fight.
  - [x] 3.2 Invalid-word feedback (red shake, no turn consumed).
  - [x] 3.3 Board quality was done in 1.8; the offensive-word blocklist is in (`blocked_words.txt`, 145 words from LDNOOBW, CC BY 4.0, credited in the credits).
  - [x] 3.4 Enemy intent above the enemy's head and marked in the info panel, boss signature moves on every 3rd enemy turn (never the same move 3x in a row, no stun right after a stun). The multi-hit and grid-effect fields wait for task 5.
    - _Requirements: 4.3_
  - [x] 3.5 Turn indicator, invalid-word reject feedback (no turn used), numeric HP on the name lines, and a damage breakdown in the word hint line (shown whenever a valid word is selected rather than only on hover).
  - [x] 3.6 Checkpoint: all five stages playable with telegraphs. Boss HP raised: King 80, Kapre 100, Tiyanak 120, Manananggal 140, Bakunawa 220 (enemy hearts are capped at 12 on screen).

- [x] 4. Phase B: tiles
  - [x] 4.1 `TileKind`, extended `LetterTileData` (kind + turn timer), corner badge and timer labels on `TilePrefab`. Badges are short words (+20%, +5HP, x2, WARD, SMOKE, LOCK, -2HP, -5HP) so colour is never the only cue. Also done here (your request): tapping a tile in the word row sends that tile back to the board.
  - [x] 4.2 Good tiles (Ginto, Dahon, Pilak, Agimat) with spawn rules and effects.
  - [x] 4.3 Bad tiles (Usok, Gapos, Alab, Sumpa, Wala) and the `LetterGrid` API.
  - [x] 4.4 `OrasyonMeter` (fills by word length up to 30, empties on each new enemy) and the cast: hero `flurry` clip, `hero_flurry_hit`, 10 fixed damage ignoring guard, cures poison/weaken, clears all bad tiles. Warding also clears Smoked and Locked. Tutorial gained a special-tiles step.
    - _Requirements: 4.4_
  - [x] 4.5 Debug menu (`Tools/Orasyon/Debug`) that places every tile kind, clears bad tiles and fills the meter. Checkpoint: every kind verified in Play mode.

- [x] 4b. UI polish (your feedback): enemy moves shown as icons in the info panel and above the enemy's head (the move's own VFX picture, plus damage / crit / status icons; hover a move for its description), clearer enemy description and weakness line, tutorial panel no longer overlaps its button, no text on gold tiles, tapping a word-row tile returns it to the board.

- [ ] 5. Board mechanics, stage by stage (design section 5)
  - [ ] 5.1 Stage 1: `GridEffect` base, `StealTilesEffect`; Duwende Minion and King Pilfer (off during the tutorial); mini-tutorial panels.
    - _Requirements: 4.1, 4.2_
  - [ ] 5.2 Stage 2: `SmokeTilesEffect`; Kapre Smoke Veil and Great Cloud with `kapre_smoke`.
  - [ ] 5.3 Stage 3: `BurnTilesEffect` (Santelmo), `LockTilesEffect` (Sigbin), `HexTilesEffect` and `ScrambleAllEffect` plus multi-hit attacks (`hits`) for the Tiyanak boss, with `tiyanak_wail` and screen shake.
  - [ ] 5.4 Stage 4: Amalanhig hex, Wak-Wak smoke, `EnemyStateController` and the Manananggal Airborne/Grounded state with badge and popups.
  - [ ] 5.5 Stage 5: Berberoka and Siyokoy tile effects, `ConsumeTilesEffect`, Void tile, Eclipse meter UI, Bakunawa phases at 66% and 33%, Total Eclipse with `bakunawa_beam`, the 6+ and 8+ letter gong rule, vignette tween.
  - [ ] 5.6 Checkpoint: each stage uses its mechanic and can be won with the Orasyon counter.

- [ ] 6. Optional: Phase D and F polish
  - [ ] 6.1 Chapter Select, stars, defeat/victory stats.
  - [ ] 6.2 `AudioManager` with SFX and music hooks (use the `sfxHook` on `VfxDefinition`).
  - [ ] 6.3 Pause menu, keyboard play, status words.
  - [ ] 6.4 Balance playtests and 60 FPS profile; remove the old `Characters` folder once nothing references it (ask first).

## Open questions for you

1. Backgrounds for stages 3 to 5: I will show screenshots of candidate `Terrains` packs before locking them in (Task 2.1). Tell me now if you already have preferences.
2. Stage order differs from the GDD text (Tiyanak is now stage 3 and Manananggal stage 4, following your folders). OK to also update the GDD wording later?
3. Any objection to the plan order (art, VFX and refill, then five stages with simple attacks, then mechanics)?
