using ChessChallenge.API;
using System.Collections.Generic;
using System;
using System.Linq;

public class MyBot : IChessBot
{

    Dictionary<ulong, double> evaluationsCache = new Dictionary<ulong, double>();
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

        if (evaluationsCache.ContainsKey(board.ZobristKey)) return evaluationsCache[board.ZobristKey];

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

        evaluationsCache[board.ZobristKey] = score;

        return score;
    }

    private Move ChooseBestMove(Move[] candidateMoves, Board board, int maxDepth) {

        double currentEval = EvaluatePosition(board);
        
        double[] evals = new double[candidateMoves.Length];
        for (int i=0; i < candidateMoves.Length; i++) {
            double moveEval = 0.0;
            board.MakeMove(candidateMoves[i]);
            
            double thisPositionEval = EvaluatePosition(board);


            Move[] candidateReplies = board.GetLegalMoves();
            if (board.IsInCheckmate()) moveEval = board.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;
            else if (thisPositionEval - currentEval < -3) moveEval = double.NegativeInfinity;
            else if (candidateReplies.Length == 0) moveEval = 0;
            else if (maxDepth == 0) moveEval = thisPositionEval;
            else {
                Move bestReply = ChooseBestMove(candidateReplies, board, maxDepth-1);
                board.MakeMove(bestReply);
                moveEval = EvaluatePosition(board);
                board.UndoMove(bestReply);
            }
            board.UndoMove(candidateMoves[i]);
            evals[i] = moveEval;
        }

        Array.Sort(evals, candidateMoves);
        return board.IsWhiteToMove ? candidateMoves[candidateMoves.Length -1] : candidateMoves[0];
    }

    public Move Think(Board board, Timer timer)
    {        
        return ChooseBestMove(board.GetLegalMoves(), board, 1);
    }
}