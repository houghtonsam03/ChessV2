using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveTester : MonoBehaviour
{
    private int n = 1;
    private Board board;
    void Start()
    {
        board = new Board();
        board.setPos(Board.startingFen);
        
    }
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            Debug.Log(Perft(board,n));
            n++;
        }
    }
    public static long Perft(Board board,int depth)
    {

        if (depth == 0) return 1;
        List<Move> moves = ChessGame.GenerateMoves(board,board.colourToMove);
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