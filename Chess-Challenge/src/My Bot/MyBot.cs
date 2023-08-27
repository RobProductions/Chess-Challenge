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

		//Apply the move first
		//To ensure that its accounted for in evaluation
		_board.MakeMove(currentMove);

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
			
			//Make sure to return the board state when leaving
			_board.UndoMove(currentMove);
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

		//Since there might not be legal moves by this point,
		//We need to escape if the game has ended
		if(_board.IsInCheckmate() || _board.IsDraw())
		{
			//Make sure to return board state
			_board.UndoMove(currentMove);

			if(thisTeamTurn)
			{
				return -40000;
			}
			else
			{
				return 40000;
			}
		}

		//Get the new moves for the next turn
		var nextMoves = GetLegalMoves();
		//Since we're now looking at the NEXT moves and not the one that was just made,
		//thisTeamTurn actually means we should minimize, not maximize
		int bestValue = int.MinValue;
		if(thisTeamTurn)
		{
			bestValue = int.MaxValue;
		}
		if(nextMoves.Length == 0)
		{
			Console.WriteLine("UH OH");
		}
		//Console.WriteLine(nextMoves.Length);
		foreach (Move nextMove in nextMoves)
		{
			/*
			int thisVal = -MiniMaxAbsolute(nextMove, depth - 1, !thisTeamTurn, teamMul * -1);
			//High value is for us, low value is for opponent
			bestValue = Math.Max(thisVal, bestValue);
			*/

			int thisVal = MiniMaxAbsolute(nextMove, depth - 1, !thisTeamTurn, teamMul * -1);

			if(thisTeamTurn)
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

		//Undo the move since we didn't return from eval
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