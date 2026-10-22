using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public PacStudentController pacStudent;
    public GhostController[] ghosts;

    [Header("UI")]
    public TMP_Text scoreText;
    public GameObject ghostTimerUI;
    public TMP_Text ghostTimerText;
    public GameObject[] lifeIcons;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip deadGhostMusic;

    [Header("Timers")]
    public float scaredDuration = 10f;
    public float recoverAt = 3f;

    public float ScaredTimeLeft { get; private set; }
    public bool IsPlayerDying { get; private set; }

    int score;
    int lives = 3;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateScoreUI();
        UpdateLivesUI();
        if (ghostTimerUI) ghostTimerUI.SetActive(false);
    }

    void Update()
    {
        if (ScaredTimeLeft > 0f && !IsPlayerDying)
        {
            ScaredTimeLeft -= Time.deltaTime;
            if (ghostTimerText) ghostTimerText.text = Mathf.CeilToInt(Mathf.Max(ScaredTimeLeft, 0f)).ToString();

            if (ScaredTimeLeft <= recoverAt)
            {
                foreach (var g in ghosts)
                    if (g.State == GhostState.Scared) g.SetState(GhostState.Recovering);
            }

            if (ScaredTimeLeft <= 0f) EndScared();
        }

        UpdateMusic();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    void UpdateLivesUI()
    {
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].SetActive(i < lives);
    }

    public void ActivatePowerPill()
    {
        ScaredTimeLeft = scaredDuration;
        if (ghostTimerUI) ghostTimerUI.SetActive(true);

        foreach (var g in ghosts)
            if (g.State != GhostState.Dead) g.SetState(GhostState.Scared);
    }

    void EndScared()
    {
        ScaredTimeLeft = 0f;
        if (ghostTimerUI) ghostTimerUI.SetActive(false);

        foreach (var g in ghosts)
            if (g.State != GhostState.Dead)
                g.SetState(GhostState.Normal);
    }

    public void EatGhost(GhostController ghost)
    {
        AddScore(300);
        ghost.Die();
    }

    void UpdateMusic()
    {
        if (musicSource == null || IsPlayerDying) return;

        AudioClip wanted = normalMusic;
        bool anyDead = false;
        foreach (var g in ghosts) if (g.State == GhostState.Dead) { anyDead = true; break; }

        if (anyDead) wanted = deadGhostMusic;
        else if (ScaredTimeLeft > 0f) wanted = scaredMusic;

        if (musicSource.clip != wanted)
        {
            musicSource.clip = wanted;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayerDie()
    {
        if (IsPlayerDying) return;
        StartCoroutine(PlayerDeathRoutine());
    }

    IEnumerator PlayerDeathRoutine()
    {
        IsPlayerDying = true;

        lives--;
        UpdateLivesUI();

        foreach (var g in ghosts) g.Frozen = true;

        yield return StartCoroutine(pacStudent.DeathSequence());

        ScaredTimeLeft = 0f;
        if (ghostTimerUI) ghostTimerUI.SetActive(false);
        foreach (var g in ghosts) g.ResetGhost();

        IsPlayerDying = false;

    }
}
