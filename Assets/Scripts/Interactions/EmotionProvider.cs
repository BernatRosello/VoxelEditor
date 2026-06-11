using System.Collections.Generic;
using UnityEngine;

public interface IEmotionProvider
{
    float GetEmotion(string emotionName);

    void SetEmotion(string emotionName, float value);
}

public class EmotionProvider : MonoBehaviour, IEmotionProvider
{
    [System.Serializable]
    public class EmotionEntry
    {
        public string name;

        [Range(-1f, 1f)]
        public float value;
    }

    [SerializeField]
    private List<EmotionEntry> emotions = new();

    private Dictionary<string, EmotionEntry> emotionMap;

    private void Awake()
    {
        emotionMap = new Dictionary<string, EmotionEntry>();

        foreach (var emotion in emotions)
        {
            if (string.IsNullOrWhiteSpace(emotion.name))
                continue;

            emotionMap[emotion.name] = emotion;
        }
    }

    public float GetEmotion(string emotionName)
    {
        if (emotionMap.TryGetValue(
                emotionName,
                out var emotion))
        {
            return emotion.value;
        }

        return 0f;
    }

    public void SetEmotion(
        string emotionName,
        float value)
    {
        value = Mathf.Clamp(value, -1f, 1f);

        if (emotionMap.TryGetValue(
                emotionName,
                out var emotion))
        {
            emotion.value = value;
            return;
        }

        EmotionEntry newEmotion = new()
        {
            name = emotionName,
            value = value
        };

        emotions.Add(newEmotion);

        emotionMap[emotionName] = newEmotion;
    }

    public void AddEmotion(
        string emotionName,
        float delta)
    {
        SetEmotion(
            emotionName,
            GetEmotion(emotionName) + delta);
    }
}