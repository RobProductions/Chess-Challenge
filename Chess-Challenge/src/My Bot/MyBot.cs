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
	const int repeatMoveHeuristic = -50;
	const int capKingHeuristic = 50;
	const int centerSquareWeight = 10;

	//Decision data
	Move[] repeatMoveArray = new Move[8];
	int repeatMoveIndex = 0;

	//State cache
	Board _board;
	bool isWhiteTeam;
	Random fallbackRand = new(150);

	public Move Think(Board board, Timer timer)
	{
		_board = board;
		Move[] moves = GetLegalMoves();

		if(timer.MillisecondsRemaining < 500)
		{
			//Panic time!
			//We can NOT run out of time, so use a faster method of picking
			//based on random chance - it's at least something
			Console.WriteLine("PANIC TIME");
			return moves[fallbackRand.Next(moves.Length)];
		}

		isWhiteTeam = _board.IsWhiteToMove;
		int enemyPiecesCount = 0;

		foreach (PieceType thisType in Enum.GetValues(typeof(PieceType)))
		{
			if (thisType == PieceType.None)
			{
				continue;
			}

			enemyPiecesCount += _board.GetPieceList(thisType, !isWhiteTeam).Count;
		}

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

			//Check if this move was made previously and decrease score
			//This is to ensure that the game doesn't draw as easily
			foreach(Move repeatMove in repeatMoveArray)
			{
				if(repeatMove.IsNull)
				{
					continue;
				}
				
				if(repeatMove.Equals(thisMove))
				{
					thisMoveValue += repeatMoveHeuristic;
					break;
				}
				
			}
			//Check if this new move now threatens
			//the opponent's king, if so this is good
			//for trapping it in the endgame
			if(_board.IsInCheck())
			{
				thisMoveValue += capKingHeuristic;
			}
			var targetSquare = thisMove.TargetSquare;
			//For pawns and knights, center positions are better
			if(thisMove.MovePieceType == PieceType.Pawn || thisMove.MovePieceType == PieceType.Knight)
			{
				if (targetSquare.Rank >= 2 && targetSquare.Rank <= 5
				&& targetSquare.File >= 2 && targetSquare.File <= 5)
				{
					thisMoveValue += centerSquareWeight;

					if (targetSquare.Rank >= 3 && targetSquare.Rank <= 4
					&& targetSquare.File >= 3 && targetSquare.File <= 4)
					{
						thisMoveValue += centerSquareWeight;
					}
				}
			}

			//Now undo the move to return the board state
			_board.UndoMove(thisMove);

			if(thisMoveValue > highestValue)
			{
				highestValue = thisMoveValue;
				highestMoveIndex = i;
			}
			if(timer.MillisecondsRemaining < 500)
			{
				//Panic time!
				Console.WriteLine("PANIC TIME 2: THE PANICKING");
				break;
			}
		}

		Console.WriteLine("Highest value: " + highestValue);
		var highestMove = moves[highestMoveIndex];

		//Add this chosen move to the repeat move array
		repeatMoveArray[repeatMoveIndex] = highestMove;
		repeatMoveIndex++;
		if (repeatMoveIndex >= repeatMoveArray.Length)
		{
			repeatMoveIndex = 0;
		}
		return highestMove;
	}

	//MAIN LOGIC

	/// <summary>
	/// Run Minimax on the current move
	/// </summary>
	/// <param name="depth">How many times to continue the search</param>
	/// <param name="thisTeamTurn">Whether we're making the move or not</param>
	/// <returns></returns>
	int MiniMaxAbsolute(int depth, bool thisTeamTurn, int teamMul, int alpha = int.MaxValue, int beta = -int.MaxValue)
	{

		//Since there might not be legal moves by this point,
		//We need to escape if the game has ended
		if (_board.IsInCheckmate() || _board.IsDraw())
		{
			//If whiteToMove, we actually receive -1 in teamMul because
			//White's move was made already
			//So return -40000 * teamMul which is good for black
			//because it becomes 40000 on white's turn
			return -40000 * teamMul;
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

				finalValue += (_board.GetPieceList(thisType, true).Count * 
					selfPieceWeights[(int)thisType]);
				finalValue -= (_board.GetPieceList(thisType, false).Count * 
					(selfPieceWeights[(int)thisType]));
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

		

		//Get the new moves for the next turn
		var nextMoves = GetLegalMoves();
		//Since we're now looking at the NEXT moves and not the one that was just made,
		//thisTeamTurn actually means we should minimize, not maximize
		int bestValue = int.MaxValue;
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

			//Make the next move for next iteration/eval
			_board.MakeMove(nextMove);

			//Higher value = we're winning
			//Lower value = enemy is winning
			int thisVal = -MiniMaxAbsolute(depth - 1, !thisTeamTurn, -teamMul, -beta, -alpha);

			//Return the board state
			_board.UndoMove(nextMove);

			bestValue = Math.Min(thisVal, bestValue);
			alpha = Math.Min(thisVal, alpha);
			if(alpha <= beta)
			{
				//Console.WriteLine("asdasd" + alpha);
				break;
			}
			/*
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
			*/
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