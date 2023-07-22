using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

public class MyBot : IChessBot
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
                evals[i] = EvaluatePosition(board);
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

        // I'm a pusher
        for (int i=legalMoves.Length - 1; (i>= 0 && i>=legalMoves.Length -4); i--) {
            if (legalMoves[i].MovePieceType == PieceType.Pawn) {
                return legalMoves[i];
            }
        }

        return legalMoves[legalMoves.Length - 1];
    }
}