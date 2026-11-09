# Requirements: Five Stages, New Character Art and VFX

## Introduction

Replace the old character art with the new sprite sheets in `Assets/Sprites/Characters-New`, add sprite-based VFX from `Assets/Sprites/VFX`, and extend the game from 2 to 5 stages ending with the Bakunawa. Mechanics, story beats and balance come from `IMPLEMENTATION.md` (sections 3 to 7) and the GDD (`game-criteria.txt`).

Stage list. The enemy order of each stage is the order of the folders in `Assets/Sprites/Characters-New/Chapter N/` (slots 1 and 2 are regular enemies, slot 3 is the boss):
1. Backyard Mound: Duwende Minion, Enchanted Mushroom, Duwende King
2. Old Balete: Bat, Balete Tree, Kapre
3. Deep Forest: Santelmo, Sigbin, Tiyanak
4. Village Rooftops: Amalanhig, Wak-Wak, Manananggal
5. Cosmic Ocean / Eclipse: Berberoka, Siyokoy, Bakunawa

Note: the GDD text lists Manananggal before Tiyanak. The folder order wins, so the GDD wording will be updated later (IMPLEMENTATION.md section 12).

## Requirement 1: New character art

**User story:** As a player, I want the new character art in every battle, so the game looks consistent.

Acceptance criteria:
1. WHEN a battle loads THEN the player and every enemy SHALL use frames from `Characters-New` (idle, attack, hurt, death; cast and walk where the sheet exists).
2. WHEN a sheet is imported THEN it SHALL be sliced as a 256x256 grid with a fixed pivot, so frames do not jitter between cells.
3. WHEN a sheet has no clip for a state (for example Balete Tree has no cast or walk) THEN the game SHALL fall back to the existing behavior (idle hop, red flash, fade) without errors.
4. WHEN the old `Characters` folder is no longer referenced THEN it MAY be removed, but only after the user confirms.
5. Lola SHALL use her new sheets (talk, laugh, give, heal, idle) in the story scenes.

## Requirement 2: Sprite VFX

**User story:** As a player, I want attacks and abilities to have visible effects, so combat feels alive.

Acceptance criteria:
1. WHEN the player casts a word THEN the hero SHALL use `attack` (short words, `hero_impact`), `cast` (medium words, `hero_bolt` then `hero_impact`) or `kidlat` (long words, `hero_kidlat_bolt` striking the enemy). The Orasyon meter cast SHALL use the hero `flurry` clip with `hero_flurry_hit`.
2. WHEN an enemy uses an attack THEN its matching VFX (see design, VFX mapping) SHALL play on the player or the board.
3. WHEN a grid mechanic fires (smoke, steal, scramble, devour) THEN its VFX SHALL play over the affected tiles or the whole board.
4. WHEN Lola's heal or a Dahon tile heals the player THEN `lola_heal` SHALL play on the player.
5. VFX SHALL be pooled and SHALL clean themselves up, with no per-frame allocations.
6. IF a VFX sprite is missing THEN the existing code-drawn `Burst` or `Projectile` SHALL be used as a fallback.

## Requirement 3: Five playable stages

**User story:** As a player, I want to play through five stages, so I can reach the final Bakunawa fight.

Acceptance criteria:
1. The game SHALL contain `Level1` to `Level5` scenes, each with 2 regular enemies and 1 boss, in Build Settings.
2. WHEN the boss of stage N is defeated THEN the game SHALL play the interlude (stages 1 to 4) or the epilogue (stage 5) and load the next stage or the credits.
3. Each stage SHALL use a distinct background pack from `Assets/Sprites/Terrains`.
4. Each stage boss SHALL drop one treasure (slots 1 to 4 plus the stage 5 reward), per IMPLEMENTATION.md section 6.
5. Stage 1 SHALL keep the tutorial and the fixed first board, now played against the Duwende Minion.
6. The enemy order of each stage SHALL be read from the numeric prefixes of the folders in `Characters-New/Chapter N/`, so reordering folders reorders the fights without code changes.
7. Every enemy slot SHALL use its own art (no reused enemies).

## Requirement 4: Creature mechanics

**User story:** As a player, I want each creature to disrupt the board differently.

Acceptance criteria:
1. Duwende SHALL steal high-value tiles (section 5.1). Kapre SHALL smoke tiles (5.2). Manananggal SHALL toggle Airborne/Grounded (5.3). Tiyanak SHALL scramble the board (5.4). Bakunawa SHALL consume tiles with the Eclipse meter and three phases (5.5).
2. Each mechanic SHALL be a `GridEffect` ScriptableObject, so `BattleManager` does not need per-creature code.
3. Enemy intent SHALL be shown before the enemy acts (section 3.2).
4. The Orasyon meter SHALL clear all bad tiles (section 3.3).

## Requirement 5: Phased delivery

Acceptance criteria:
1. The work SHALL be delivered in phases A to E from IMPLEMENTATION.md section 11, and the game SHALL stay playable after each phase.
2. Art and VFX integration (Phase 0 in the tasks) SHALL ship first, so the existing Chapters 1 and 2 already show the new look.

## Requirement 6: Story

Acceptance criteria:
1. The prologue and the interludes after stages 1 to 4 SHALL use the story text from IMPLEMENTATION.md section 6.4, with Lola on screen where she speaks.
2. The epilogue SHALL end with the moon returning and the Albularyo closing the book, then credits.

## Requirement 7: The board always has fresh letters

**User story:** As a player, I want new letters to appear as soon as I use some, so I always have enough tiles to find a word and never have to hunt for a word just to refresh a nearly empty board.

Acceptance criteria:
1. WHEN a word is played THEN every used tile SHALL be replaced with a new letter right away, so the board is full (16 of 16 tiles) at the start of the next turn.
2. WHEN tiles are replaced THEN unused tiles SHALL keep their position and letters, and the new tiles SHALL appear in the slots just freed, with a short drop-in animation.
3. WHEN the board is refilled THEN it SHALL contain at least one common word of 3 or more letters, with a vowel ratio of 30% to 45% and at most 2 of any rare letter (J, X, Z, K). IF it does not THEN the game SHALL silently re-roll the new tiles (up to 5 tries) or swap in common letters.
4. The board SHALL never need to be emptied before it refreshes, and empty gaps SHALL appear only for Void tiles (Bakunawa) and for tiles an enemy has stolen (Duwende), never because of normal play.
5. Locked, Smoked and stolen tiles SHALL count as present, but the viable-word check SHALL use only tiles the player can actually select.
6. The old behavior (gaps, refill only when stuck) MAY remain as a "Classic" mode setting, but the default SHALL be the always-refill mode.
7. This SHALL ship in Phase 0 of the plan so Chapters 1 and 2 already play this way.

## Out of scope

- New music and SFX (IMPLEMENTATION.md section 8) other than placeholder hooks.
- Chapter select, stars and save data beyond "highest stage unlocked" (IMPLEMENTATION.md section 7, Phase D). Listed as optional tasks.
- Enemies from IMPLEMENTATION.md that have no art (Mangkukulam's Doll, Sirena, Bakunawa Hatchling, Tikbalang Colt). Their moves are redistributed to the creatures that have art.
