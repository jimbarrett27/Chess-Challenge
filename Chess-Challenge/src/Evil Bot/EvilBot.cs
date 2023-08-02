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


        if (board.IsInCheckmate()) return board.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;
        if (board.IsDraw()) return 0.0;

        if (evaluationsCache.ContainsKey(board.ZobristKey)) return evaluationsCache[board.ZobristKey];
        
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

    private double EvaluateMove(Board board, int depth, double alpha, double beta, bool playerIsMaximising) {

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw()) return EvaluatePosition(board);

        else if (playerIsMaximising) {
            double val = double.NegativeInfinity;
            foreach (Move move in board.GetLegalMoves()) {
                board.MakeMove(move);
                val = Math.Max(val, EvaluateMove(board, depth-1, alpha, beta, false));
                if (val > beta) {
                    board.UndoMove(move);
                    break;
                }
                alpha = Math.Max(alpha, val);
                board.UndoMove(move);
            }
            return val;
        }
        else {
            double val = double.PositiveInfinity;
            foreach (Move move in board.GetLegalMoves()) {
                board.MakeMove(move);
                val = Math.Min(val, EvaluateMove(board, depth-1, alpha, beta, true));
                if (val < alpha) {
                    board.UndoMove(move);
                    break;
                }
                beta = Math.Min(beta, val);
                board.UndoMove(move);
            }
            return val;
        }
    }

    public Move Think(Board board, Timer timer)
    {   
        Move[] candidateMoves = board.GetLegalMoves();
        double[] evals = new double[candidateMoves.Length];
        for (int i=0; i<candidateMoves.Length; i++){
            board.MakeMove(candidateMoves[i]);
            evals[i] = EvaluateMove(board, 2, double.NegativeInfinity, double.PositiveInfinity, board.IsWhiteToMove);
            board.UndoMove(candidateMoves[i]);
        }

        Array.Sort(evals, candidateMoves);

        return board.IsWhiteToMove ? candidateMoves[candidateMoves.Length - 1] : candidateMoves[0];  
    }
}
}