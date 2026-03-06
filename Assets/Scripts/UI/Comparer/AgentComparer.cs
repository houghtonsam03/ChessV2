using System.Linq;
using Mono.Cecil.Cil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AgentComparer : MonoBehaviour
{
    public ChessAgent Agent1;
    public ChessAgent Agent2;
    public float TimeLimit;
    public int gameLimit;
    // Agents & Game
    private ChessAgent[] agents;
    private ChessGame currentGame;
    private GameObject chessObject;
    // Statistics
    private int gameCount = 1;
    private int[] states = new int[12];
    private int[] stateCounts = new int[4]; // Wins, Draws, Losses, Total

    // Layout Elements
    private LayoutElement[] bars;
    private TextMeshProUGUI[] texts;
    
    void Start()
    {
        // Start agents & game
        chessObject = new GameObject("Game");
        currentGame = chessObject.AddComponent<ChessGame>();
        bool p1White = UnityEngine.Random.value < 0.5f;
        currentGame.Agent1 = Agent1; currentGame.Agent2 = Agent2;
        currentGame.Begin(p1White,true,true,TimeLimit,false);

        // Get Elements
        bars = new LayoutElement[3];
        texts = new TextMeshProUGUI[3];
        bars[0] = transform.Find("StatsBar").Find("Win").GetComponent<LayoutElement>();
        bars[1] = transform.Find("StatsBar").Find("Draw").GetComponent<LayoutElement>();
        bars[2] = transform.Find("StatsBar").Find("Loss").GetComponent<LayoutElement>();
        texts[0] = transform.Find("Title").Find("Win").GetComponent<TextMeshProUGUI>();
        texts[1] = transform.Find("Title").Find("Draw").GetComponent<TextMeshProUGUI>();
        texts[2] = transform.Find("Title").Find("Loss").GetComponent<TextMeshProUGUI>();

    }
    void Update()
    {
        int endState = currentGame.GetEndState();
        if (endState != 0 && gameCount <= gameLimit)
        {
            gameCount++;
            states[endState]++;
            UpdateBar(endState);
            bool p1White = UnityEngine.Random.value < 0.5f;
            currentGame.Rematch(p1White);
            Debug.Log(ChessGame.StringState(endState));
        }
    }
    private void UpdateBar(int newState)
    {
        if (1 <= newState && newState <= 3) 
        {
            stateCounts[0]++;
            stateCounts[3]++;
        }
        if (4 <= newState && newState <= 6)
        {
            stateCounts[2]++;
            stateCounts[3]++;
        }
        if (7 <= newState && newState <= 12) 
        {
            stateCounts[1]++;
            stateCounts[3]++;    
        }
        float[] proportions = new float[3]{(float)stateCounts[0]/stateCounts[3],(float)stateCounts[1]/stateCounts[3],(float)stateCounts[2]/stateCounts[3]};
        for (int i=0;i<3;i++)
        {
            bars[i].flexibleWidth = proportions[i];
            texts[i].text = texts[i].text.Split(":")[0] + ": " + stateCounts[i];
        }
    }
}