using ChessChallenge.API;
using System;
using System.Collections.Generic;


namespace ChessChallenge.Example
{

	public class EvilBot : IChessBot
	{
		//Logic stats
		int[] selfPieceWeights =
		{
		0, //Unused - None
		100, //Pawn
		200, //Knight
		300, //Bishop
		350, //Rook
		500, //Queen
		900, //King
	};
		int enemyPieceWeightModifier = 30;
		int[] enemyPieceWeights =
		{
		0, //Unused - None
		100, //Pawn
		130, //Knight
		150, //Bishop
		180, //Rook
		210, //Queen
		400, //King
	};

		//State cache
		Board _board;
		//int[] moveValues = new int[256];
		bool isWhiteTeam;
		Random fallbackRand = new(150);

		public Move Think(Board board, Timer timer)
		{
			_board = board;
			Move[] moves = GetLegalMoves();

			if (timer.MillisecondsRemaining < 50)
			{
				//Panic time!
				//We can NOT run out of time, so use a faster method of picking
				//based on random chance - it's at least something
				//Console.WriteLine("SOMETHING BAD");
				return moves[fallbackRand.Next(moves.Length)];
			}

			isWhiteTeam = _board.IsWhiteToMove;

			//Acquire all the move values and pick the best one
			int highestMoveIndex = 0;
			int highestValue = int.MinValue;

			for (int i = 0; i < moves.Length; i++)
			{
				var thisMove = moves[i];
				//Apply the move so we can do heuristics outside of minimax later
				_board.MakeMove(thisMove);
				//First, run the results of minimax
				int thisMoveValue = MiniMaxAbsolute(3, true, isWhiteTeam ? 1 : -1);
				//Add heuristic modifiers for this single move

				//Now undo the move to return the board state
				_board.UndoMove(thisMove);

				if (thisMoveValue > highestValue)
				{
					highestValue = thisMoveValue;
					highestMoveIndex = i;
				}
			}

			//Console.WriteLine("Highest value: " + highestValue);
			return moves[highestMoveIndex];
		}

		//MAIN LOGIC

		/// <summary>
		/// Run Minimax on the current move
		/// </summary>
		/// <param name="depth">How many times to continue the search</param>
		/// <param name="thisTeamTurn">Whether we're making the move or not</param>
		/// <returns></returns>
		int MiniMaxAbsolute(int depth, bool thisTeamTurn, int teamMul)
		{

			//Since there might not be legal moves by this point,
			//We need to escape if the game has ended
			if (_board.IsInCheckmate() || _board.IsDraw())
			{
				//Make sure to return board state
				//_board.UndoMove(currentMove);

				if (thisTeamTurn)
				{
					return 40000;
				}
				else
				{
					return -40000;
				}
			}

			if (depth == 0)
			{
				//Count up number of pieces we have left
				//And also subtract the number of enemy pieces left
				//(with some modifiers)

				int finalValue = 0;
				foreach (PieceType thisType in Enum.GetValues(typeof(PieceType)))
				{
					//For each type, get the count and multiply by the weight of that type
					if (thisType == PieceType.None)
					{
						continue;
					}

					finalValue += (_board.GetPieceList(thisType, isWhiteTeam).Count *
						selfPieceWeights[(int)thisType]);
					finalValue -= (_board.GetPieceList(thisType, !isWhiteTeam).Count *
						(selfPieceWeights[(int)thisType]));
					//finalValue -= _board.GetPieceList(thisType, !whiteToMove).Count
					//	* (selfPieceWeights[(int)thisType] - enemyPieceWeightModifier);
				}
				return finalValue;

				/*
				ulong bitboardToUse = _board.BlackPiecesBitboard;
				if(isWhiteTeam)
				{
					bitboardToUse = _board.WhitePiecesBitboard;
				}
				int numOfPieces = 0;
				//Check each bit for a possible piece
				for(int i = 0; i < 64; i++)
				{
					if((bitboardToUse & (ulong)(1 << i)) == 1)
					{
						numOfPieces++;
					}
				}

				return numOfPieces;
				*/
			}



			//Get the new moves for the next turn
			var nextMoves = GetLegalMoves();
			//Since we're now looking at the NEXT moves and not the one that was just made,
			//thisTeamTurn actually means we should minimize, not maximize
			int bestValue = int.MinValue;
			if (thisTeamTurn)
			{
				bestValue = int.MaxValue;
			}
			if (nextMoves.Length == 0)
			{
				Console.WriteLine("UH OH");
			}
			//TODO: Make this negamax instead

			//Console.WriteLine(nextMoves.Length);
			foreach (Move nextMove in nextMoves)
			{
				/*
				int thisVal = -MiniMaxAbsolute(nextMove, depth - 1, !thisTeamTurn, teamMul * -1);
				//High value is for us, low value is for opponent
				bestValue = Math.Max(thisVal, bestValue);
				*/

				//Make the next move for next iteration/eval
				_board.MakeMove(nextMove);

				//Higher value = we're winning
				//Lower value = enemy is winning
				int thisVal = MiniMaxAbsolute(depth - 1, !thisTeamTurn, teamMul * -1);

				//Return the board state
				_board.UndoMove(nextMove);

				if (thisTeamTurn)
				{
					//Console.WriteLine("T: " + thisVal + " | B: " + bestValue);
					bestValue = Math.Min(thisVal, bestValue);
				}
				else
				{
					//Console.WriteLine("ET: " + thisVal + " | B: " + bestValue);
					bestValue = Math.Max(thisVal, bestValue);
				}
			}

			return bestValue;
		}


		//HELPERS

		/// <summary>
		/// Helper to return legal moves for saving characters
		/// </summary>
		/// <returns></returns>
		Move[] GetLegalMoves()
		{
			return _board.GetLegalMoves();
		}
	}

	// A simple bot that can spot mate in one, and always captures the most valuable piece it can.
	// Plays randomly otherwise.
	public class OldEvilBot : IChessBot
    {
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();

            // Pick a random move to play if nothing better is found
            Random rng = new();
            Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
            int highestValueCapture = 0;

            foreach (Move move in allMoves)
            {
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    moveToPlay = move;
                    break;
                }

                // Find highest value capture
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                if (capturedPieceValue > highestValueCapture)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }

            return moveToPlay;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }
    }


}
