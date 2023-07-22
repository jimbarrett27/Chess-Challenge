using ChessChallenge.API;
using System.Collections.Generic;
using System;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        Random rand = new Random();

        Move[] moves = board.GetLegalMoves();
        List<Move> pawnMoves = new List<Move>();
        foreach (Move move in moves) {
            if (move.MovePieceType == PieceType.Pawn) pawnMoves.Add(move);
        }

        if (pawnMoves.Count > 0) return pawnMoves[rand.Next(pawnMoves.Count)];
        
        return moves[rand.Next(moves.Length)];
    }
}