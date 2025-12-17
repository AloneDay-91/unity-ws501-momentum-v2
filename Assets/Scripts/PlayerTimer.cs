using UnityEngine;
using TMPro;

public class PlayerTimer : MonoBehaviour
{
    public float currentTime = 0f;
    public bool isTimerRunning = false;
    public TextMeshProUGUI timerText;

    void Update()
    {
        if (isTimerRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerText();
        }
    }

    public void StartTimer()
    {
        isTimerRunning = true;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    public void ResetTimer()
    {
        currentTime = 0f;
        isTimerRunning = false;
        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText != null)
        {
            // Format time to minutes, seconds, and milliseconds
            float minutes = Mathf.FloorToInt(currentTime / 60);
            float seconds = Mathf.FloorToInt(currentTime % 60);
            float milliseconds = (currentTime % 1) * 1000;
            timerText.text = string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
        }
    }
}
