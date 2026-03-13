using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveTester : MonoBehaviour
{
    public string testFen;
    private int n = 1;
    private Board board;
    private Stopwatch sw;    
    void Start()
    {
        if (testFen == "") testFen = Board.startingFen;
        board = new Board();
        board.setPos(testFen);
        sw = new Stopwatch();
    }
    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            long start = Stopwatch.GetTimestamp();
            long nodes = Perft(board,n);
            long total = Stopwatch.GetTimestamp()-start;
            double freq = (double)Stopwatch.Frequency;

            double totSec = total/freq;
            this.transform.Find("TextFrame").Find("Text").GetComponent<TextMeshProUGUI>().text += $"Perft({n}) = {nodes} | Total: {totSec}sec\n";
            n++;
        }
    }
    public long Perft(Board board,int depth)
    {
        Span<Move> moveStorage = stackalloc Move[256];
        int totalMoves = MoveGenerator.GenerateMoves(board,board.colourToMove,moveStorage);

        if (depth == 1) return totalMoves;

        long nodes = 0;
        for (int i=0;i<totalMoves;i++)
        {
            board.MakeMove(moveStorage[i]);
            nodes += Perft(board,depth-1);
            board.UndoMove();
        }
        return nodes;
    }
}