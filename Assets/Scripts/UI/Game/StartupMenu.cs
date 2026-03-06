using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartupMenu : MonoBehaviour
{
    private int p1Toggle;
    private int p2Toggle;
    private int colourToggle;

    private Image p1;
    private Image p2;
    private TMP_Dropdown timeDropdown;
    void Start()
    {
        p1 = gameObject.transform.Find("Panel").Find("Player1").GetComponent<Image>();
        p2 = gameObject.transform.Find("Panel").Find("Player2").GetComponent<Image>();
        timeDropdown = gameObject.transform.Find("Panel").Find("Time").GetComponent<TMP_Dropdown>();
        p1Toggle = 0; p2Toggle = 0; colourToggle = 0;
        SetImage();
    }
    public void StartGame()
    {
        ChessGame game = GameObject.Find("Game").GetComponent<ChessGame>();

        bool p1White;

        switch (colourToggle)
        {
            case 0: p1White = true; break;
            case 1: p1White = false; break;
            default: p1White = UnityEngine.Random.value <0.5f; break;
        }

        string selectedText = timeDropdown.options[timeDropdown.value].text;
        float time = ParseSeconds(selectedText);

        game.Begin(p1White,p1Toggle==1,p2Toggle==1,time,true);
        this.gameObject.SetActive(false);
    }
    public void ToggleColour()
    {
        colourToggle += 1;
        colourToggle %= 3;
        SetImage();
    }
    public void ToggleP1()
    {
        p1Toggle += 1;
        p1Toggle %= 2;
        SetImage();
    }
    public void ToggleP2()
    {
        p2Toggle += 1;
        p2Toggle %= 2;
        SetImage();
    }
    private void SetImage()
    {
        string p1Image = "";
        string p2Image = "";
        switch (colourToggle)
        {
            case 0: p1Image += "white"; p2Image += "black"; break;
            case 1: p1Image += "black"; p2Image += "white"; break;
            default: p1Image += "random"; p2Image += "random"; break;
        }
        if (p1Toggle == 0) p1Image += "Player";
        else p1Image += "Robot";

        if (p2Toggle == 0) p2Image += "Player";
        else p2Image += "Robot";

        p1.sprite = Resources.Load<Sprite>(p1Image);
        p2.sprite = Resources.Load<Sprite>(p2Image);
    }
    public float ParseSeconds(string text)
    {
        string[] parts = text.Split(":");
        int.TryParse(parts[0],out int minutes);
        int.TryParse(parts[1],out int seconds);
        return minutes*60 + seconds;
    }
}