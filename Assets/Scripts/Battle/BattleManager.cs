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

        [Header("Potions")]
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
                letterGrid.SetBoard(ParseBoard(firstBoard), dropIn: true);
            else
                NewBoard();

            attackButton.onClick.AddListener(OnAttackPressed);
            clearButton.onClick.AddListener(letterGrid.ClearSelection);
            scrambleButton.onClick.AddListener(OnScramblePressed);
            menuButton.onClick.AddListener(LoadMainMenu);
            retryButton.onClick.AddListener(Retry);
            resultMenuButton.onClick.AddListener(LoadMainMenu);
            buffs.OnBuffUsed += UseBuff;
            if (treasureShelf != null)
                treasureShelf.OnTreasureClicked += t => ShowFeedback($"<b>{t.displayName}</b>  <color=#9FE2BF>{t.EffectText}</color>\n{t.description}", true);
            if (dropIconTemplate != null) dropIconTemplate.gameObject.SetActive(false);
            if (rewardGroup != null) rewardGroup.SetActive(false);
            letterGrid.OnSelectionChanged += RefreshInput;

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
        }

        // ── Player actions ───────────────────────────────────────────────────
        private bool CanAct => !busy && !battleOver && !actionsLocked && CurrentEnemy != null;

        private void OnAttackPressed()
        {
            if (!CanAct || !letterGrid.CurrentWordIsValid) return;
            StartCoroutine(AttackRoutine(letterGrid.ConsumeSelection()));
        }

        /// <summary>Shuffles the tiles still on the board. Free: the letters stay the same, only their places change.</summary>
        private void OnScramblePressed()
        {
            if (!CanAct) return;
            letterGrid.Scramble();
            ShowFeedback("You shuffle the remaining letters.", true);
        }

        private void UseBuff(BuffType type)
        {
            switch (type)
            {
                case BuffType.HealingPotion:
                    int healed = Mathf.Min(healingPotionAmount, playerMaxHP - playerHP);
                    playerHP += healed;
                    playerAnimator.PlayOnce(player.cast);
                    Popup(PlayerRect, "+" + healed, new Color(1f, 0.45f, 0.45f));
                    ShowFeedback($"Healing Potion restores {healed} HP.", true);
                    break;
                case BuffType.WardingPotion:
                    shieldCharges++;
                    playerAnimator.PlayOnce(player.cast);
                    Popup(PlayerRect, "Warded", new Color(0.45f, 1f, 0.5f));
                    ShowFeedback("A green ward surrounds you. The next attack will be blocked.", true);
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
        private IEnumerator AttackRoutine(string word)
        {
            busy = true;
            RefreshInput();
            OnPlayerAttack?.Invoke(word);

            var enemy = CurrentEnemy;
            bool weakness = IsThemeWord(enemy, word);
            bool empowered = damageMultiplier > 1;
            int damage = Mathf.RoundToInt(LetterScoring.Damage(word) * PlayerProgress.DamageMultiplier);   // treasures add damage
            if (weakness) damage = Mathf.RoundToInt(damage * 1.5f);
            damage *= damageMultiplier;
            damageMultiplier = 1;
            RefreshStatus();
            ShowFeedback($"<b>{word}</b>!", true);

            // Walk up to the enemy and strike
            yield return WalkTo(playerAnimator, player, enemyHomeX - meleeGap);
            float swing = playerAnimator.Duration(player.attack);
            playerAnimator.PlayOnce(player.attack);
            yield return new WaitForSeconds(swing * 0.55f);

            enemyHP = Mathf.Max(0, enemyHP - damage);
            RefreshHearts();
            PlayHurt(enemyAnimator, enemy);
            Popup(EnemyRect, "-" + damage, new Color(1f, 0.85f, 0.3f));

            string msg = $"<b>{word}</b> deals {damage} damage!";
            if (weakness)  msg += "  <color=#9CFF7A>Weakness!</color>";
            if (empowered) msg += "  <color=#7FB2FF>Power x2!</color>";
            var reward = buffs.AwardForWord(word.Length);
            if (reward != null) msg += $"\n<color=#9FE2BF>You found a {reward.displayName}!</color>";
            ShowFeedback(msg, true);

            yield return new WaitForSeconds(swing * 0.45f);
            yield return WalkTo(playerAnimator, player, playerHomeX);

            // Enemy defeated → next encounter (or victory)
            if (enemyHP <= 0)
            {
                yield return DefeatRoutine(enemyAnimator, enemy, fadeOut: true);
                enemyIndex++;
                if (CurrentEnemy == null)
                {
                    battleOver = true;
                    RefreshInput();
                    if (bossReward != null)
                    {
                        newTreasure = PlayerProgress.Grant(bossReward);
                        yield return TreasureDropRoutine(bossReward);
                    }
                    ShowResult(victory: true);
                    yield break;
                }
                yield return TravelRoutine();
                FinishTurn();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);
            yield return EnemyTurnRoutine(enemy);
            if (CheckPlayerDefeat()) yield break;
            FinishTurn();
        }

        private IEnumerator EnemyTurnRoutine(CharacterDefinition enemy)
        {
            // Close the distance to the player, strike, then go back
            float home = EnemyRect.anchoredPosition.x;
            float reach = (player.spriteSize + enemy.spriteSize) * 0.5f * 0.75f;
            yield return WalkTo(enemyAnimator, enemy, playerHomeX + reach);

            float strike = enemyAnimator.Duration(enemy.attack);
            enemyAnimator.PlayOnce(enemy.attack);
            yield return new WaitForSeconds(strike * 0.6f);

            if (shieldCharges > 0)
            {
                shieldCharges--;
                Popup(PlayerRect, "Blocked!", new Color(0.55f, 0.8f, 1f));
                ShowFeedback($"Your ward blocks {enemy.attackName}!", true);
            }
            else
            {
                playerHP = Mathf.Max(0, playerHP - enemy.attackDamage);
                PlayHurt(playerAnimator, player);
                Popup(PlayerRect, "-" + enemy.attackDamage, new Color(1f, 0.4f, 0.35f));
                ShowFeedback($"The {enemy.displayName} uses {enemy.attackName}! -{enemy.attackDamage} HP", false);
            }
            RefreshHearts();
            RefreshStatus();

            yield return new WaitForSeconds(strike * 0.4f);
            yield return WalkTo(enemyAnimator, enemy, home);
        }

        /// <summary>Player walks forward while the background scrolls and the next enemy approaches.</summary>
        private IEnumerator TravelRoutine()
        {
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
            yield return new WaitForSeconds(0.5f);
        }

        private bool CheckPlayerDefeat()
        {
            if (playerHP > 0) return false;
            battleOver = true;
            RefreshInput();
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
                float dir = Mathf.Sign(targetX - startX);
                rt.localScale = new Vector3(dir < 0 ? -1f : 1f, 1f, 1f);
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
            rt.localScale = Vector3.one;
            if (hasWalk) anim.ReturnToIdle();
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

        private static void SetupAnimator(UIFrameAnimator anim, CharacterDefinition def)
        {
            if (anim == null || def == null) return;
            anim.FramesPerSecond = def.framesPerSecond;
            var rt = (RectTransform)anim.transform;
            rt.sizeDelta = new Vector2(def.spriteSize, def.spriteSize);
            rt.localScale = Vector3.one;
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
            if (attackButton != null)   attackButton.interactable   = canAct && !actionsLocked && letterGrid.CurrentWordIsValid;
            if (scrambleButton != null) scrambleButton.interactable = canAct && !actionsLocked;
            if (clearButton != null)    clearButton.interactable    = canAct && !gridLocked;
            if (buffs != null)          buffs.Interactable          = canAct && !actionsLocked;
        }

        private void RefreshHearts()
        {
            if (playerHearts != null) playerHearts.Set(playerHP, playerMaxHP);
            var enemy = CurrentEnemy;
            if (enemyHearts != null && enemy != null) enemyHearts.Set(enemyHP, enemy.maxHP);
        }

        private void RefreshStatus()
        {
            if (playerStatusLabel == null) return;
            string s = "";
            if (shieldCharges > 0)     s += $"<color=#7CFF8A>Warded x{shieldCharges}</color>   ";
            if (damageMultiplier > 1)  s += "<color=#7FB2FF>Powered: next word x2</color>";
            playerStatusLabel.text = s;
        }

        private void ShowFeedback(string msg, bool good)
        {
            if (feedbackLabel == null) return;
            feedbackLabel.color = good ? new Color(0.98f, 0.90f, 0.45f) : new Color(1f, 0.55f, 0.5f);
            feedbackLabel.text = msg;
        }

        private void Popup(RectTransform target, string text, Color color)
        {
            if (damagePopupTemplate == null) return;
            var popup = Instantiate(damagePopupTemplate, damagePopupTemplate.transform.parent);
            popup.gameObject.SetActive(true);
            popup.text = text;
            popup.color = color;
            var rt = (RectTransform)popup.transform;
            rt.anchoredPosition = target.anchoredPosition + new Vector2(0f, target.sizeDelta.y * 0.85f);
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
