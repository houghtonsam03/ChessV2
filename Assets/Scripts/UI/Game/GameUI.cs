using UnityEngine;

public class GameUI : MonoBehaviour
{
    private ChessGame game;
    private BoardUI boardUI;
    private PlayerListener playerListener;
    private ChessTimer chessTimer;

    public void Setup(ChessGame g,Board board,bool p1White,ChessAgent[] agents,float[] evals,float TimeLimit)
    {
        // Chess Game
        game = g;
        GameObject gameObject = g.gameObject;

        // Board UI
        GameObject prefab = Resources.Load<GameObject>("Chessboard");
        GameObject boardObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
        boardObject.transform.parent = gameObject.transform;
        boardUI = boardObject.GetComponent<BoardUI>();
        boardUI.readBoard(board);

        // Player Input Listener
        playerListener = boardUI.GetComponent<PlayerListener>();
        playerListener.Setup(game,boardUI,this,p1White,agents);
    
        // Chess Timer
        prefab = Resources.Load<GameObject>("ChessTimer");
        GameObject timerObject = Instantiate(prefab,Vector3.zero,Quaternion.identity);
        timerObject.transform.parent = gameObject.transform;
        chessTimer = timerObject.GetComponent<ChessTimer>();
        chessTimer.Setup(TimeLimit,TimeLimit);
    }
    public void UpdateGraphics(Board board,float whiteTime,float blackTime)
    {
        boardUI.readBoard(board);
        chessTimer.UpdateTimes(whiteTime,blackTime);
    }
    public void EndTurn()
    {
        playerListener.EndTurn();
    }
    public void EndGame(int state,int whiteKingPos,int blackKingPos)
    {
        if (state == 0) return; // Not intended use of function

        int winner = 2;
        if (state < 4) winner = 1;
        if (state > 6) winner = 0;

        ChessGame.PrintState(state);
        boardUI.DrawGameOver(winner,whiteKingPos,blackKingPos);
        playerListener.EndGame();
    }
    public void DrawBitboard(ulong bitboard)
    {
        boardUI.DrawBitboard(bitboard);
    }
}