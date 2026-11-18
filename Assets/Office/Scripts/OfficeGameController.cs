using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class OfficeGameController : MonoBehaviour
{
    [Header("Scene references")]
    public Image workScreen;
    public Image solitaireScreen;
    public Image neutralBoss;
    public Image angryBoss;
    public Image stressFill;
    public Image productivityFill;
    public PixelClock pixelClock;
    public Text statusText;
    public Text warningText;
    public GameObject warningPanel;
    public Text monitorTitle;
    public Text monitorActionLabel;
    public Image monitorActionIcon;
    public Sprite openSolitaireIcon;
    public Sprite closeSolitaireIcon;
    public Text solitaireMovesText;
    public Text solitaireBonusText;
    public GameObject endPanel;
    public Text endTitleText;
    public Text endBodyText;

    [Header("Controls")]
    public Button monitorAction;
    public Button[] solitaireCards;
    public OfficeOptions options;
    public Button restartButton;
    public Button menuButton;

    [Header("Boss animation")]
    public Sprite[] neutralBossFrames;
    public Sprite[] angryBossFrames;

    [Header("Tunable shift")]
    [Min(30f)] public float shiftDurationSeconds = 180f;
    [Range(0f, 100f)] public float startingStress = 30f;
    [Range(0f, 100f)] public float startingProductivity = 65f;
    public float workStressPerSecond = 0.78f;
    public float restStressPerSecond = 1.18f;
    public float workProductivityPerSecond = 0.82f;
    public float watchedWorkProductivityPerSecond = 1.35f;
    public float restProductivityPerSecond = 0.38f;
    public float idleProductivityPerSecond = 2f;
    public float capDecayPerSecond = 9f;

    [Header("Solitaire bonus")]
    [Range(0f, 1f)] public float solitaireBonusChance = 0.25f;
    [Min(0f)] public float solitaireBonusStress = 3f;
    [Min(0f)] public float solitaireAttemptInterval = 2f;

    [Header("Boss timing")]
    public Vector2 firstVisitDelay = new Vector2(9f, 14f);
    public Vector2 visitDelay = new Vector2(14f, 22f);
    public float warningSeconds = 1.8f;
    public float reactionSeconds = 1.25f;
    public float watchingSeconds = 4.6f;
    public float attentionIncreasePerReprimand = 3.2f;

    public float Stress { get; private set; }
    public float Productivity { get; private set; }
    public int Reprimands { get; private set; }
    public bool IsResting { get; private set; }
    public bool BossWatching { get; private set; }
    public bool GameEnded { get; private set; }
    public bool IsPaused => options != null && options.IsOpen;

    private const float BonusNoticeDuration = 1f;
    private float elapsedSeconds;
    private float caughtAt;
    private int solitaireMoves;
    private float nextSolitaireAttemptAt;
    private float bonusNoticeUntil;
    private Vector2 bonusNoticeOrigin;
    private bool working;
    private bool caughtThisVisit;
    private float messageUntil;

    private void Start()
    {
        Stress = startingStress;
        Productivity = startingProductivity;

        monitorAction?.onClick.AddListener(() => SetResting(!IsResting));
        if (solitaireCards != null)
        {
            foreach (Button card in solitaireCards)
                card.onClick.AddListener(OnCardClicked);
        }
        restartButton?.onClick.AddListener(Restart);
        menuButton?.onClick.AddListener(ReturnToMenu);

        if (solitaireBonusText != null)
            bonusNoticeOrigin = solitaireBonusText.rectTransform.anchoredPosition;
        if (endPanel != null)
            endPanel.SetActive(false);

        HideBoss();
        SetResting(false, false);
        UpdateClock();
        UpdateInterface();
        StartCoroutine(BossRoutine());
        ShowMessage("Наведите курсор на отчёт, чтобы работать.", 4.5f);
    }

    private void Update()
    {
        HandleKeyboard();
        if (GameEnded || IsPaused)
            return;

        float delta = Time.deltaTime;
        elapsedSeconds += delta;
        UpdateStats(delta);
        UpdateBossReaction();
        UpdateClock();
        UpdateInterface();
        UpdateBonusNotice();

        if (Stress >= 100f)
        {
            Finish(false, "ВЫГОРАНИЕ", "Стресс достиг 100%. Персонаж написал заявление об увольнении по собственному.");
        }
        else if (Productivity <= 0f)
        {
            Finish(false, "УВОЛЬНЕНИЕ", "Продуктивность упала до нуля. Вас попросили освободить рабочее место.");
        }
        else if (Reprimands >= 3)
        {
            Finish(false, "УВОЛЬНЕНИЕ", "Третий выговор стал последним. Вы уволены.");
        }
        else if (elapsedSeconds >= shiftDurationSeconds)
        {
            bool premium = OfficeBalance.HasPremium(Reprimands);
            string premiumLine = premium
                ? "Премия сохранена. Отличная имитация интенсивной деятельности."
                : "Смена закончена, но после второго выговора премии не будет.";
            Finish(true, "СМЕНА ОКОНЧЕНА", premiumLine);
        }
    }

    private void HandleKeyboard()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameEnded)
                ReturnToMenu();
            else
                options?.Toggle();
            return;
        }

        if (!GameEnded && !IsPaused && Keyboard.current.spaceKey.wasPressedThisFrame)
            SetResting(!IsResting);
    }

    private void UpdateStats(float delta)
    {
        working = !IsResting && workScreen != null && Mouse.current != null && Application.isFocused &&
            RectTransformUtility.RectangleContainsScreenPoint(workScreen.rectTransform, Mouse.current.position.ReadValue());

        if (IsResting)
        {
            Stress -= restStressPerSecond * delta;
            Productivity -= restProductivityPerSecond * delta;
        }
        else if (working)
        {
            Stress += workStressPerSecond * delta;
            float productivityRate = BossWatching
                ? watchedWorkProductivityPerSecond
                : workProductivityPerSecond;
            Productivity += productivityRate * delta;
        }
        else
        {
            Productivity -= idleProductivityPerSecond * delta;
        }

        Stress = Mathf.Clamp(Stress, 0f, 100f);
        Productivity = Mathf.Clamp(Productivity, 0f, 100f);

        float cap = OfficeBalance.ProductivityCap(Reprimands);
        if (Productivity > cap)
            Productivity = Mathf.MoveTowards(Productivity, cap, capDecayPerSecond * delta);
    }

    private IEnumerator BossRoutine()
    {
        yield return new WaitForSeconds(Random.Range(firstVisitDelay.x, firstVisitDelay.y));

        while (!GameEnded)
        {
            caughtThisVisit = false;
            BossWatching = false;

            float adjustedWarning = Mathf.Max(0.9f, warningSeconds - Reprimands * 0.2f);
            ShowNeutralBoss(0);
            OfficeAudio.Play(OfficeAudio.Cue.Footsteps);
            ShowMessage("Кто-то рядом", Mathf.Min(1.2f, adjustedWarning));
            yield return new WaitForSeconds(adjustedWarning);
            if (GameEnded)
                yield break;

            BossWatching = true;
            ShowNeutralBoss(2);
            yield return new WaitForSeconds(reactionSeconds);

            float watchedFor = 0f;
            while (watchedFor < watchingSeconds && !GameEnded)
            {
                watchedFor += Time.deltaTime;
                if (!IsPaused && IsResting && !caughtThisVisit)
                    CatchPlayer();
                yield return null;
            }

            BossWatching = false;
            if (caughtThisVisit)
            {
                float remainingReaction = 2.6f - (Time.time - caughtAt);
                if (remainingReaction > 0f)
                    yield return new WaitForSeconds(remainingReaction);
            }
            HideBoss();

            float minDelay = Mathf.Max(6.5f, visitDelay.x - Reprimands * attentionIncreasePerReprimand);
            float maxDelay = Mathf.Max(minDelay + 2f, visitDelay.y - Reprimands * attentionIncreasePerReprimand);
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }

    private void CatchPlayer()
    {
        caughtThisVisit = true;
        caughtAt = Time.time;
        Reprimands++;
        ShowAngryBoss();
        SetResting(false, false);
        if (Reprimands < 3)
            OfficeAudio.Play(OfficeAudio.Cue.Warning);

        if (Reprimands == 1)
            ShowMessage("ВЫГОВОР 1/3: потолок продуктивности снижен до 67%.", 3.6f);
        else if (Reprimands == 2)
            ShowMessage("ВЫГОВОР 2/3: потолок 33%, премия снята.", 3.6f);
        else
            ShowMessage("ВЫГОВОР 3/3: приготовьтесь покинуть офис.", 3.6f);

        UpdateInterface();
    }

    private void UpdateBossReaction()
    {
        if (angryBoss == null || !angryBoss.gameObject.activeSelf || angryBossFrames == null ||
            angryBossFrames.Length < 3)
            return;

        float reactionAge = Time.time - caughtAt;
        int frame = reactionAge < 0.9f ? 1 : reactionAge < 2.1f ? 0 : 2;
        if (angryBoss.sprite != angryBossFrames[frame])
            angryBoss.sprite = angryBossFrames[frame];
    }

    private void ShowNeutralBoss(int frame)
    {
        if (neutralBoss != null)
        {
            neutralBoss.gameObject.SetActive(true);
            if (neutralBossFrames != null && neutralBossFrames.Length > frame)
                neutralBoss.sprite = neutralBossFrames[frame];
        }

        if (angryBoss != null)
            angryBoss.gameObject.SetActive(false);
    }

    private void ShowAngryBoss()
    {
        if (neutralBoss != null)
            neutralBoss.gameObject.SetActive(false);

        if (angryBoss != null)
        {
            angryBoss.gameObject.SetActive(true);
            UpdateBossReaction();
        }
    }

    private void HideBoss()
    {
        if (neutralBoss != null)
            neutralBoss.gameObject.SetActive(false);
        if (angryBoss != null)
            angryBoss.gameObject.SetActive(false);
    }

    private void OnCardClicked()
    {
        if (!IsResting || GameEnded || IsPaused)
            return;

        solitaireMoves++;
        OfficeAudio.Play(OfficeAudio.Cue.Card);
        if (elapsedSeconds >= nextSolitaireAttemptAt)
        {
            nextSolitaireAttemptAt = elapsedSeconds + solitaireAttemptInterval;
            if (Random.value < solitaireBonusChance)
            {
                float relief = Mathf.Min(Stress, solitaireBonusStress);
                Stress -= relief;
                if (relief > 0f && solitaireBonusText != null)
                {
                    solitaireBonusText.text = $"−{relief:0.#} СТ";
                    bonusNoticeUntil = elapsedSeconds + BonusNoticeDuration;
                    UpdateBonusNotice();
                }
            }
        }
        UpdateInterface();
    }

    private void UpdateBonusNotice()
    {
        if (solitaireBonusText == null)
            return;

        bool visible = IsResting && elapsedSeconds < bonusNoticeUntil;
        solitaireBonusText.gameObject.SetActive(visible);
        if (!visible)
            return;

        float progress = 1f - (bonusNoticeUntil - elapsedSeconds) / BonusNoticeDuration;
        solitaireBonusText.rectTransform.anchoredPosition = bonusNoticeOrigin + Vector2.up * (24f * progress);
        Color color = solitaireBonusText.color;
        color.a = Mathf.Clamp01((1f - progress) * 3f);
        solitaireBonusText.color = color;
    }

    private void SetResting(bool resting, bool showMessage = true)
    {
        if (GameEnded || IsPaused)
            return;

        IsResting = resting;
        working = false;
        if (!resting)
        {
            bonusNoticeUntil = 0f;
            UpdateBonusNotice();
        }
        if (workScreen != null)
            workScreen.gameObject.SetActive(!resting);
        if (solitaireScreen != null)
            solitaireScreen.gameObject.SetActive(resting);
        if (monitorTitle != null)
            monitorTitle.text = resting ? "ПАСЬЯНС" : "ОТЧЁТ";
        if (monitorActionLabel != null)
            monitorActionLabel.text = resting ? "ЗАКРЫТЬ" : "ПАСЬЯНС";
        if (monitorActionIcon != null)
            monitorActionIcon.sprite = resting ? closeSolitaireIcon : openSolitaireIcon;
        EventSystem.current?.SetSelectedGameObject(null);

        if (showMessage)
            OfficeAudio.Play(OfficeAudio.Cue.Screen);
        if (showMessage && !BossWatching && Time.time >= messageUntil)
        {
            ShowMessage(
                resting ? "Кликайте по картам, чтобы снять больше стресса." : "Наведите курсор на отчёт, чтобы работать.",
                2f);
        }

        UpdateInterface();
    }

    private void UpdateClock()
    {
        int minutes = OfficeBalance.CurrentShiftMinutes(elapsedSeconds, shiftDurationSeconds);
        pixelClock?.SetMinutes(minutes);
    }

    private void UpdateInterface()
    {
        if (stressFill != null)
            stressFill.fillAmount = Stress / 100f;
        if (productivityFill != null)
            productivityFill.fillAmount = Productivity / 100f;

        if (solitaireMovesText != null)
            solitaireMovesText.text = $"ХОДОВ: {solitaireMoves}";

        if (statusText != null)
        {
            string mode = IsResting ? "ПАСЬЯНС" : working ? "РАБОТА" : "ОЖИДАНИЕ";
            string premium = OfficeBalance.HasPremium(Reprimands) ? "ЕСТЬ" : "НЕТ";
            statusText.text =
                $"РЕЖИМ: {mode}\n" +
                $"СТРЕСС: {Stress:0}%\n" +
                $"ПРОДУКТИВНОСТЬ: {Productivity:0}% (МАКС. {OfficeBalance.ProductivityCap(Reprimands):0}%)\n" +
                $"ВЫГОВОРЫ: {Reprimands}/3   ПРЕМИЯ: {premium}";
        }

        if (warningPanel != null)
            warningPanel.SetActive(Time.time < messageUntil);
    }

    private void ShowMessage(string message, float seconds)
    {
        if (warningText != null)
            warningText.text = message;
        messageUntil = Time.time + seconds;
        UpdateInterface();
    }

    private void Finish(bool won, string title, string body)
    {
        if (GameEnded)
            return;

        GameEnded = true;
        OfficeAudio.Play(won ? OfficeAudio.Cue.Win : OfficeAudio.Cue.Lose);
        StopAllCoroutines();
        BossWatching = false;
        HideBoss();

        if (endPanel != null)
            endPanel.SetActive(true);
        if (endTitleText != null)
        {
            endTitleText.text = title;
            endTitleText.color = won ? new Color(0.55f, 1f, 0.25f) : new Color(1f, 0.27f, 0.22f);
        }
        if (endBodyText != null)
        {
            endBodyText.text =
                body + "\n\n" +
                $"Итог: {Reprimands}/3 выговоров, стресс {Stress:0}%, ОТ {Productivity:0}%.";
        }
    }

    public void Restart()
    {
        OfficeAudio.Play(OfficeAudio.Cue.Click);
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void ReturnToMenu()
    {
        OfficeAudio.Play(OfficeAudio.Cue.Click);
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
    }
}
