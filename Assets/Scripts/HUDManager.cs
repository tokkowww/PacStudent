using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    public GameObject[] lifeIcons;        
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text scaredTimerText;
    public TMP_Text levelNameText;

    [Header("Settings")]
    public string levelName = "Level 1";
    public string startSceneName = "StartScene"; 

    private int lives = 3;
    private int score = 0;
    private float gameTime = 0f;

    void Start()
    {
        SetLives(3);
        SetScore(0);
        SetTimer(0f);
        SetScaredTimer(0);
        levelNameText.text = levelName;
    }

    public void SetLives(int value)
    {
        lives = value;
        for (int i = 0; i < lifeIcons.Length; i++)
            lifeIcons[i].SetActive(i < lives);
    }

    public void SetScore(int value)
    {
        score = value;
        scoreText.text = score.ToString("D6");   
    }

    public void SetTimer(float seconds)
    {
        gameTime = seconds;
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        int hundredths = (int)((seconds * 100) % 100);
        timerText.text = $"{minutes:00}:{secs:00}:{hundredths:00}";
    }

    public void SetScaredTimer(int seconds)
    {

        scaredTimerText.gameObject.SetActive(seconds > 0);
        if (seconds > 0)
            scaredTimerText.text = seconds.ToString();
    }

    public void ExitToStart()
    {
        SceneManager.LoadScene(startSceneName);
    }
}