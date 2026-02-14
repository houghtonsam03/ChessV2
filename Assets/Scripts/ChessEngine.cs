using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

public class ChessEngine : MonoBehaviour
{
    public const string startingFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public float timeDelay = 0.1f;
    public enum Player1Side {White,Black,Random};
    public Player1Side PlayerOneSide;
    public bool humanPlayer1;
    public ChessAgent agent1;
    public bool humanPlayer2;
    public ChessAgent agent2;

    // System variables
    private bool player1white;
    private int turn;
    private ChessAgent[] agents;
    private Chessboard board;
    // Chess variables
    private string gameState;
    private bool gameOver = false;
    
    void Start()
    {
        // Set Side
        if (PlayerOneSide == Player1Side.White) player1white = true;
        if (PlayerOneSide == Player1Side.Black) player1white = false;
        if (PlayerOneSide == Player1Side.Random) player1white = Random.value < 0.5f;
        
        // Start agents
        agents = new ChessAgent[2]{agent1,agent2};

        if (humanPlayer1 || agent1 == null) agents[0] = null;
        else agents[0].StartAgent(player1white);
        if (humanPlayer2 || agent2 == null) agents[1] = null;
        else agents[1].StartAgent(!player1white);

        // Infer What agent starts
        turn = player1white ? 0 : 1;

        // Create board
        board = GameObject.FindAnyObjectByType<Chessboard>();
        board.setEngine(this);

    }

    void Update()
    {
        if (!gameOver)
        {   
            // Time Delay
            StartCoroutine(Wait());

            if (agents[turn] == null) // Human
            {
                return;
            }

            else // AI Agent
            {
                // Main AI Loop
                ChessAgent agent = agents[turn];
                agent.setState(gameState);
                int[] move = agent.getMove();
                Move(move[0],move[1]);
                turn = turn ^ 1;
                board.setState(gameState);
            }   
        }
    }
    IEnumerator Wait()
    {
        yield return new WaitForSeconds(timeDelay);
    }

    public bool Move(int start,int target)
    {
        return true;
    }
}
