using UnityEngine;

public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance;

    private float startTime;
    private float endTime;
    private bool isTiming = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject); // 可选：跨场景保留计时器
        }
        else
        {
            Destroy(gameObject); // 避免重复创建
        }
    }

    public void StartTimer()
    {
        startTime = Time.time;
        isTiming = true;
    }

    public void StopTimer()
    {
        endTime = Time.time;
        isTiming = false;
    }

    public float GetTime()
    {
        return isTiming ? Time.time - startTime : endTime - startTime;
    }

    public string GetFormattedTime()
    {
        float time = GetTime();
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 1000) % 1000);
        return string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    public void ResetTimer()
    {
        startTime = 0f;
        endTime = 0f;
        isTiming = false;
    }
}
