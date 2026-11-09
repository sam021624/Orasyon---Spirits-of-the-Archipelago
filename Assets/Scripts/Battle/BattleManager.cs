using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Orasyon.Battle
{
    /// <summary>
    /// Runs one chapter: a sequence of enemies ending in a boss.
    /// Player turn: spell a valid word → walk up → attack → walk back. Enemy turn: lunge and strike.
    /// Between enemies the player walks forward while the background scrolls and the next foe approaches.
    /// </summary>
    public sealed class BattleManager : MonoBehaviour
    {
        [Header("Chapter")]
        [Tooltip("1 to 5. Winning unlocks the next chapter.")]
        [SerializeField] private int             chapterNumber = 1;
        [SerializeField] private string          chapterTitle = "Chapter 1: The Backyard Mound";
        [SerializeField] private TextMeshProUGUI chapterLabel;
        [SerializeField] private TextMeshProUGUI encounterLabel;

        [Header("Player")]
        [SerializeField] private CharacterDefinition player;
        [SerializeField] private UIFrameAnimator     playerAnimator;
        [SerializeField] private TextMeshProUGUI     playerNameLabel;
        [SerializeField] private HeartBar            playerHearts;
        [SerializeField] private TextMeshProUGUI     playerStatusLabel;

        [Header("Enemies (fought in order; the last one is the boss)")]
        [SerializeField] private CharacterDefinition[] enemies;
        [SerializeField] private UIFrameAnimator     enemyAnimator;
        [SerializeField] private TextMeshProUGUI     enemyNameLabel;
        [SerializeField] private HeartBar            enemyHearts;
        [SerializeField] private EnemyInfoPanel      enemyInfo;
        [SerializeField] private TextMeshProUGUI     enemyStatusLabel;

        [Header("Effects")]
        [Tooltip("Soft round sprite used for spell orbs, sparks and impact bursts (fallback when a sprite effect is missing).")]
        [SerializeField] private Sprite        fxSprite;
        [Tooltip("Plays the sprite-sheet effects below.")]
        [SerializeField] private VfxPlayer     vfxPlayer;
        [SerializeField] private VfxDefinition heroImpactVfx;
        [SerializeField] private VfxDefinition heroBoltVfx;
        [SerializeField] private VfxDefinition heroKidlatVfx;
        [SerializeField] private VfxDefinition healVfx;
        [Tooltip("Shaken on big hits (usually the battle stage).")]
        [SerializeField] private RectTransform shakeTarget;

        [Header("Stage")]
        [SerializeField] private ParallaxBackground background;
        [SerializeField] private float playerHomeX    = -560f;
        [SerializeField] private float enemyHomeX     =  560f;
        [SerializeField] private float groundY        =   30f;
        [SerializeField] private float meleeGap       =  230f;
        [SerializeField] private float walkSpeed      = 1150f;
        [SerializeField] private float travelDistance = 1100f;
        [SerializeField] private float offscreenX     = 1250f;

        [Header("Board")]
        [SerializeField] private LetterGrid  letterGrid;
        [SerializeField] private CanvasGroup gridGroup;
        [Tooltip("Fixed 16-letter board for the first enemy (used by the tutorial). Leave empty for random.")]
        [SerializeField] private string      firstBoard = "WORDATESLINGBUKA";
        [SerializeField] private int         minWordsOnBoard = 8;

        [Header("Buttons")]
        [SerializeField] private Button attackButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button scrambleButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button hintButton;
        [Tooltip("Hints available per chapter. Each one lights up a short everyday word on the board.")]
        [SerializeField] private int    hintsPerChapter = 2;
        [Tooltip("Against regular enemies, hints never suggest words longer than this, so they cannot one-shot a minion.")]
        [SerializeField] private int    maxHintLength = 5;
        [Tooltip("Against bosses (big HP pools) hints may suggest long words. 0 = no limit: the longest everyday word on the board.")]
        [SerializeField] private int    maxHintLengthBoss = 0;
        [Tooltip("Scrambles allowed against each enemy.")]
        [SerializeField] private int    scramblesPerEnemy = 3;

        [Header("Turn feedback")]
        [SerializeField] private TurnIndicator turnIndicator;
        [SerializeField] private EnemyIntentUI intentUI;


        [Header("Orasyon (the book's prayer)")]
        [SerializeField] private OrasyonMeter  orasyonMeter;
        [SerializeField] private VfxDefinition flurryHitVfx;
        [Tooltip("Damage of EACH blow of the prayer (it strikes 5 times, so the total is 5x this). Ignores the enemy's guard.")]
        [SerializeField] private int           orasyonHitDamage = 5;
        [SerializeField] private int           orasyonHits = 5;

        [Header("Potions")]
        [Tooltip("Power potions (double damage) a single enemy fight can award, so bosses cannot be skipped by stacking them.")]
        [SerializeField] private int maxPowerPotionsPerEnemy = 2;
        [SerializeField] private BuffInventory buffs;
        [SerializeField] private int           healingPotionAmount = 10;   // one heart

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackLabel;
        [SerializeField] private TextMeshProUGUI damagePopupTemplate;

        [Header("Result")]
        [SerializeField] private GameObject      resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitle;
        [SerializeField] private TextMeshProUGUI resultBody;
        [SerializeField] private Button          retryButton;
        [SerializeField] private Button          resultMenuButton;
        [SerializeField] private string          mainMenuScene = "MainMenu";

        [Header("Treasure (boss drop)")]
        [Tooltip("Permanent item the chapter boss drops. Empty = no drop.")]
        [SerializeField] private TreasureDefinition bossReward;
        [SerializeField] private TreasureShelf      treasureShelf;
        [SerializeField] private Image              dropIconTemplate;
        [SerializeField] private GameObject         rewardGroup;
        [SerializeField] private Image              rewardIcon;
        [SerializeField] private TextMeshProUGUI    rewardName;
        [SerializeField] private TextMeshProUGUI    rewardEffect;

        [Header("Story")]
        [Tooltip("Dialogue played after the chapter is won (it then loads the next chapter). Empty = Play Again.")]
        [SerializeField] private Orasyon.Story.StoryScript nextStory;
        [TextArea(2, 3)]
        [SerializeField] private string victoryMessage = "The way ahead is clear.";

        /// <summary>Fired when the player commits a word (before the attack plays).</summary>
        public event Action<string> OnPlayerAttack;
        /// <summary>Fired when control returns to the player.</summary>
        public event Action OnTurnFinished;

        // ── Runtime ──────────────────────────────────────────────────────────
        private readonly System.Random rng = new System.Random();
        private int  playerHP;
        private int  playerMaxHP;
        private bool newTreasure;
        private int  enemyHP;
        private int  enemyIndex;
        private bool busy;
        private bool battleOver;
        private int  shieldCharges;
        private int  damageMultiplier = 1;
        private bool gridLocked;
        private bool actionsLocked;

        private int  hintsLeft;
        private int  scramblesLeft;
        private int  powerPotionsAwarded;
        private WordPreview wordPreview;

        // Enemy intent (the next move is chosen at the end of the enemy's turn and shown above its head)
        private EnemyAttack nextAttack;
        private EnemyAttack previousAttack, olderAttack;
        private int         enemyTurnIndex;

        // Status effects
        private int  poisonTurns, poisonDamage;    // on the player
        private int  weakenTurns, weakenPercent;   // on the player
        private bool stunned, stunImmune;          // on the player (immune right after a stun, so it can't chain)
        private int  guardTurns, guardPercent;     // on the enemy
        private Vector2 shakeBase;
        private bool    shaking;

        private static readonly Color MagicGreen = new Color(0.55f, 1f, 0.45f, 1f);
        private static readonly Color MagicGold  = new Color(1f, 0.85f, 0.35f, 1f);

        private CharacterDefinition CurrentEnemy =>
            enemies != null && enemyIndex < enemies.Length ? enemies[enemyIndex] : null;

        private RectTransform PlayerRect => (RectTransform)playerAnimator.transform;
        private RectTransform EnemyRect  => (RectTransform)enemyAnimator.transform;

        // ── Setup ────────────────────────────────────────────────────────────
        private void Start()
        {
            WordDictionary.EnsureLoaded();

            if (chapterLabel != null) chapterLabel.text = chapterTitle;
            if (playerNameLabel != null) playerNameLabel.text = player.displayName;
            playerMaxHP = player.maxHP + PlayerProgress.BonusMaxHP;   // treasures add hearts
            playerHP = playerMaxHP;
            SetupAnimator(playerAnimator, player);
            SetX(PlayerRect, player, playerHomeX - offscreenX);

            enemyIndex = 0;
            LoadEnemy(enemyIndex);
            SetX(EnemyRect, CurrentEnemy, enemyHomeX);

            if (!string.IsNullOrEmpty(firstBoard) && firstBoard.Length >= letterGrid.Rows * letterGrid.Cols)
                {
                    letterGrid.SetBoard(ParseBoard(firstBoard), dropIn: true);
                    // The tutorial introduces special tiles: a gold tile away from the sample word (row 3, col 3 of the first board)
                    if (chapterNumber <= 1) letterGrid.SetKind(letterGrid.Index(2, 2), TileKind.Gold);
                }
            else
                NewBoard();

            attackButton.onClick.AddListener(OnAttackPressed);
            clearButton.onClick.AddListener(letterGrid.ClearSelection);
            scrambleButton.onClick.AddListener(OnScramblePressed);
            if (orasyonMeter != null && orasyonMeter.Button != null) orasyonMeter.Button.onClick.AddListener(OnOrasyonPressed);
            menuButton.onClick.AddListener(LoadMainMenu);
            hintsLeft = hintsPerChapter + PlayerProgress.BonusHints;   // Kampana ng Gubat
            buffs.Add(BuffType.WardingPotion, PlayerProgress.StartingWardingPotions);   // Agimat ng Bubong
            if (hintButton != null) hintButton.onClick.AddListener(OnHintPressed);
            RefreshHintLabel();
            retryButton.onClick.AddListener(Retry);
            resultMenuButton.onClick.AddListener(LoadMainMenu);
            buffs.OnBuffUsed += UseBuff;
            if (treasureShelf != null)
                treasureShelf.OnTreasureClicked += t => ShowFeedback($"<b>{t.displayName}</b>  <color=#9FE2BF>{t.EffectText}</color>\n{t.description}", true);
            if (dropIconTemplate != null) dropIconTemplate.gameObject.SetActive(false);
            if (rewardGroup != null) rewardGroup.SetActive(false);
            letterGrid.OnSelectionChanged += RefreshInput;

            WordPreview.DamageEstimator = PreviewDamage;
            WordPreview.DamageDescriber = DescribeDamage;
            wordPreview = FindAnyObjectByType<WordPreview>();
            if (shakeTarget != null) shakeBase = shakeTarget.anchoredPosition;

            resultPanel.SetActive(false);
            damagePopupTemplate.gameObject.SetActive(false);
            ShowFeedback("", true);
            RefreshHearts();
            RefreshStatus();
            StartCoroutine(IntroRoutine());
        }

        /// <summary>Used by the tutorial to restrict input while it explains things.</summary>
        public void SetTutorialLocks(bool lockGrid, bool lockActions)
        {
            gridLocked = lockGrid;
            actionsLocked = lockActions;
            RefreshInput();
        }

        private IEnumerator IntroRoutine()
        {
            busy = true;
            RefreshInput();
            yield return WalkTo(playerAnimator, player, playerHomeX);
            busy = false;
            RefreshInput();
            ShowIntent();
            if (turnIndicator != null) turnIndicator.ShowPlayerTurn();
        }

        // ── Player actions ───────────────────────────────────────────────────
        private bool CanAct => !busy && !battleOver && !actionsLocked && CurrentEnemy != null;

        private void OnAttackPressed()
        {
            if (!CanAct) return;
            if (!letterGrid.CurrentWordIsValid) { RejectWord(); return; }
            var fx = TileEffects.From(letterGrid.SelectedTiles);   // gold / heal / silver / ward / hex tiles in this word
            string word = letterGrid.ConsumeSelection();
            letterGrid.ClearHighlights();   // any hint glow goes away once you attack
            StartCoroutine(AttackRoutine(word, fx));
        }

        /// <summary>
        /// A word that is not in the book: the preview tiles flash red and shake, the selection stays,
        /// and the turn is not used.
        /// </summary>
        private void RejectWord()
        {
            string word = letterGrid.CurrentWord;
            if (word.Length == 0) return;
            string msg = word.Length < WordDictionary.MinLength ? "Need 3+ letters" : "Not a word";
            ShowFeedback($"<color=#FF8A7A>{msg}</color>", false);
            if (wordPreview != null) wordPreview.Reject();
        }

        /// <summary>Lights up the tiles of a short everyday word the player can make.</summary>
        private void OnHintPressed()
        {
            if (!CanAct || hintsLeft <= 0) return;

            int cap = CurrentEnemy != null && CurrentEnemy.isBoss ? maxHintLengthBoss : maxHintLength;
            string word = letterGrid.FindHintWord(cap > 0 ? cap : int.MaxValue);
            if (word == null)
            {
                // Costs nothing: the hint is only spent when a word is shown
                ShowFeedback("Lola can't find a word here. Try Scramble.", true);
                return;
            }

            hintsLeft--;
            RefreshHintLabel();
            letterGrid.HighlightWord(word);
            ShowFeedback($"Lola whispers through the book: <b>\"{word}\"</b>  <color=#9FE2BF>({hintsLeft} hint{(hintsLeft == 1 ? "" : "s")} left)</color>", true);
            RefreshInput();
        }

        private void RefreshHintLabel()
        {
            if (hintButton == null) return;
            var label = hintButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"HINT ({hintsLeft})";
        }

        /// <summary>Shuffles the tiles still on the board. Free: the letters stay the same, only their places change.</summary>
        private void OnScramblePressed()
        {
            if (!CanAct) return;
            if (scramblesLeft <= 0) { ShowFeedback("No scrambles left against this foe.", false); return; }
            scramblesLeft--;
            RefreshScrambleLabel();
            letterGrid.Scramble();
            ShowFeedback($"You shuffle the letters.  <color=#9FE2BF>({scramblesLeft} scramble{(scramblesLeft == 1 ? "" : "s")} left)</color>", true);
            RefreshInput();
        }

        private void RefreshScrambleLabel()
        {
            if (scrambleButton == null) return;
            var label = scrambleButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = $"SCRAMBLE ({scramblesLeft})";
        }

        private void UseBuff(BuffType type)
        {
            switch (type)
            {
                case BuffType.HealingPotion:
                    int healed = Mathf.Min(healingPotionAmount, playerMaxHP - playerHP);
                    playerHP += healed;
                    playerAnimator.PlayOnce(player.cast);
                    if (vfxPlayer != null) vfxPlayer.Play(healVfx, PlayerCenter, 1.1f);
                    Popup(PlayerRect, "+" + healed, new Color(1f, 0.45f, 0.45f));
                    ShowFeedback($"Healing Potion restores {healed} HP.", true);
                    break;
                case BuffType.WardingPotion:
                    // Like Bookworm's green potion: cures ailments, plus a ward against the next attack
                    bool cured = poisonTurns > 0 || weakenTurns > 0;
                    poisonTurns = 0; weakenTurns = 0;
                    shieldCharges++;
                    int unveiled = letterGrid.ClearBadTiles(smokeAndLockOnly: true);   // smoke and locks only; Orasyon clears the rest
                    playerAnimator.PlayOnce(player.cast);
                    Popup(PlayerRect, "Warded", new Color(0.45f, 1f, 0.5f));
                    ShowFeedback((cured ? "Your ailments fade. " : "") + (unveiled > 0 ? "The smoke and locks lift. " : "") +
                                 "A green ward surrounds you. The next attack will be blocked.", true);
                    break;
                case BuffType.PowerPotion:
                    damageMultiplier = 2;
                    playerAnimator.PlayOnce(player.cast);
                    Popup(PlayerRect, "Power!", new Color(0.5f, 0.75f, 1f));
                    ShowFeedback("Power surges through you. Your next word deals double damage!", true);
                    break;
            }
            RefreshHearts();
            RefreshStatus();
        }

        // ── Turns ────────────────────────────────────────────────────────────
        private IEnumerator AttackRoutine(string word, TileEffects fx)
        {
            busy = true;
            RefreshInput();
            OnPlayerAttack?.Invoke(word);

            var enemy = CurrentEnemy;
            int damage = ComputeDamage(word, fx, out bool weakness);
            bool empowered = damageMultiplier > 1;
            bool weakened  = weakenTurns > 0;
            bool guarded   = guardTurns > 0;
            damageMultiplier = 1;
            if (weakenTurns > 0) weakenTurns--;   // these last a number of words
            if (guardTurns > 0)  guardTurns--;
            RefreshStatus();

            // Longer words look (and hit) bigger: 3-4 letters = staff swing, 5-6 = spell, 7+ = brilliant spell
            int tier = word.Length >= 7 ? 3 : word.Length >= 5 ? 2 : 1;
            ShowFeedback(tier == 3 ? $"<b>{word}</b>!  <color=#FFD700>Brilliant!</color>"
                       : tier == 2 ? $"<b>{word}</b>!  <color=#9CFF7A>The book glows...</color>"
                       : $"<b>{word}</b>!", true);

            void Hit()
            {
                enemyHP = Mathf.Max(0, enemyHP - damage);
                RefreshHearts();
                PlayHurt(enemyAnimator, enemy);
                Popup(EnemyRect, "-" + damage, new Color(1f, 0.85f, 0.3f), 1f + 0.25f * (tier - 1));
                if (vfxPlayer != null && heroImpactVfx != null)
                    vfxPlayer.Play(heroImpactVfx, EnemyCenter, 0.9f + 0.35f * (tier - 1));
                else
                    StartCoroutine(Burst(EnemyCenter, 140f + tier * 70f, tier == 1 ? new Color(1f, 0.95f, 0.75f, 0.9f) : tier == 3 ? MagicGold : MagicGreen));
                if (tier == 3)
                {
                    StartCoroutine(Shake(0.35f, 16f));
                    PopupAt((PlayerRect.anchoredPosition + EnemyRect.anchoredPosition) * 0.5f + new Vector2(0f, 330f), "BRILLIANT!", MagicGold, 1.7f);
                }

                string msg = $"<b>{word}</b> deals {damage} damage!";
                if (weakness)  msg += "  <color=#9CFF7A>Weakness!</color>";
                if (empowered) msg += "  <color=#7FB2FF>Power x2!</color>";
                if (weakened)  msg += "  <color=#C9A2FF>(weakened)</color>";
                if (guarded)   msg += "  <color=#8CC8FF>(guarded)</color>";
                bool powerAllowed = powerPotionsAwarded < maxPowerPotionsPerEnemy;
                var reward = buffs.AwardForWord(word.Length + PlayerProgress.PotionLengthBonus, powerAllowed);   // Mata ng Buwan
                if (reward != null && reward.type == BuffType.PowerPotion) powerPotionsAwarded++;
                if (reward != null) msg += $"\n<color=#9FE2BF>You found a {reward.displayName}!</color>";
                ShowFeedback(msg, true);
            }

            if (tier == 1)
            {
                // Walk up to the enemy and strike with the staff
                yield return WalkTo(playerAnimator, player, enemyHomeX - meleeGap);
                float swing = playerAnimator.Duration(player.attack);
                playerAnimator.PlayOnce(player.attack);
                yield return new WaitForSeconds(swing * 0.55f);
                Hit();
                yield return new WaitForSeconds(swing * 0.45f);
                yield return WalkTo(playerAnimator, player, playerHomeX);
            }
            else if (tier == 3 && vfxPlayer != null && heroKidlatVfx != null)
            {
                // Long words call down lightning (kidlat) on the enemy
                var clip = player.kidlat != null && player.kidlat.Length > 0 ? player.kidlat
                         : player.cast != null && player.cast.Length > 0 ? player.cast : player.attack;
                float castTime = playerAnimator.Duration(clip);
                playerAnimator.PlayOnce(clip);
                StartCoroutine(Burst(PlayerStaff, 200f, MagicGold));
                yield return new WaitForSeconds(castTime * 0.55f);
                vfxPlayer.Play(heroKidlatVfx, EnemyRect.anchoredPosition, 1f);
                yield return new WaitForSeconds(0.18f);
                Hit();
                yield return new WaitForSeconds(Mathf.Max(0.45f, castTime * 0.45f));
            }
            else
            {
                // Cast from where he stands: raise the staff, a spell orb flies to the enemy
                var clip = player.cast != null && player.cast.Length > 0 ? player.cast : player.attack;
                float castTime = playerAnimator.Duration(clip);
                playerAnimator.PlayOnce(clip);
                StartCoroutine(Burst(PlayerStaff, tier == 3 ? 200f : 130f, tier == 3 ? MagicGold : MagicGreen));
                yield return new WaitForSeconds(castTime * 0.5f);
                if (vfxPlayer != null && heroBoltVfx != null)
                    yield return vfxPlayer.TravelRoutine(heroBoltVfx, PlayerStaff, EnemyCenter, 0.35f, tier == 3 ? 1.5f : 1f);
                else
                    yield return Projectile(tier);
                Hit();
                yield return new WaitForSeconds(Mathf.Max(0.35f, castTime * 0.5f));
            }

            // The tiles used in the word do their thing, and the book gains charge
            if (orasyonMeter != null) orasyonMeter.Add(word.Length);
            ApplyWordTileEffects(fx);
            if (playerHP <= 0 && CheckPlayerDefeat()) yield break;   // a Sumpa (hex) tile can be fatal

            // Enemy defeated → next encounter (or victory)
            if (enemyHP <= 0)
            {
                yield return ResolveEnemyDefeat(enemy);
                yield break;
            }

            // Bookworm style: the used tiles are replaced right away, so the board is always full
            if (LetterGrid.RefillAfterEachWord)
            {
                letterGrid.RefillEmpty(Mathf.Min(minWordsOnBoard, 4));
                MaybeDropSpecialTile(word.Length);
            }

            yield return new WaitForSeconds(0.25f);
            if (turnIndicator != null) turnIndicator.ShowEnemyTurn();
            yield return EnemyPhase(enemy);
            if (CheckPlayerDefeat()) yield break;
            FinishTurn();
        }

        /// <summary>The enemy is down: death animation, then the next encounter, or the treasure and the victory screen.</summary>
        private IEnumerator ResolveEnemyDefeat(CharacterDefinition enemy)
        {
            yield return DefeatRoutine(enemyAnimator, enemy, fadeOut: true);
            enemyIndex++;
            if (CurrentEnemy == null)
            {
                battleOver = true;
                RefreshInput();
                HideIntent();
                if (turnIndicator != null) turnIndicator.Hide();
                if (bossReward != null)
                {
                    newTreasure = PlayerProgress.Grant(bossReward);
                    yield return TreasureDropRoutine(bossReward);
                }
                PlayerProgress.CompleteStage(chapterNumber);
                ShowResult(victory: true);
                yield break;
            }
            yield return TravelRoutine();
            FinishTurn();
        }

        // ── Special tiles ────────────────────────────────────────────────────
        /// <summary>Healing leaves, wards and hexes take effect once their word has been played.</summary>
        private void ApplyWordTileEffects(TileEffects fx)
        {
            if (fx.heal > 0)
            {
                int healed = Mathf.Min(fx.heal * TileKindInfo.HealAmount, playerMaxHP - playerHP);
                if (healed > 0)
                {
                    playerHP += healed;
                    if (vfxPlayer != null) vfxPlayer.Play(healVfx, PlayerCenter, 1.0f);
                    Popup(PlayerRect, "+" + healed, new Color(0.55f, 1f, 0.55f));
                }
            }
            if (fx.shield > 0)
            {
                shieldCharges += fx.shield;
                Popup(PlayerRect, "Agimat!", new Color(0.55f, 0.78f, 1f));
            }
            if (fx.hex > 0)
            {
                int cost = fx.hex * TileKindInfo.HexDamage;
                playerHP = Mathf.Max(0, playerHP - cost);
                playerAnimator.Flash(new Color(0.75f, 0.5f, 1f), 0.3f);
                Popup(PlayerRect, "Sumpa -" + cost, new Color(0.78f, 0.55f, 1f));
            }
            if (fx.heal > 0 || fx.shield > 0 || fx.hex > 0)
            {
                RefreshHearts();
                RefreshStatus();
            }
        }

        /// <summary>The good tiles that can appear in this chapter.</summary>
        private TileKind[] SpecialPool =>
            chapterNumber <= 1 ? new[] { TileKind.Gold }
          : chapterNumber == 2 ? new[] { TileKind.Gold, TileKind.Heal }
          : new[] { TileKind.Gold, TileKind.Heal, TileKind.Silver, TileKind.Shield };

        /// <summary>A fresh board gets 1 special tile in chapter 1 and 1-2 afterwards (at most 3 on the board).</summary>
        private void SpawnBoardSpecials()
        {
            var pool = SpecialPool;
            int count = chapterNumber <= 1 ? 1 : rng.Next(1, 3);
            for (int i = 0; i < count; i++) letterGrid.TryAddSpecial(pool[rng.Next(pool.Length)]);
        }

        /// <summary>Long words are rewarded: 5-6 letters drop a special tile 50% of the time, 7+ letters always.</summary>
        private void MaybeDropSpecialTile(int wordLength)
        {
            double chance = wordLength >= 7 ? 1.0 : wordLength >= 5 ? 0.5 : 0.0;
            if (chance <= 0.0 || rng.NextDouble() >= chance) return;
            var pool = SpecialPool;
            letterGrid.TryAddSpecial(pool[rng.Next(pool.Length)], onlyRefilled: true);
        }

        // ── Orasyon ──────────────────────────────────────────────────────────
        private void OnOrasyonPressed()
        {
            if (!CanAct || orasyonMeter == null || !orasyonMeter.IsFull) return;
            StartCoroutine(OrasyonRoutine());
        }

        /// <summary>
        /// The book's prayer: a free action. Fixed damage that ignores guard, cures poison and weakness,
        /// and clears every bad tile. It does not end the player's turn.
        /// </summary>
        private IEnumerator OrasyonRoutine()
        {
            busy = true;
            orasyonMeter.ResetCharge();
            RefreshInput();
            letterGrid.ClearSelection();

            var enemy = CurrentEnemy;
            bool cured = poisonTurns > 0 || weakenTurns > 0;
            poisonTurns = 0; weakenTurns = 0;
            int cleared = letterGrid.ClearBadTiles(smokeAndLockOnly: false);
            RefreshStatus();
            ShowFeedback("<color=#FFD700><b>ORASYON!</b></color>  The book answers your prayer." +
                         (cured ? "  <color=#9FE2BF>Your ailments fade.</color>" : "") +
                         (cleared > 0 ? "  <color=#9FE2BF>The board is cleansed.</color>" : ""), true);

            // Like a short word: run up to the enemy and strike it up close
            yield return WalkTo(playerAnimator, player, enemyHomeX - meleeGap);

            var clip = player.flurry != null && player.flurry.Length > 0 ? player.flurry
                     : player.cast != null && player.cast.Length > 0 ? player.cast : player.attack;
            float dur = playerAnimator.Duration(clip);
            playerAnimator.PlayOnce(clip);
            StartCoroutine(Burst(PlayerStaff, 240f, MagicGold));
            StartCoroutine(Shake(0.3f, 8f));

            // The blows are spread through the middle of the animation
            int hits = Mathf.Max(1, orasyonHits);
            yield return new WaitForSeconds(dur * 0.25f);
            for (int i = 0; i < hits; i++)
            {
                int part = orasyonHitDamage;
                enemyHP = Mathf.Max(0, enemyHP - part);
                RefreshHearts();
                PlayHurt(enemyAnimator, enemy);
                Vector2 jitter = new Vector2(UnityEngine.Random.Range(-60f, 60f), UnityEngine.Random.Range(-40f, 60f));
                if (vfxPlayer != null && flurryHitVfx != null) vfxPlayer.Play(flurryHitVfx, EnemyCenter + jitter, 0.9f);
                else StartCoroutine(Burst(EnemyCenter + jitter, 170f, MagicGold));
                Popup(EnemyRect, "-" + part, MagicGold);
                yield return new WaitForSeconds(dur * 0.5f / hits);
            }
            yield return new WaitForSeconds(dur * 0.25f);
            yield return WalkTo(playerAnimator, player, playerHomeX);

            if (enemyHP <= 0)
            {
                yield return ResolveEnemyDefeat(enemy);
                yield break;
            }
            busy = false;
            RefreshInput();
        }

        /// <summary>
        /// The enemy acts, then the player's turn begins: poison ticks, and if stunned the player
        /// loses the turn and the enemy acts again (once; a stun can't chain).
        /// </summary>
        private IEnumerator EnemyPhase(CharacterDefinition enemy)
        {
            yield return EnemyTurnRoutine(enemy);
            while (playerHP > 0)
            {
                yield return PoisonTick();
                if (playerHP <= 0 || !stunned) break;

                stunned = false;
                stunImmune = true;
                RefreshStatus();
                Popup(PlayerRect, "Stunned!", new Color(1f, 0.88f, 0.4f), 1.2f);
                ShowFeedback("<color=#FFE066>You are stunned and lose your turn!</color>", false);
                yield return new WaitForSeconds(1f);
                yield return EnemyTurnRoutine(enemy);
            }
            stunImmune = false;
        }

        private IEnumerator PoisonTick()
        {
            if (poisonTurns <= 0) yield break;
            poisonTurns--;
            playerHP = Mathf.Max(0, playerHP - poisonDamage);
            playerAnimator.Flash(new Color(0.55f, 1f, 0.5f), 0.25f);
            Popup(PlayerRect, "Poison -" + poisonDamage, new Color(0.55f, 1f, 0.45f));
            if (feedbackLabel != null) feedbackLabel.text += $"\n<color=#9CFF7A>Poison deals {poisonDamage} damage.</color>";
            RefreshHearts();
            RefreshStatus();
            yield return new WaitForSeconds(0.7f);
        }

        private IEnumerator EnemyTurnRoutine(CharacterDefinition enemy)
        {
            var atk = nextAttack ?? PickNextAttack(enemy);
            HideIntent();
            yield return EnemyActRoutine(enemy, atk);
            yield return TileTickRoutine();

            // Choose and show the next move at the END of this turn, so the player can plan for it
            olderAttack = previousAttack;
            previousAttack = atk;
            enemyTurnIndex++;
            nextAttack = PickNextAttack(enemy);
            if (playerHP > 0 && enemyHP > 0) ShowIntent();
        }

        /// <summary>End of an enemy turn: every Alab (burning) tile hurts you, and timed bad tiles wear off.</summary>
        private IEnumerator TileTickRoutine()
        {
            int burning = letterGrid.CountKind(TileKind.Burning);
            if (burning > 0 && playerHP > 0)
            {
                int dmg = burning * TileKindInfo.BurnDamage;
                playerHP = Mathf.Max(0, playerHP - dmg);
                playerAnimator.Flash(new Color(1f, 0.55f, 0.25f), 0.3f);
                Popup(PlayerRect, "Alab -" + dmg, new Color(1f, 0.6f, 0.3f));
                if (feedbackLabel != null) feedbackLabel.text += $"\n<color=#FFA866>{burning} burning tile{(burning > 1 ? "s" : "")} scorch you for {dmg}.</color>";
                RefreshHearts();
                yield return new WaitForSeconds(0.6f);
            }
            letterGrid.TickTileTimers();
        }

        private IEnumerator EnemyActRoutine(CharacterDefinition enemy, EnemyAttack atk)
        {
            float strike = enemyAnimator.Duration(enemy.attack);

            // Self-buff (guard, heal): performed in place
            if (atk.IsSelfBuff)
            {
                ShowFeedback($"The {enemy.displayName} uses <b>{atk.name}</b>!", false);
                enemyAnimator.PlayOnce(enemy.attack);
                yield return new WaitForSeconds(strike * 0.5f);
                if (vfxPlayer != null && atk.vfx != null)
                    vfxPlayer.Play(atk.vfx, EnemyCenter, 1.1f);
                else
                    StartCoroutine(Burst(EnemyCenter, 260f, atk.effect == AttackEffect.Leech ? new Color(1f, 0.55f, 0.7f, 0.9f) : new Color(0.55f, 0.8f, 1f, 0.9f)));
                ShowFeedback($"The {enemy.displayName} uses <b>{atk.name}</b>!" + ApplyEffect(enemy, atk, 0), false);
                RefreshHearts();
                RefreshStatus();
                yield return new WaitForSeconds(Mathf.Max(0.4f, strike * 0.5f));
                yield break;
            }

            // Close the distance to the player, strike, then go back
            float home = EnemyRect.anchoredPosition.x;
            float reach = (player.spriteSize + enemy.spriteSize) * 0.5f * 0.75f;
            ShowFeedback($"The {enemy.displayName} uses <b>{atk.name}</b>!", false);
            yield return WalkTo(enemyAnimator, enemy, playerHomeX + reach);

            enemyAnimator.PlayOnce(enemy.attack);
            yield return new WaitForSeconds(strike * 0.6f);

            bool spriteFx = vfxPlayer != null && (atk.vfx != null || atk.hitVfx != null);
            if (vfxPlayer != null && atk.vfx != null)
            {
                if (atk.vfxTravels) yield return vfxPlayer.TravelRoutine(atk.vfx, EnemyCenter, PlayerCenter, 0.3f, 1f);
                else                vfxPlayer.Play(atk.vfx, PlayerCenter, 1.1f);
            }
            if (vfxPlayer != null && atk.hitVfx != null) vfxPlayer.Play(atk.hitVfx, PlayerCenter, 1.1f);

            if (shieldCharges > 0)
            {
                shieldCharges--;
                Popup(PlayerRect, "Blocked!", new Color(0.55f, 0.8f, 1f));
                StartCoroutine(Burst(PlayerCenter, 300f, new Color(0.45f, 1f, 0.5f, 0.8f)));
                ShowFeedback($"Your ward blocks {atk.name}!", true);
            }
            else
            {
                int dmg = atk.RollDamage(rng);
                bool crit = rng.NextDouble() < atk.critChance;
                if (crit) dmg *= 2;
                playerHP = Mathf.Max(0, playerHP - dmg);
                PlayHurt(playerAnimator, player);
                if (!spriteFx) StartCoroutine(Burst(PlayerCenter, crit ? 300f : 180f, new Color(1f, 0.45f, 0.35f, 0.85f)));
                if (crit)
                {
                    Popup(PlayerRect, "CRIT -" + dmg, new Color(1f, 0.3f, 0.25f), 1.45f);
                    StartCoroutine(Shake(0.3f, 14f));
                }
                else
                {
                    Popup(PlayerRect, "-" + dmg, new Color(1f, 0.4f, 0.35f));
                }

                string msg = (crit ? "<color=#FF6B6B>Critical hit!</color>  " : "") +
                             $"The {enemy.displayName} uses <b>{atk.name}</b>! -{dmg} HP";
                if (atk.effect != AttackEffect.None && rng.NextDouble() < atk.effectChance)
                    msg += ApplyEffect(enemy, atk, dmg);
                ShowFeedback(msg, false);
            }
            RefreshHearts();
            RefreshStatus();

            yield return new WaitForSeconds(strike * 0.4f);
            yield return WalkTo(enemyAnimator, enemy, home);
        }

        /// <summary>
        /// Chooses the enemy's next move. Bosses use their signature move on a fixed cycle; every other turn is
        /// weighted random, never the same move three times in a row and never a stun right after a stun.
        /// </summary>
        private EnemyAttack PickNextAttack(CharacterDefinition enemy)
        {
            var list = enemy.GetAttacks();
            if (enemy.signatureEvery > 0 && (enemyTurnIndex + 1) % enemy.signatureEvery == 0)
                foreach (var a in list)
                    if (a.isSignature) return a;

            for (int tries = 0; tries < 12; tries++)
            {
                var a = PickAttack(list);
                if (a == previousAttack && a == olderAttack) continue;
                if (a.effect == AttackEffect.Stun && previousAttack != null && previousAttack.effect == AttackEffect.Stun) continue;
                return a;
            }
            return PickAttack(list);
        }

        /// <summary>Weighted random pick; signature moves are only used on the boss cycle.</summary>
        private EnemyAttack PickAttack(EnemyAttack[] list)
        {
            float total = 0f;
            foreach (var a in list) if (!a.isSignature) total += Mathf.Max(0f, a.weight);
            if (total <= 0f) return list[0];

            double roll = rng.NextDouble() * total;
            foreach (var a in list)
            {
                if (a.isSignature) continue;
                roll -= Mathf.Max(0f, a.weight);
                if (roll < 0) return a;
            }
            return list[list.Length - 1];
        }

        private void ShowIntent()
        {
            var enemy = CurrentEnemy;
            if (enemy == null || nextAttack == null) return;
            if (intentUI != null) intentUI.Show(nextAttack);
            if (enemyInfo != null) enemyInfo.Show(enemy, nextAttack);
        }

        private void HideIntent()
        {
            if (intentUI != null) intentUI.Hide();
        }

        /// <summary>Applies an attack's status effect and returns the extra feedback line.</summary>
        private string ApplyEffect(CharacterDefinition enemy, EnemyAttack atk, int damageDealt)
        {
            string c = EnemyAttack.EffectColor(atk.effect);
            switch (atk.effect)
            {
                case AttackEffect.Poison:
                    poisonTurns  = Mathf.Max(poisonTurns, atk.effectTurns);
                    poisonDamage = Mathf.Max(poisonDamage, atk.effectPower);
                    playerAnimator.Flash(new Color(0.55f, 1f, 0.5f), 0.3f);
                    return $"\n<color={c}>You are poisoned! ({poisonDamage} per turn, {poisonTurns} turns)</color>";

                case AttackEffect.Stun:
                    if (stunImmune) return "";
                    stunned = true;
                    return $"\n<color={c}>You are stunned!</color>";

                case AttackEffect.Weaken:
                    weakenTurns   = Mathf.Max(weakenTurns, atk.effectTurns);
                    weakenPercent = Mathf.Max(weakenPercent, atk.effectPower);
                    return $"\n<color={c}>You feel weakened! Your next {weakenTurns} words deal -{weakenPercent}% damage.</color>";

                case AttackEffect.Guard:
                    guardTurns   = atk.effectTurns;
                    guardPercent = atk.effectPower;
                    Popup(EnemyRect, "Guard", new Color(0.55f, 0.8f, 1f));
                    return $"\n<color={c}>It takes -{guardPercent}% damage from your next {guardTurns} words.</color>";

                case AttackEffect.Leech:
                    int heal = damageDealt > 0 ? Mathf.Max(1, damageDealt * atk.effectPower / 100) : atk.effectPower;
                    heal = Mathf.Min(heal, enemy.maxHP - enemyHP);
                    if (heal <= 0) return "";
                    enemyHP += heal;
                    Popup(EnemyRect, "+" + heal, new Color(1f, 0.55f, 0.7f));
                    return $"\n<color={c}>The {enemy.displayName} heals {heal} HP!</color>";
            }
            return "";
        }

        private void ClearStatuses()
        {
            poisonTurns = weakenTurns = guardTurns = 0;
            stunned = stunImmune = false;
            RefreshStatus();
        }

        // ── Damage ───────────────────────────────────────────────────────────
        private int ComputeDamage(string word, TileEffects fx, out bool weakness)
        {
            weakness = IsThemeWord(CurrentEnemy, word);
            // treasures and gold tiles share one additive bonus group
            float d = LetterScoring.Damage(word, fx.silver) * (PlayerProgress.DamageMultiplier + TileKindInfo.GoldBonus * fx.gold);
            if (weakness) d *= 1.5f;
            d *= damageMultiplier;
            if (weakenTurns > 0) d *= 1f - weakenPercent / 100f;
            if (guardTurns > 0)  d *= 1f - guardPercent / 100f;
            return Mathf.Max(1, Mathf.RoundToInt(d));
        }

        private int PreviewDamage(string word) => ComputeDamage(word, TileEffects.From(letterGrid.SelectedTiles), out _);

        /// <summary>"11 damage (base 7 x1.5 weakness x1.2 treasures)": teaches the system and helps balancing.</summary>
        private string DescribeDamage(string word)
        {
            var fx = TileEffects.From(letterGrid.SelectedTiles);
            int total = ComputeDamage(word, fx, out bool weakness);
            var parts = new System.Collections.Generic.List<string> { $"base {LetterScoring.Damage(word, fx.silver)}" };
            if (fx.SilverCount > 0) parts[0] += $" (silver x{fx.SilverCount})";
            float bonus = PlayerProgress.DamageMultiplier + TileKindInfo.GoldBonus * fx.gold;
            if (bonus > 1f)         parts.Add($"x{bonus:0.##}" + (fx.gold > 0 ? " treasures+gold" : " treasures"));
            if (weakness)           parts.Add("x1.5 weakness");
            if (damageMultiplier > 1) parts.Add($"x{damageMultiplier} power");
            if (weakenTurns > 0)    parts.Add($"x{1f - weakenPercent / 100f:0.##} weakened");
            if (guardTurns > 0)     parts.Add($"x{1f - guardPercent / 100f:0.##} guarded");
            return $"{total} damage  <size=70%><color=#D8CCA6>({string.Join("  ", parts)})</color></size>";
        }

        private void OnDestroy()
        {
            if (WordPreview.DamageEstimator == (Func<string, int>)PreviewDamage) WordPreview.DamageEstimator = null;
            if (WordPreview.DamageDescriber == (Func<string, string>)DescribeDamage) WordPreview.DamageDescriber = null;
        }

        // ── Visual effects (spells, sparks, shake) ───────────────────────────
        private Vector2 PlayerCenter => PlayerRect.anchoredPosition + new Vector2(0f, player.spriteSize * 0.45f);
        private Vector2 PlayerStaff  => PlayerRect.anchoredPosition + new Vector2(70f, player.spriteSize * 0.75f);
        private Vector2 EnemyCenter  => EnemyRect.anchoredPosition + new Vector2(0f, (CurrentEnemy != null ? CurrentEnemy.spriteSize : 300f) * 0.45f);

        private Image MakeFx(Vector2 pos, float size, Color color)
        {
            var go = new GameObject("FX", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(PlayerRect.parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);
            rt.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = fxSprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A glowing spell orb arcs from the staff to the enemy, leaving sparks.</summary>
        private IEnumerator Projectile(int tier)
        {
            if (fxSprite == null) yield break;
            Color color = tier == 3 ? MagicGold : MagicGreen;
            float size = tier == 3 ? 150f : 95f;
            var orb = MakeFx(PlayerStaff, size, color);
            var core = MakeFx(PlayerStaff, size * 0.45f, new Color(1f, 1f, 0.92f, 1f));
            Vector2 from = PlayerStaff, to = EnemyCenter;
            float duration = tier == 3 ? 0.45f : 0.35f;
            int frame = 0;

            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                Vector2 p = Vector2.Lerp(from, to, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * (tier == 3 ? 110f : 70f);
                float pulse = 1f + 0.15f * Mathf.Sin(t * 30f);
                orb.rectTransform.anchoredPosition = core.rectTransform.anchoredPosition = p;
                orb.rectTransform.localScale = Vector3.one * pulse;
                if (frame++ % 2 == 0)
                {
                    var c = color; c.a = 0.7f;
                    StartCoroutine(FadeFx(MakeFx(p + UnityEngine.Random.insideUnitCircle * 18f, size * 0.4f, c), 0.35f, 0.3f));
                }
                yield return null;
            }
            Destroy(orb.gameObject);
            Destroy(core.gameObject);
        }

        /// <summary>Expanding, fading flash (impacts, casting, guarding).</summary>
        private IEnumerator Burst(Vector2 pos, float size, Color color)
        {
            if (fxSprite == null) yield break;
            var fx = MakeFx(pos, size, color);
            yield return FadeFx(fx, 0.35f, 1.5f);
        }

        private static IEnumerator FadeFx(Image fx, float duration, float endScale)
        {
            Color c = fx.color;
            float startAlpha = c.a;
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                if (fx == null) yield break;
                fx.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, endScale, 1f - (1f - t) * (1f - t));
                c.a = startAlpha * (1f - t);
                fx.color = c;
                yield return null;
            }
            if (fx != null) Destroy(fx.gameObject);
        }

        private IEnumerator Shake(float duration, float magnitude)
        {
            if (shakeTarget == null || shaking) yield break;
            shaking = true;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                shakeTarget.anchoredPosition = shakeBase + UnityEngine.Random.insideUnitCircle * magnitude * (1f - t / duration);
                yield return null;
            }
            shakeTarget.anchoredPosition = shakeBase;
            shaking = false;
        }

        /// <summary>Player walks forward while the background scrolls and the next enemy approaches.</summary>
        private IEnumerator TravelRoutine()
        {
            ClearStatuses();   // ailments and guards fade between fights
            if (turnIndicator != null) turnIndicator.Hide();
            LoadEnemy(enemyIndex);
            var next = CurrentEnemy;
            SetX(EnemyRect, next, enemyHomeX + travelDistance);
            ShowFeedback("", true);

            if (player.walk != null && player.walk.Length > 0) playerAnimator.PlayLoop(player.walk);
            float remaining = travelDistance;
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, walkSpeed * 0.8f * Time.deltaTime);
                remaining -= step;
                if (background != null) background.Scroll(step);
                var p = EnemyRect.anchoredPosition;
                EnemyRect.anchoredPosition = new Vector2(p.x - step, p.y);
                yield return null;
            }
            playerAnimator.ReturnToIdle();

            ShowFeedback(next.isBoss
                ? $"<color=#FF6B6B>BOSS!</color>  The {next.displayName} blocks your path!"
                : $"A {next.displayName} appears!", next.isBoss ? false : true);
            NewBoard();
            ShowIntent();
            yield return new WaitForSeconds(0.5f);
        }

        private bool CheckPlayerDefeat()
        {
            if (playerHP > 0) return false;
            battleOver = true;
            RefreshInput();
            HideIntent();
            if (turnIndicator != null) turnIndicator.Hide();
            StartCoroutine(PlayerDefeatRoutine());
            return true;
        }

        private IEnumerator PlayerDefeatRoutine()
        {
            yield return DefeatRoutine(playerAnimator, player, fadeOut: false);
            ShowResult(victory: false);
        }

        /// <summary>Bookworm-style drop: the treasure pops out where the boss fell, then flies to its shelf slot.</summary>
        private IEnumerator TreasureDropRoutine(TreasureDefinition treasure)
        {
            ShowFeedback($"<color=#FFD700>Destroyed!</color>  The {enemies[enemies.Length - 1].displayName} dropped <b>{treasure.displayName}</b>!", true);
            if (dropIconTemplate == null || treasure.icon == null) { yield return new WaitForSeconds(1f); yield break; }

            var drop = Instantiate(dropIconTemplate, dropIconTemplate.transform.parent);
            drop.gameObject.SetActive(true);
            drop.sprite = treasure.icon;
            var rt = (RectTransform)drop.transform;
            rt.SetAsLastSibling();
            rt.anchoredPosition = EnemyRect.anchoredPosition + new Vector2(0f, EnemyRect.sizeDelta.y * 0.4f);
            Vector3 start = rt.position;

            // Pop up with a little bounce and spin
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.5f)
            {
                float s = Mathf.Sin(t * Mathf.PI * 0.5f);
                rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.2f, 1f, s + 0.15f * Mathf.Sin(t * Mathf.PI));
                rt.position = start + Vector3.up * 90f * s * rt.lossyScale.y;
                rt.localEulerAngles = new Vector3(0f, 0f, (1f - t) * 25f);
                yield return null;
            }
            yield return new WaitForSeconds(0.8f);

            // Fly to the shelf
            var target = treasureShelf != null ? treasureShelf.SlotTarget(treasure) : null;
            if (target != null)
            {
                Vector3 from = rt.position, to = target.position;
                for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
                {
                    float e = t * t * (3f - 2f * t);
                    rt.position = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 120f * rt.lossyScale.y;
                    rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, e);
                    yield return null;
                }
            }
            Destroy(drop.gameObject);
            if (treasureShelf != null) treasureShelf.Refresh();
            yield return new WaitForSeconds(0.4f);
        }

        private IEnumerator DefeatRoutine(UIFrameAnimator anim, CharacterDefinition def, bool fadeOut)
        {
            if (def.death != null && def.death.Length > 0)
            {
                anim.PlayOnce(def.death, holdLast: true);
                yield return new WaitForSeconds(anim.Duration(def.death) + 0.3f);
            }
            if (fadeOut) yield return anim.FadeTo(0f, 0.4f);
        }

        private void FinishTurn()
        {
            EnsurePlayableBoard();
            busy = false;
            RefreshInput();
            if (turnIndicator != null) turnIndicator.ShowPlayerTurn();
            OnTurnFinished?.Invoke();
        }

        /// <summary>Used tiles stay removed; only when no word can be made are the gaps refilled.</summary>
        private void EnsurePlayableBoard()
        {
            if (letterGrid.HasAnyWord()) return;
            int added = letterGrid.RefillEmpty(minWordsOnBoard);
            if (added > 0 && feedbackLabel != null)
                feedbackLabel.text += "\n<color=#9FE2BF>No words left. New letters fill the empty spaces.</color>";
        }

        // ── Movement ─────────────────────────────────────────────────────────
        /// <summary>
        /// Moves a character horizontally. Characters with a walk clip (side view) play it and face the
        /// direction of travel; front-facing creatures without one keep their idle and hop along instead.
        /// </summary>
        private IEnumerator WalkTo(UIFrameAnimator anim, CharacterDefinition def, float targetX)
        {
            var rt = (RectTransform)anim.transform;
            bool hasWalk = def.walk != null && def.walk.Length > 0;
            float baseY = groundY + def.hoverHeight;
            float startX = rt.anchoredPosition.x;

            if (hasWalk)
            {
                bool movingRight = targetX >= startX;
                rt.localScale = new Vector3(def.spriteFacesRight == movingRight ? 1f : -1f, 1f, 1f);
                anim.PlayLoop(def.walk);
            }

            while (Mathf.Abs(rt.anchoredPosition.x - targetX) > 0.5f)
            {
                float x = Mathf.MoveTowards(rt.anchoredPosition.x, targetX, walkSpeed * Time.deltaTime);
                float y = baseY;
                if (!hasWalk)
                {
                    // Hop: 3 bounces per 1000px travelled, flying creatures bob more gently
                    float travelled = Mathf.Abs(x - startX);
                    float height = def.hoverHeight > 0f ? 22f : 34f;
                    y += Mathf.Abs(Mathf.Sin(travelled / 1000f * 3f * Mathf.PI)) * height;
                }
                rt.anchoredPosition = new Vector2(x, y);
                yield return null;
            }

            rt.anchoredPosition = new Vector2(targetX, baseY);
            rt.localScale = new Vector3(RestFacing(def), 1f, 1f);
            if (hasWalk) anim.ReturnToIdle();
        }

        /// <summary>
        /// Horizontal scale that makes a character look at the opponent: the player stands on the left
        /// looking right, enemies stand on the right looking left. Artwork that faces the other way is mirrored.
        /// </summary>
        private float RestFacing(CharacterDefinition def)
        {
            bool shouldFaceRight = def == player;
            return def.spriteFacesRight == shouldFaceRight ? 1f : -1f;
        }

        private void SetX(RectTransform rt, CharacterDefinition def, float x)
        {
            rt.anchoredPosition = new Vector2(x, groundY + (def != null ? def.hoverHeight : 0f));
        }

        // ── Enemies / board ──────────────────────────────────────────────────
        private void LoadEnemy(int index)
        {
            var enemy = CurrentEnemy;
            if (enemy == null) return;

            enemyHP = enemy.maxHP;
            scramblesLeft = scramblesPerEnemy;
            powerPotionsAwarded = 0;
            if (orasyonMeter != null) orasyonMeter.ResetCharge();
            enemyTurnIndex = 0;
            previousAttack = olderAttack = null;
            nextAttack = PickNextAttack(enemy);
            RefreshScrambleLabel();
            HideIntent();
            if (enemyNameLabel != null)
                enemyNameLabel.text = enemy.isBoss ? $"<color=#FF6B6B>BOSS</color>  {enemy.displayName}" : enemy.displayName;
            if (encounterLabel != null)
                encounterLabel.text = enemy.isBoss ? "Boss Battle" : $"Encounter {index + 1} of {enemies.Length}";
            if (enemyInfo != null) enemyInfo.Show(enemy);
            SetupAnimator(enemyAnimator, enemy);
            RefreshHearts();
        }

        private void NewBoard()
        {
            string plant = null;
            var enemy = CurrentEnemy;
            if (enemy != null && enemy.themeWords != null && enemy.themeWords.Length > 0)
            {
                string pick = enemy.themeWords[rng.Next(enemy.themeWords.Length)].ToUpperInvariant();
                if (pick.Length <= letterGrid.Rows * letterGrid.Cols && WordDictionary.IsWord(pick)) plant = pick;
            }
            letterGrid.GenerateBoard(plant, minWordsOnBoard, dropIn: true);
            SpawnBoardSpecials();
        }

        private char[,] ParseBoard(string letters)
        {
            var board = new char[letterGrid.Rows, letterGrid.Cols];
            string src = letters.ToUpperInvariant().Replace(" ", "");
            for (int i = 0; i < letterGrid.Rows * letterGrid.Cols; i++)
                board[i / letterGrid.Cols, i % letterGrid.Cols] = src[i];
            return board;
        }

        private static bool IsThemeWord(CharacterDefinition enemy, string word)
        {
            if (enemy?.themeWords == null) return false;
            foreach (var w in enemy.themeWords)
                if (string.Equals(w, word, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void SetupAnimator(UIFrameAnimator anim, CharacterDefinition def)
        {
            if (anim == null || def == null) return;
            anim.FramesPerSecond = def.framesPerSecond;
            var rt = (RectTransform)anim.transform;
            rt.sizeDelta = new Vector2(def.spriteSize, def.spriteSize);
            rt.localScale = new Vector3(RestFacing(def), 1f, 1f);
            anim.SetAlpha(1f);
            anim.SetIdle(def.idle);
        }

        private static void PlayHurt(UIFrameAnimator anim, CharacterDefinition def)
        {
            if (def.hurt != null && def.hurt.Length > 0) anim.PlayOnce(def.hurt);
            anim.Flash(new Color(1f, 0.55f, 0.55f), 0.15f);
        }

        // ── UI ───────────────────────────────────────────────────────────────
        private void RefreshInput()
        {
            bool canAct = !busy && !battleOver;
            if (gridGroup != null)
            {
                bool grid = canAct && !gridLocked;
                gridGroup.interactable = grid;
                gridGroup.blocksRaycasts = grid;
            }
            if (attackButton != null)   attackButton.interactable   = canAct && !actionsLocked && letterGrid.CurrentWord.Length > 0;
            if (scrambleButton != null) scrambleButton.interactable = canAct && !actionsLocked && scramblesLeft > 0;
            if (clearButton != null)    clearButton.interactable    = canAct && !gridLocked;
            if (hintButton != null)     hintButton.interactable     = canAct && !actionsLocked && hintsLeft > 0;
            if (orasyonMeter != null)   orasyonMeter.SetInteractable(canAct && !actionsLocked);
            if (buffs != null)          buffs.Interactable          = canAct && !actionsLocked;
        }

        private void RefreshHearts()
        {
            if (playerHearts != null) playerHearts.Set(playerHP, playerMaxHP);
            var enemy = CurrentEnemy;
            if (enemyHearts != null && enemy != null) enemyHearts.Set(enemyHP, enemy.maxHP);
            // Numeric HP sits on the name line, e.g. "Albularyo  40/40"
            if (playerNameLabel != null && player != null)
                playerNameLabel.text = $"{player.displayName}  <size=70%><color=#E8E0C0>{playerHP}/{playerMaxHP}</color></size>";
            if (enemyNameLabel != null && enemy != null)
                enemyNameLabel.text = (enemy.isBoss ? "<color=#FF6B6B>BOSS</color>  " : "") + enemy.displayName +
                                      $"  <size=70%><color=#E8E0C0>{enemyHP}/{enemy.maxHP}</color></size>";
        }

        private void RefreshStatus()
        {
            if (playerStatusLabel != null)
            {
                string s = "";
                if (shieldCharges > 0)    s += $"<color=#7CFF8A>Warded x{shieldCharges}</color>   ";
                if (damageMultiplier > 1) s += "<color=#7FB2FF>Powered x2</color>   ";
                if (poisonTurns > 0)      s += $"<color=#9CFF7A>Poisoned ({poisonDamage}/turn, {poisonTurns})</color>   ";
                if (weakenTurns > 0)      s += $"<color=#C9A2FF>Weakened -{weakenPercent}% ({weakenTurns})</color>   ";
                if (stunned)              s += "<color=#FFE066>Stunned</color>";
                playerStatusLabel.text = s;
            }
            if (enemyStatusLabel != null)
                enemyStatusLabel.text = guardTurns > 0 ? $"<color=#8CC8FF>Guarded -{guardPercent}% ({guardTurns})</color>" : "";
        }

        private void ShowFeedback(string msg, bool good)
        {
            if (feedbackLabel == null) return;
            feedbackLabel.color = good ? new Color(0.98f, 0.90f, 0.45f) : new Color(1f, 0.55f, 0.5f);
            feedbackLabel.text = msg;
        }

        private void Popup(RectTransform target, string text, Color color, float scale = 1f)
        {
            PopupAt(target.anchoredPosition + new Vector2(0f, target.sizeDelta.y * 0.85f), text, color, scale);
        }

        private void PopupAt(Vector2 position, string text, Color color, float scale = 1f)
        {
            if (damagePopupTemplate == null) return;
            var popup = Instantiate(damagePopupTemplate, damagePopupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            var rt = (RectTransform)popup.transform;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one * scale;
            rt.SetAsLastSibling();
            StartCoroutine(PopupRoutine(popup));
        }

        private static IEnumerator PopupRoutine(TextMeshProUGUI popup)
        {
            var rt = (RectTransform)popup.transform;
            Vector2 start = rt.anchoredPosition;
            Color c = popup.color;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.9f)
            {
                rt.anchoredPosition = start + new Vector2(0f, 70f * t);
                c.a = 1f - t * t;
                popup.color = c;
                yield return null;
            }
            Destroy(popup.gameObject);
        }

        private void ShowResult(bool victory)
        {
            won = victory;
            resultPanel.SetActive(true);
            var retryLabel = retryButton.GetComponentInChildren<TextMeshProUGUI>();
            bool showReward = victory && bossReward != null;
            if (rewardGroup != null) rewardGroup.SetActive(showReward);
            if (showReward)
            {
                if (rewardIcon != null) rewardIcon.sprite = bossReward.icon;
                if (rewardName != null)
                    rewardName.text = (newTreasure ? "Treasure found: " : "Already owned: ") + $"<b>{bossReward.displayName}</b>";
                if (rewardEffect != null) rewardEffect.text = bossReward.EffectText;
            }

            if (victory)
            {
                resultTitle.text = "Chapter Complete!";
                resultBody.text  = victoryMessage;
                if (retryLabel != null) retryLabel.text = nextStory != null ? "CONTINUE" : "PLAY AGAIN";
            }
            else
            {
                resultTitle.text = "Defeated";
                resultBody.text  = "Your orasyon falters... but the book is still in your hands.";
                if (retryLabel != null) retryLabel.text = "TRY AGAIN";
            }
        }

        private bool won;

        private void LoadMainMenu() => SceneManager.LoadScene(mainMenuScene);

        /// <summary>After a win with a follow-up story: continue the adventure. Otherwise replay this chapter.</summary>
        private void Retry()
        {
            if (won && nextStory != null) Orasyon.Story.StoryRouter.Play(nextStory);
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
