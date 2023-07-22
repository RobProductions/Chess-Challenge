using ChessChallenge.API;
using System.Collections.Generic;

public class MyBot : IChessBot
{
	public Move Think(Board board, Timer timer)
	{
		Move[] moves = board.GetLegalMoves();

		return moves[0];
	}
}