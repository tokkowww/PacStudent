using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public PacStudentController pacStudent { get; private set; }
    readonly List<GhostController> ghosts = new List<GhostController>();

    [Header("UI")]
    public TMP_Text scoreText;
    public GameObject ghostTimerUI;
    public TMP_Text ghostTimerText;
    public GameObject[] lifeIcons;
    public TMP_Text gameTimerText;
    public Selectable[] hudButtons;

    [Header("Level")]
    public string levelKey = "Level1";

    [Header("Round Start / Game Over")]
    public GameObject blockingImage;
    public TMP_Text countdownText;
    public GameObject gameOverText;
    public string startSceneName = "StartScene";
    public float gameOverDelay = 3f;

    [Header("Music")]
    public AudioSource musicSource;
    public AudioClip introMusic;
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip deadGhostMusic;

    [Header("Timers")]
    public float scaredDuration = 10f;
    public float recoverAt = 3f;

    public float ScaredTimeLeft { get; private set; }
    public bool IsPlayerDying { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsGameOver { get; private set; }
    public float GameTime { get; private set; }

    public GhostState GlobalGhostState =>
        ScaredTimeLeft <= 0f ? GhostState.Normal :
        ScaredTimeLeft <= recoverAt ? GhostState.Recovering : GhostState.Scared;

    int score;
    int lives = 3;
    int pelletsLeft;

    void Awake()
    {
        Instance = this;
    }

    public void RegisterGhost(GhostController ghost)
    {
        if (!ghosts.Contains(ghost)) ghosts.Add(ghost);
    }

    public void UnregisterGhost(GhostController ghost)
    {
        ghosts.Remove(ghost);
    }

    public void RegisterPacStudent(PacStudentController pac)
    {
        pacStudent = pac;
    }

    void Start()
    {
        UpdateScoreUI();
        UpdateLivesUI();
        UpdateGameTimerUI();
        if (ghostTimerUI) ghostTimerUI.SetActive(false);
        if (gameOverText) gameOverText.SetActive(false);
        if (blockingImage) blockingImage.SetActive(false);
        if (countdownText) countdownText.gameObject.SetActive(false);

        StartCoroutine(RoundStart());
    }

    IEnumerator RoundStart()
    {
        IsPlaying = false;

        if (blockingImage) blockingImage.SetActive(true);
        if (countdownText) countdownText.gameObject.SetActive(true);

        if (musicSource && introMusic)
        {
            musicSource.clip = introMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        string[] steps = { "3", "2", "1", "GO!" };
        foreach (string step in steps)
        {
            if (countdownText) countdownText.text = step;
            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.gameObject.SetActive(false);
        if (blockingImage) blockingImage.SetActive(false);
        if (musicSource) musicSource.Stop();

        pelletsLeft = GameObject.FindGameObjectsWithTag("Pellet").Length
                    + GameObject.FindGameObjectsWithTag("PowerPill").Length;

        GameTime = 0f;
        IsPlaying = true;
    }

    void Update()
    {
        if (!IsPlaying) return;

        GameTime += Time.deltaTime;
        UpdateGameTimerUI();

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

    void UpdateGameTimerUI()
    {
        if (gameTimerText) gameTimerText.text = HighScoreData.FormatTime(GameTime);
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    public void PelletEaten()
    {
        pelletsLeft--;
        if (pelletsLeft <= 0) GameOver();
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

        if (musicSource.clip != wanted || !musicSource.isPlaying)
        {
            musicSource.clip = wanted;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void PlayerDie()
    {
        if (IsPlayerDying || !IsPlaying) return;
        StartCoroutine(PlayerDeathRoutine());
    }

    IEnumerator PlayerDeathRoutine()
    {
        IsPlayerDying = true;

        lives--;
        UpdateLivesUI();

        foreach (var g in ghosts) g.Frozen = true;

        if (pacStudent) yield return StartCoroutine(pacStudent.DeathSequence());

        IsPlayerDying = false;

        if (lives <= 0)
        {
            GameOver();
            yield break;
        }

        ScaredTimeLeft = 0f;
        if (ghostTimerUI) ghostTimerUI.SetActive(false);
        foreach (var g in ghosts) g.ResetGhost();
    }

    void GameOver()
    {
        if (IsGameOver) return;
        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        IsGameOver = true;
        IsPlaying = false;

        foreach (var g in ghosts) g.Frozen = true;
        foreach (var b in hudButtons) if (b) b.interactable = false;
        if (musicSource) musicSource.Stop();

        if (blockingImage) blockingImage.SetActive(true);
        if (gameOverText) gameOverText.SetActive(true);

        HighScoreData.TrySave(levelKey, score, GameTime);

        yield return new WaitForSeconds(gameOverDelay);

        SceneManager.LoadScene(startSceneName);
    }
}