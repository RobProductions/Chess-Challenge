using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
	Board _board;
	int[] moveValues = new int[256];
	bool isWhiteTeam;

	public Move Think(Board board, Timer timer)
	{
		_board = board;
        isWhiteTeam = _board.IsWhiteToMove;
        Move[] moves = GetLegalMoves();

		//Acquire all the move values through minimax
		for(int i = 0; i < moves.Length; i++)
		{
			moveValues[i] = MiniMaxAbsolute(moves[i], 1, true);
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
        return moves[highestMoveIndex];
	}

	//MAIN LOGIC

	int MiniMaxAbsolute(Move currentMove, int depth, bool thisTeamTurn)
	{
		if(depth == 0)
		{
			//Count up number of pieces we have left
			ulong bitboardToUse = _board.BlackPiecesBitboard;
			if(isWhiteTeam)
			{
				bitboardToUse = _board.WhitePiecesBitboard;
			}
			return 0;
		}

		return 0;
	}


	//HELPERS

	Move[] GetLegalMoves()
	{
		return _board.GetLegalMoves();
	}
}