using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveTester : MonoBehaviour
{
    public string testFen;
    private int n = 1;
    private Board board;
    void Start()
    {
        if (testFen == "") testFen = Board.startingFen;
        board = new Board();
        board.setPos(testFen);
    }
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            float timeStart = Time.realtimeSinceStartup;
            long nodes = Perft(board,n);
            this.transform.Find("TextFrame").Find("Text").GetComponent<TextMeshProUGUI>().text += $"Perft({n}) = {nodes} ( {Time.realtimeSinceStartup-timeStart} sec)\n";
            n++;
        }
    }
    public static long Perft(Board board,int depth)
    {

        List<Move> moves = ChessGame.GenerateMoves(board,board.colourToMove);
        if (depth == 1) return moves.Count;
        long nodes = 0;
        for (int i=0;i<moves.Count;i++)
        {
            board.MakeMove(moves[i]);
            nodes += Perft(board,depth-1);
            board.UndoMove();
        }
        return nodes;
    }
}