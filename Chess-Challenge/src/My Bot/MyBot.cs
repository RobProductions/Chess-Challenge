using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
	//Logic stats
	int[] selfPieceWeights = 
	{
		0, //Unused - None
		100, //Pawn
		130, //Knight
		150, //Bishop
		180, //Rook
		210, //Queen
		400, //King
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

		if(timer.MillisecondsRemaining < 50)
		{
			//Panic time!
			//We can NOT run out of time, so use a faster method of picking
			//based on random chance - it's at least something
			Console.WriteLine("SOMETHING BAD");
			return moves[fallbackRand.Next(moves.Length)];
		}

		isWhiteTeam = _board.IsWhiteToMove;

		//Acquire all the move values and pick the best one
		int highestMoveIndex = 0;
		int highestValue = int.MinValue;

		for (int i = 0; i < moves.Length; i++)
		{
			//First, run the results of minimax
			int thisMoveValue = MiniMaxAbsolute(moves[i], 3, true, isWhiteTeam ? 1 : -1);
			//Add heuristic modifiers for this single move


			if(thisMoveValue > highestValue)
			{
				highestValue = thisMoveValue;
				highestMoveIndex = i;
			}
		}

		Console.WriteLine("Highest value: " + highestValue);
		return moves[highestMoveIndex];
	}

	//MAIN LOGIC

	/// <summary>
	/// Run Minimax on the current move
	/// </summary>
	/// <param name="currentMove">Move to start with</param>
	/// <param name="depth">How many times to continue the search</param>
	/// <param name="thisTeamTurn">Whether we're making the move or not</param>
	/// <returns></returns>
	int MiniMaxAbsolute(Move currentMove, int depth, bool thisTeamTurn, int teamMul)
	{

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
				finalValue += (_board.GetPieceList(thisType, true).Count * selfPieceWeights[(int)thisType]);
				finalValue -= (_board.GetPieceList(thisType, false).Count * selfPieceWeights[(int)thisType]);
				//finalValue -= _board.GetPieceList(thisType, !whiteToMove).Count
				//	* (selfPieceWeights[(int)thisType] - enemyPieceWeightModifier);
			}
			return finalValue * teamMul;

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

		/*
		if(_board.IsInCheckmate() || _board.IsDraw())
		{
			return 40000 * teamMul;
		}
		*/

		//Apply the move
		_board.MakeMove(currentMove);

		//Get the new moves for the next turn
		var nextMoves = GetLegalMoves();
		int bestValue = int.MinValue;

		foreach (Move nextMove in nextMoves)
		{
			int thisVal = -MiniMaxAbsolute(nextMove, depth - 1, !thisTeamTurn, teamMul * -1);
			//High value is for us, low value is for opponent
			bestValue = Math.Max(thisVal, bestValue);
		}

		//Make sure to undo the move before leaving this branch
		_board.UndoMove(currentMove);
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