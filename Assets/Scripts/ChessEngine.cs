using System.Collections;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;

public class ChessEngine : MonoBehaviour
{
    public const string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public bool graphics;
    public float timeDelay = 0.1f;
    public enum Player1Side {White,Black,Random};
    public Player1Side PlayerOneSide;
    public bool humanPlayer1;
    public ChessAgent agent1;
    public bool humanPlayer2;
    public ChessAgent agent2;

    // System variables
    private bool player1white;
    private ChessAgent[] agents;
    private Chessboard board;
    private PlayerListener playerListener;

    // Timer variables;
    private float timer;
    // Chess variables
    private string gameState;
    private bool isWhiteTurn;
    private bool gameOver = false;
    
    void Start()
    {
        // Initial gameState
        gameState = startingFen;

        // Spawn chessboard and PlayerListener if we want graphics
        if (graphics || humanPlayer1 || humanPlayer2)
        {
            GameObject prefab = Resources.Load<GameObject>("ChessboardPrefab");
            GameObject boardObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
            board = boardObject.GetComponent<Chessboard>();
            board.setState(startingFen);
            PlayerListener playerListener = board.AddComponent<PlayerListener>();
            playerListener.Setup(this,board);
        }

        // Set Side
        if (PlayerOneSide == Player1Side.White) player1white = true;
        if (PlayerOneSide == Player1Side.Black) player1white = false;
        if (PlayerOneSide == Player1Side.Random) player1white = Random.value < 0.5f;
        isWhiteTurn = true;
        
        // Start agents
        agents = new ChessAgent[2]{agent1,agent2};
        if (humanPlayer1 || agent1 == null) agents[0] = null;
        else agents[0].StartAgent(player1white);
        if (humanPlayer2 || agent2 == null) agents[1] = null;
        else agents[1].StartAgent(!player1white);

    }

    void Update()
    {
        if (!gameOver)
        {   
            // Time Delay
            timer += Time.deltaTime;
            if (timer < timeDelay) return;
            
            // Find the index of the player whose turn it is.
            int turnIndex = (player1white & isWhiteTurn) || (!player1white & !isWhiteTurn) ? 0 : 1;
            if (agents[turnIndex] == null) // Human
            {
                // We wait for playerListener to make move.
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                agents[turnIndex].setState(gameState);
                int[] move = agents[turnIndex].getMove();
                Move(move[0],move[1]);

                // Time delay logic
                timer = 0;
            }   
        }
    }
    public void Move(int start,int target)
    {
        if (isLegalMove(start,target))
        {
            isWhiteTurn = !isWhiteTurn;
            // Chess State logic.

        }
        // Debug Logic
        else
        {
            string s = "";
            if (isWhiteTurn) s += "white";
            else s+= "black";
            Debug.Log($"Illegal move by {s} player.");
        }
    }
    public bool isLegalMove(int start,int target)
    {
        // Place holder
        return false;
    }
}
