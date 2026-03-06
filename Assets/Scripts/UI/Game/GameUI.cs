using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using static ChessSound;

public class GameUI : MonoBehaviour
{
    private ChessGame game;
    private BoardUI boardUI;
    private PlayerListener playerListener;
    private ChessSound sound;
    private LeftUI leftUI;
    private RightUI rightUI;
    private GameoverMenu gameoverMenu;
    private Board board;

    public void Setup(ChessGame g,Board b,bool player1White,ChessAgent[] agents,float?[] evals,float TimeLimit)
    {
        // Chess Game
        game = g;
        GameObject gameObject = g.gameObject;
        board = new Board();
        board.setPos(b);

        // Board UI
        GameObject prefab = Resources.Load<GameObject>("Chessboard");
        GameObject boardObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        boardObject.transform.parent = gameObject.transform;
        boardUI = boardObject.GetComponent<BoardUI>();
        bool isFlipped = agents[0] != null && agents[1] == null;
        boardUI.Setup(isFlipped);
        boardUI.readBoard(board);

        // Player Input Listener
        playerListener = boardUI.GetComponent<PlayerListener>();
        playerListener.Setup(game,boardUI,this,agents,isFlipped);

        // Sound
        prefab = Resources.Load<GameObject>("Speaker");
        GameObject speakerObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        speakerObject.transform.parent = gameObject.transform;
        sound = boardUI.GetComponent<ChessSound>();
        sound.SetSpeaker(speakerObject.GetComponent<AudioSource>());
        sound.PlayStartSound();
    
        // Left UI (Timers & Evals)
        prefab = Resources.Load<GameObject>("LeftUI");
        GameObject leftObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        leftObject.transform.SetParent(gameObject.transform);
        leftUI = leftObject.GetComponent<LeftUI>();
        leftUI.Setup(TimeLimit,TimeLimit,evals);
        if (isFlipped) leftUI.FlipUI();

        // Right UI (Game History, New Game & Draw Offer/Resign)
        prefab = Resources.Load<GameObject>("RightUI");
        GameObject rightObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        rightObject.transform.SetParent(gameObject.transform);
        rightUI = rightObject.GetComponent<RightUI>();
        rightUI.Setup();

        // Gameover menu (inactive)
        prefab = Resources.Load<GameObject>("GameoverMenu");
        GameObject menuObject = Instantiate(prefab,prefab.transform.position,Quaternion.identity);
        menuObject.transform.SetParent(gameObject.transform);
        gameoverMenu = menuObject.GetComponent<GameoverMenu>();
        gameoverMenu.Setup(this,game,player1White);

    }
    public void Rematch(Board b,ChessAgent[] agents,float?[] evals,float TimeLimit)
    {
        board.setPos(b);

        bool isFlipped = agents[0] != null && agents[1] == null;
        boardUI.Setup(isFlipped);
        boardUI.readBoard(board);

        playerListener.NewGame(agents,isFlipped);

        sound.PlayStartSound();

        leftUI.UpdateTimes(TimeLimit,TimeLimit);
        leftUI.UpdateEval(evals);

        rightUI.Clean();

        gameoverMenu.Close();
    }
    public void UpdateGraphics(float whiteTime,float blackTime,float?[] evals)
    {
        leftUI.UpdateTimes(whiteTime,blackTime);
        leftUI.UpdateEval(evals);
    }
    public void EndTurn(Move move,bool isWhite,bool isCapture,bool isCheck, bool isCheckmate)
    {
        playerListener.EndTurn();

        int movingPiece = board.GetPieceType(move.StartSquare);
        string moveStr = Move.AlgebraicNotation(move,isCheck,isCheckmate,isCapture,movingPiece);
        rightUI.UpdateHistory(moveStr,board.colourToMove==Piece.white);

        board.MakeMove(move);
        boardUI.readBoard(board);
        

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

        boardUI.DrawGameOver(winner,whiteKingPos,blackKingPos);
        gameoverMenu.Show(state);
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