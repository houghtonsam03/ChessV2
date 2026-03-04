using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RightUI : MonoBehaviour
{
    private TextMeshProUGUI historyText;
    private ScrollRect historyScrollRect;
    // Logic
    private int moveNumber;
    public void Setup()
    {
        historyText = this.gameObject.transform.Find("History").Find("Scroll View").Find("Viewport").Find("Content").GetComponent<TextMeshProUGUI>();
        historyScrollRect = this.gameObject.transform.Find("History").Find("Scroll View").GetComponent<ScrollRect>();
        moveNumber = 0;
    }
    public void UpdateHistory(string moveString, bool whiteToMove)
    {
        string s = "";
        if (whiteToMove) 
        {
            moveNumber++;
            s += $"{moveNumber}. <pos=15%>{moveString}";
        }
        else
        {
            s += $"<pos=60%>{moveString}\n";
        }
        historyText.text += s;
        bool isOverflowing = moveNumber > 10;
        bool isAtBottom = historyScrollRect.verticalNormalizedPosition <= 0.05f;
        if (!isOverflowing || isAtBottom) StartCoroutine(SnapToBottom());
    }

    private IEnumerator SnapToBottom()
    {
        // Wait for the end of the frame so the Content Size Fitter 
        // has finished resizing the text object to its new height
        yield return new WaitForEndOfFrame();
        
        // 0 is the bottom, 1 is the top
        historyScrollRect.verticalNormalizedPosition = 0f;
    }
}