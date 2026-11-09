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

- [ ] 3. Phase A: core feel (IMPLEMENTATION.md section 11)
  - [ ] 3.1 Scramble limit 3 per enemy with a counter on the button.
  - [ ] 3.2 Invalid-word feedback (red shake, no turn consumed).
  - [ ] 3.3 `EnsureViableBoard` extras and blocklist loading.
  - [ ] 3.4 `EnemyAttack` new fields, `PickNextAttack`, `EnemyIntentUI`, signature cycle for bosses.
    - _Requirements: 4.3_
  - [ ] 3.5 Turn indicator and input lock during the enemy turn; updated damage formula with hover breakdown.
  - [ ] 3.6 Checkpoint: all five stages playable with telegraphs.

- [ ] 4. Phase B: tiles
  - [ ] 4.1 `TileKind`, extended `LetterTileData`, overlay child on `TilePrefab`.
  - [ ] 4.2 Good tiles (Ginto, Dahon, Pilak, Agimat) with spawn rules and effects.
  - [ ] 4.3 Bad tiles (Usok, Gapos, Alab, Sumpa, Wala) and the `LetterGrid` API.
  - [ ] 4.4 `OrasyonMeter` and button (casts the hero `flurry` clip and `hero_flurry_hit`); Warding also clears Smoked and Locked.
    - _Requirements: 4.4_
  - [ ] 4.5 Debug menu that places every tile kind. Checkpoint: each kind behaves per spec.

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
