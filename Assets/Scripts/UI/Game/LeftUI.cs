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
    public void Setup(float whiteTime, float blackTime,float?[] evals)
    {
        whiteTimerText = this.gameObject.transform.Find("WhiteTimer").Find("WhiteTimerText").GetComponent<TextMeshProUGUI>();
        blackTimerText = this.gameObject.transform.Find("BlackTimer").Find("BlackTimerText").GetComponent<TextMeshProUGUI>();
        whiteEvalObject = this.gameObject.transform.Find("WhiteEval").transform.gameObject;
        blackEvalObject = this.gameObject.transform.Find("BlackEval").transform.gameObject;
        whiteEvalText = this.gameObject.transform.Find("WhiteText").GetComponent<TextMeshProUGUI>();
        blackEvalText = this.gameObject.transform.Find("BlackText").GetComponent<TextMeshProUGUI>();
        UpdateTimes(whiteTime,blackTime);
        UpdateEval(evals);
    }
    public void UpdateTimes(float whiteTime, float blackTime)
    {
        whiteTimerText.text = ReformatTime(whiteTime);
        blackTimerText.text = ReformatTime(blackTime);
    }
    public void UpdateEval(float?[] evals)
    {
        float? whiteEval = evals[0];
        float? blackEval = evals[1];
        float whiteBarHeight = sigmoidEval(whiteEval);
        whiteEvalObject.transform.Find("White").GetComponent<LayoutElement>().flexibleHeight = whiteBarHeight;
        whiteEvalObject.transform.Find("Black").GetComponent<LayoutElement>().flexibleHeight = 1 - whiteBarHeight;
        if (whiteEval.HasValue) whiteEvalText.text = whiteEval.Value.ToString();
        else whiteEvalText.text = "";
        whiteEvalText.gameObject.transform.position = this.transform.position + new Vector3(-0.4f,-3+3.5f*(whiteBarHeight-0.5f),0);


        float blackBarHeight = sigmoidEval(blackEval);
        blackEvalObject.transform.Find("Black").GetComponent<LayoutElement>().flexibleHeight = blackBarHeight;
        blackEvalObject.transform.Find("White").GetComponent<LayoutElement>().flexibleHeight = 1 - blackBarHeight;
        if (blackEval.HasValue) blackEvalText.text = (-blackEval.Value).ToString();
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
        if (eval.HasValue) return 1/(1+Mathf.Exp(-0.2f*eval.Value));
        else return 0.5f;
    }
}