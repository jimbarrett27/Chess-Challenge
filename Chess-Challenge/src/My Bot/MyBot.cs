using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    private int getGamePhase(Board board) {
        // 0 = opening, 1 = middlegame, 2 = endgame
        if (board.PlyCount < 16) return 0;
        else if (board.PlyCount < 60) return 1;
        else return 2;
    }

    private double scoreKing(Board board, Piece king) {

        double score = 0.0;
        int goodRank = king.IsWhite ? 0 : 7;
        if (getGamePhase(board) < 3) {
            score += Math.Abs(king.Square.Rank - goodRank) == 0 ? 0.1 : -0.5; 
        }
        return score;

    }

    private double scoreQueen(Board board, Piece queen) {
        return 9.0 + ((1.0/64.0) * BitboardHelper.GetNumberOfSetBits(
            BitboardHelper.GetSliderAttacks(PieceType.Queen, queen.Square, board)
        ));
    }

    private double scoreRook(Board board, Piece rook) {
        return 5.0 + ((1.0/64.0) * BitboardHelper.GetNumberOfSetBits(
            BitboardHelper.GetSliderAttacks(PieceType.Rook, rook.Square, board)
        ));
    }

    private double scoreBishop(Board board, Piece bishop) {
        return 3.0 + ((1.0/64.0) * BitboardHelper.GetNumberOfSetBits(
            BitboardHelper.GetSliderAttacks(PieceType.Bishop, bishop.Square, board)
        ));
    }

    private double scoreKnight(Board board, Piece knight) {
        return 3.0 + ((1.0/64.0) * Math.Abs(knight.Square.Index - 31));
    }

    private double scorePawn(Board board, Piece pawn) {
        return 1.0;
    }

    private double EvaluatePosition(Board board) {

        if (board.IsInCheckmate()) return board.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;
        
        double score = 0.0;
        PieceList[] allPieceList = board.GetAllPieceLists();
        for (int i=0; i<allPieceList.Length; i++) {
            PieceList pieceList = allPieceList[i];
            foreach (Piece piece in pieceList) {
                double pieceScore = 0.0;
                switch (pieceList.TypeOfPieceInList){
                    case PieceType.King:
                        pieceScore += scoreKing(board, piece);
                        break;
                    case PieceType.Queen:
                        pieceScore += scoreQueen(board, piece);
                        break;
                    case PieceType.Rook:
                        pieceScore += scoreRook(board, piece);
                        break;
                    case PieceType.Bishop:
                        pieceScore += scoreBishop(board, piece);
                        break;
                    case PieceType.Knight:
                        pieceScore += scoreKnight(board, piece);
                        break;
                    case PieceType.Pawn:
                        pieceScore += scorePawn(board, piece);
                        break;
                }

                score += pieceScore * (pieceList.IsWhitePieceList ? 1.0 : -1.0);
            }
            if (pieceList.TypeOfPieceInList == PieceType.King) continue;
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

        // Console.WriteLine(EvaluatePosition(board));

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