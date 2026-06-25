using UnityEngine;

[System.Serializable]
public class SubtitleLine
{
    public float startTime;
    public float endTime;

    [TextArea(2, 5)]
    public string text;
}