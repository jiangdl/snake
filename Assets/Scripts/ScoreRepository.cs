using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScoreRepository
{
    private const string ScoreKey = "snake_score_history_v1";
    private const int MaxStoredScores = 100;

    [Serializable]
    public class ScoreEntry
    {
        public int score;
        public string timestamp;
    }

    [Serializable]
    private class ScoreData
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
        // Backward compatibility for old schema where only int scores were stored.
        public List<int> scores = new List<int>();
    }

    public static void SaveScore(int score)
    {
        if (score < 0) return;

        var data = LoadData();
        data.entries.Add(new ScoreEntry
        {
            score = score,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        SortAndTrim(data.entries);
        PlayerPrefs.SetString(ScoreKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    public static List<ScoreEntry> GetTopEntries(int count)
    {
        var data = LoadData();
        SortAndTrim(data.entries);

        int take = Mathf.Clamp(count, 0, data.entries.Count);
        if (take == 0) return new List<ScoreEntry>();

        return data.entries.GetRange(0, take);
    }

    public static List<ScoreEntry> GetAllEntries()
    {
        var data = LoadData();
        SortAndTrim(data.entries);
        return new List<ScoreEntry>(data.entries);
    }

    public static void ClearHistory()
    {
        PlayerPrefs.DeleteKey(ScoreKey);
        PlayerPrefs.Save();
    }

    private static ScoreData LoadData()
    {
        string raw = PlayerPrefs.GetString(ScoreKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return new ScoreData();

        try
        {
            var data = JsonUtility.FromJson<ScoreData>(raw) ?? new ScoreData();

            // Migrate older score-only payloads into timestamped entries.
            if ((data.entries == null || data.entries.Count == 0) && data.scores != null && data.scores.Count > 0)
            {
                data.entries = new List<ScoreEntry>();
                for (int i = 0; i < data.scores.Count; i++)
                {
                    data.entries.Add(new ScoreEntry
                    {
                        score = data.scores[i],
                        timestamp = "legacy"
                    });
                }
            }

            if (data.entries == null)
                data.entries = new List<ScoreEntry>();

            if (data.scores == null)
                data.scores = new List<int>();

            return data;
        }
        catch
        {
            return new ScoreData();
        }
    }

    private static void SortAndTrim(List<ScoreEntry> entries)
    {
        entries.Sort((a, b) => b.score.CompareTo(a.score));

        if (entries.Count > MaxStoredScores)
            entries.RemoveRange(MaxStoredScores, entries.Count - MaxStoredScores);
    }
}
