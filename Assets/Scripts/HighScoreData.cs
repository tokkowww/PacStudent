using UnityEngine;

public static class HighScoreData
{
    static string ScoreKey(string level) => level + "_HighScore";
    static string TimeKey(string level) => level + "_HighScoreTime";

    public static bool HasRecord(string level) => PlayerPrefs.HasKey(ScoreKey(level));
    public static int GetScore(string level) => PlayerPrefs.GetInt(ScoreKey(level), 0);
    public static float GetTime(string level) => PlayerPrefs.GetFloat(TimeKey(level), 0f);

    public static bool TrySave(string level, int score, float time)
    {
        int best = GetScore(level);
        bool better = !HasRecord(level) || score > best || (score == best && time < GetTime(level));
        if (!better) return false;

        PlayerPrefs.SetInt(ScoreKey(level), score);
        PlayerPrefs.SetFloat(TimeKey(level), time);
        PlayerPrefs.Save();
        return true;
    }

    public static string FormatTime(float t)
    {
        int minutes = (int)(t / 60f);
        int seconds = (int)(t % 60f);
        int hundredths = (int)((t * 100f) % 100f);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, hundredths);
    }
}