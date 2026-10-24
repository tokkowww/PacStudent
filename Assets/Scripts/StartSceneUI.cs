using UnityEngine;
using TMPro;

public class StartSceneUI : MonoBehaviour
{
    [Header("Level 1")]
    public string level1Key = "Level1";
    public TMP_Text level1ScoreText;
    public TMP_Text level1TimeText;

    [Header("Level 2")]
    public string level2Key = "Level2";
    public TMP_Text level2ScoreText;
    public TMP_Text level2TimeText;

    void Start()
    {
        Show(level1Key, level1ScoreText, level1TimeText);
        Show(level2Key, level2ScoreText, level2TimeText);
    }

    void Show(string level, TMP_Text scoreText, TMP_Text timeText)
    {
        if (scoreText) scoreText.text = HighScoreData.GetScore(level).ToString();
        if (timeText) timeText.text = HighScoreData.FormatTime(HighScoreData.GetTime(level));
    }
}