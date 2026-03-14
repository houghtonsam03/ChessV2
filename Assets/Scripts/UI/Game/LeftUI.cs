using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LeftUI : MonoBehaviour
{
    [Header("UI References")]
    private TextMeshProUGUI whiteTimerText;
    private GameObject whiteEvalObject;
    private TextMeshProUGUI whiteEvalText;
    private TextMeshProUGUI blackTimerText;
    private GameObject blackEvalObject;
    private TextMeshProUGUI blackEvalText;
    public static readonly float textEvalLimit = 20f; 
    public void Setup(float whiteTime, float blackTime,float?[] evals)
    {
        whiteTimerText = this.gameObject.transform.Find("White").Find("Timer").Find("Time").GetComponent<TextMeshProUGUI>();
        blackTimerText = this.gameObject.transform.Find("Black").Find("Timer").Find("Time").GetComponent<TextMeshProUGUI>();
        whiteEvalObject = this.gameObject.transform.Find("White").Find("Eval").transform.gameObject;
        blackEvalObject = this.gameObject.transform.Find("Black").Find("Eval").transform.gameObject;
        whiteEvalText = this.gameObject.transform.Find("White").Find("Text").GetComponent<TextMeshProUGUI>();
        blackEvalText = this.gameObject.transform.Find("Black").Find("Text").GetComponent<TextMeshProUGUI>();
        UpdateTimes(whiteTime,blackTime);
        UpdateEval(evals);
    }
    public void UpdateTimes(float whiteTime, float blackTime)
    {
        whiteTimerText.text = ReformatTime(whiteTime);
        blackTimerText.text = ReformatTime(blackTime);
    }
    public void FlipUI()
    {
        this.gameObject.transform.Find("White").gameObject.transform.position += Vector3.up*6;
        this.gameObject.transform.Find("White").Find("Timer").transform.position += Vector3.down*4.8f;

        this.gameObject.transform.Find("Black").gameObject.transform.position += Vector3.down*6;
        this.gameObject.transform.Find("Black").Find("Timer").transform.position += Vector3.up*4.8f;
    }
    public void UpdateEval(float?[] evals)
    {
        float? whiteEval = evals[0];
        float? blackEval = evals[1];
        float whiteBarHeight = sigmoidEval(whiteEval);
        whiteEvalObject.transform.Find("White").GetComponent<LayoutElement>().flexibleHeight = whiteBarHeight;
        whiteEvalObject.transform.Find("Black").GetComponent<LayoutElement>().flexibleHeight = 1 - whiteBarHeight;
        if (whiteEval.HasValue && Math.Abs(whiteEval.Value) < textEvalLimit) whiteEvalText.text = whiteEval.Value.ToString("F2");
        else whiteEvalText.text = "";
        whiteEvalText.gameObject.transform.position = this.transform.position + new Vector3(-0.4f,-3+3.5f*(whiteBarHeight-0.5f),0);


        float blackBarHeight = sigmoidEval(blackEval);
        blackEvalObject.transform.Find("Black").GetComponent<LayoutElement>().flexibleHeight = blackBarHeight;
        blackEvalObject.transform.Find("White").GetComponent<LayoutElement>().flexibleHeight = 1 - blackBarHeight;
        if (blackEval.HasValue && Math.Abs(blackEval.Value) < textEvalLimit) blackEvalText.text = (-blackEval.Value).ToString("F2");
        else blackEvalText.text = "";
        blackEvalText.gameObject.transform.position = this.transform.position + new Vector3(-0.4f,3-3.5f*(blackBarHeight-0.5f),0);

    }
    private string ReformatTime(float time)
    {
        int minutes = ((int)time)/60;
        int seconds = ((int)time)%60;
        return string.Format("{0}:{1:00}", minutes, seconds);
    }
    private float sigmoidEval(float? eval)
    {
        if (eval.HasValue) return 1/(1+Mathf.Exp(-0.3f*eval.Value));
        else return 0.5f;
    }
}