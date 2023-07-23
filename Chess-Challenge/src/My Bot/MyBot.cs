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
	int[] moveValues = new int[256];
	bool isWhiteTeam;
	Random fallbackRand = new Random(150);

	public Move Think(Board board, Timer timer)
	{
		_board = board;
		Move[] moves = GetLegalMoves();

		if(timer.MillisecondsRemaining < 50)
		{
			//Panic time!
			//We can NOT run out of time, so use a faster method of picking
			//based on random chance - it's at least something
			return moves[fallbackRand.Next(moves.Length)];
		}

		isWhiteTeam = _board.IsWhiteToMove;

		//Acquire all the move values through minimax
		for (int i = 0; i < moves.Length; i++)
		{
			moveValues[i] = MiniMaxAbsolute(moves[i], 2, true);
		}
		//Find the best value and select it!
		//Moves that tie with highest are ignored
		int highestMoveIndex = 0;
		int highestValue = -40000;
		for (int i = 0; i < moves.Length; i++)
		{
			if (moveValues[i] > highestValue)
			{
				highestValue = moveValues[i];
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
	int MiniMaxAbsolute(Move currentMove, int depth, bool thisTeamTurn)
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
				if(thisType == PieceType.None)
				{
					continue;
				}

				//TODO: The other player should be maximizing their moves as well

				finalValue += _board.GetPieceList(thisType, isWhiteTeam).Count 
					* selfPieceWeights[(int)thisType];
				finalValue -= _board.GetPieceList(thisType, !isWhiteTeam).Count
					* (selfPieceWeights[(int)thisType] - enemyPieceWeightModifier);
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

		//Apply the move
		_board.MakeMove(currentMove);

		//Get the new moves for the next turn
		var nextMoves = GetLegalMoves();
		int highestValue = -40000;
		foreach (Move nextMove in nextMoves)
		{
			int thisVal = MiniMaxAbsolute(nextMove, depth - 1, !thisTeamTurn);
			highestValue = Math.Max(thisVal, highestValue);
		}

		//Make sure to undo the move before leaving this branch
		_board.UndoMove(currentMove);
		return highestValue;
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