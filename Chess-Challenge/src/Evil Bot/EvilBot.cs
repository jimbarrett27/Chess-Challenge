using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessChallenge.Example
{
    // A simple bot that can spot mate in one, and always captures the most valuable piece it can.
    // Plays randomly otherwise.
    // public class EvilBot : IChessBot
    // {
    //     // Piece values: null, pawn, knight, bishop, rook, queen, king
    //     int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    //     public Move Think(Board board, Timer timer)
    //     {
    //         Move[] allMoves = board.GetLegalMoves();

    //         // Pick a random move to play if nothing better is found
    //         Random rng = new();
    //         Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
    //         int highestValueCapture = 0;

    //         foreach (Move move in allMoves)
    //         {
    //             // Always play checkmate in one
    //             if (MoveIsCheckmate(board, move))
    //             {
    //                 moveToPlay = move;
    //                 break;
    //             }

    //             // Find highest value capture
    //             Piece capturedPiece = board.GetPiece(move.TargetSquare);
    //             int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

    //             if (capturedPieceValue > highestValueCapture)
    //             {
    //                 moveToPlay = move;
    //                 highestValueCapture = capturedPieceValue;
    //             }
    //         }

    //         return moveToPlay;
    //     }

    //     // Test if this move gives checkmate
    //     bool MoveIsCheckmate(Board board, Move move)
    //     {
    //         board.MakeMove(move);
    //         bool isMate = board.IsInCheckmate();
    //         board.UndoMove(move);
    //         return isMate;
    //     }
    // }

    public class EvilBot : IChessBot
{

    private double EvaluatePosition(Board board) {

        if (board.IsInCheckmate()) return board.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;

        Dictionary<PieceType, double> pieceValues = new Dictionary<PieceType, double>();
        pieceValues[PieceType.Pawn] = 1.0;
        pieceValues[PieceType.Knight] = 3.0;
        pieceValues[PieceType.Bishop] = 3.0;
        pieceValues[PieceType.Rook] = 5.0;
        pieceValues[PieceType.Queen] = 9.0;
        
        double score = 0.0;

        PieceList[] allPieceList = board.GetAllPieceLists();
        for (int i=0; i<allPieceList.Length; i++) {
            PieceList pieceList = allPieceList[i];
            if (pieceList.TypeOfPieceInList == PieceType.King) continue;
            score += pieceList.Count * (pieceList.IsWhitePieceList ? 1.0 : -1.0) * pieceValues[pieceList.TypeOfPieceInList];
        }
        return score;
    }

    private double EvaluateMove(Board board, Move move) {

        double finalEval;

        board.MakeMove(move);

        Move[] legalMoves = board.GetLegalMoves();

        if (board.IsInCheckmate()) finalEval = board.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;

        else if (legalMoves.Length == 0) finalEval = 0;

        else {
            double[] evals = new double[legalMoves.Length];
            for (int i=0; i< legalMoves.Length; i++) {
                board.MakeMove(legalMoves[i]);
                evals[i] = EvaluatePosition(board) + (legalMoves[i].MovePieceType == PieceType.Pawn ? 1 : 0);
                board.UndoMove(legalMoves[i]);

            }
            finalEval = board.IsWhiteToMove ? evals.Max() : evals.Min();
        }
        
        board.UndoMove(move);

        return finalEval;
    }

    public Move Think(Board board, Timer timer)
    {

        Random rand = new Random();
        Move[] legalMoves = board.GetLegalMoves();
        
        double scoreMultipler = board.IsWhiteToMove ? 1.0 : -1.0;
        double[] evals = new double[legalMoves.Length];
        for (int i=0; i<legalMoves.Length; i++) {
            evals[i] = scoreMultipler * EvaluateMove(board, legalMoves[i]);
        }

        Array.Sort(evals, legalMoves);

        return legalMoves[legalMoves.Length - 1];
    }
}
}