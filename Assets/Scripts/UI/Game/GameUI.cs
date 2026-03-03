using NUnit.Framework;
using UnityEngine;
using static ChessSound;

public class GameUI : MonoBehaviour
{
    private ChessGame game;
    private BoardUI boardUI;
    private PlayerListener playerListener;
    private ChessSound sound;
    private LeftUI leftUI;

    public void Setup(ChessGame g,Board board,bool p1White,ChessAgent[] agents,float? whiteEval,float? blackEval,float TimeLimit)
    {
        // Chess Game
        game = g;
        GameObject gameObject = g.gameObject;

        // Board UI
        GameObject prefab = Resources.Load<GameObject>("Chessboard");
        GameObject boardObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        boardObject.transform.parent = gameObject.transform;
        boardUI = boardObject.GetComponent<BoardUI>();
        boardUI.readBoard(board);

        // Player Input Listener
        playerListener = boardUI.GetComponent<PlayerListener>();
        playerListener.Setup(game,boardUI,this,p1White,agents);

        // Sound
        prefab = Resources.Load<GameObject>("Speaker");
        GameObject speakerObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        speakerObject.transform.parent = gameObject.transform;
        sound = boardUI.GetComponent<ChessSound>();
        sound.SetSpeaker(speakerObject.GetComponent<AudioSource>());
        sound.PlayStartSound();
    
        // Chess Timer
        prefab = Resources.Load<GameObject>("LeftUI");
        GameObject timerObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        timerObject.transform.SetParent(gameObject.transform);
        leftUI = timerObject.GetComponent<LeftUI>();
        leftUI.Setup(TimeLimit,TimeLimit,whiteEval,blackEval);

    }
    public void UpdateGraphics(Board board,float whiteTime,float blackTime,float? whiteEval, float? blackEval)
    {
        boardUI.readBoard(board);
        leftUI.UpdateTimes(whiteTime,blackTime);
        leftUI.UpdateEval(whiteEval,blackEval);

    }
    public void EndTurn(Move move,bool isWhite,bool isCheck, bool isCapture)
    {
        playerListener.EndTurn();
        if (isCheck) PlaySound(Sound.Check);
        else if (move.castling) PlaySound(Sound.Castle);
        else if (move.promotionPiece != 0) PlaySound(Sound.Promotion);
        else if (isCapture) PlaySound(Sound.Capture);
        else if (isWhite) PlaySound(Sound.MoveWhite);
        else if (!isWhite) PlaySound(Sound.MoveBlack);
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
        sound.PlayGameoverSound();
    }
    public void DrawBitboard(ulong bitboard)
    {
        boardUI.DrawBitboard(bitboard);
    }
    public void PlaySound(int s)
    {
        if (s < Sound.End) sound.PlayMoveSound(s);
        else if (s == Sound.TimeOut) sound.PlayTimeoutSound();
        else if (s == Sound.Illegal) sound.PlayIllegalSound();
        else if (s == Sound.Premove) sound.PlayPremoveSound(); 
    }
    
}