using TMPro;
using UnityEngine;

public class ChessTimer : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI whiteTimerText;
    public TextMeshProUGUI blackTimerText;
    public void Setup(float whiteTime, float blackTime)
    {
        UpdateTimes(whiteTime,blackTime);
    }
    public void UpdateTimes(float whiteTime, float blackTime)
    {
        whiteTimerText.text = ReformatTime(whiteTime);
        blackTimerText.text = ReformatTime(blackTime);
    }
    private string ReformatTime(float time)
    {
        int minutes = ((int)time)/60;
        int seconds = ((int)time)%60;
        return string.Format("{0}:{1:00}", minutes, seconds);
    }
}