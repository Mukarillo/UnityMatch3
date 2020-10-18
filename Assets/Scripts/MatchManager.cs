using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Match3
{   
	public class MatchManager : MonoBehaviour
	{
		public const float TIME_TO_EXPLODE = 0.3f;
		public const float TIME_TO_SWAP_DRAG_MODE = 0.2f;
		public const float TIME_TO_SWAP = 0.3f;

		public float timeToSwap
		{
			get
			{
				return dragMode ? TIME_TO_SWAP_DRAG_MODE : TIME_TO_SWAP;
			}
		}

		public static MatchManager instance;

		public bool dragMode;      
		public bool swapBack;
		public bool diagonalMatches;
		public float dragThreshold = 1.2f;

		public int rows, columns;
		[SerializeField]
		public List<MatchPieceType> pieceTypes = new List<MatchPieceType>();

		public bool canMove { get; set; }
		public bool needCheckMatches { get; set; }
		public bool gameIsOver { get; set; }

		private MatchPiece[][] board;
	    private GameObject matchPieceObject;
        private MatchPiece currentPiece;
		private SwapDirection currentDirection;

		public void Start()
		{
			instance = this;

		    matchPieceObject = (GameObject) Instantiate(Resources.Load("Prefabs/SampleObject"));
            Vector2 offset = matchPieceObject.GetComponent<SpriteRenderer>().bounds.size;
			CreateBoard(offset.x, offset.y);

			canMove = true;
			gameIsOver = false;
		}

		private void CreateBoard(float xOffset, float yOffset)
		{
			float startX = transform.position.x;
			float startY = transform.position.y;

			MatchPieceType[] previousLeft = new MatchPieceType[columns];
			MatchPieceType previousBelow = null;

			board = new MatchPiece[rows][];
			for (int x = 0; x < rows; x++)
			{
				board[x] = new MatchPiece[columns];
				for (int y = 0; y < columns; y++)
				{
					var tile = Instantiate(
						matchPieceObject,
						new Vector3(startX + (xOffset * x),
									startY + (yOffset * y),
									2),
						matchPieceObject.transform.rotation).AddComponent<MatchPiece>();

					List<MatchPieceType> possibletypes = new List<MatchPieceType>();
					possibletypes.AddRange(pieceTypes);

					possibletypes.Remove(previousLeft[y]);
					possibletypes.Remove(previousBelow);

					MatchPieceType type = possibletypes[Random.Range(0, possibletypes.Count)];

					tile.SetupPiece(y, x, type, TIME_TO_EXPLODE);

					previousLeft[y] = type;
					previousBelow = type;

					board[x][y] = tile;
				}
			}
		}

		public void SwapPieces(MatchPiece piece, SwapDirection direction, bool checkMatches = true)
		{
			currentPiece = piece;
			currentDirection = direction;

			bool validMove = CheckForValidMove(piece, direction);

			if (!validMove)
			{
				if (!dragMode)
					piece.gameObject.transform.DOShakePosition(0.5f, 0.3f);
				return;
			}

			canMove = false;

			MatchPiece otherPiece = GetPieceByDirection(piece, direction);

			var piecePosition = piece.transform.position;
			var otherPiecePosition = otherPiece.transform.position;

			piece.ChangeSortingLayer("ballFront");
			otherPiece.ChangeSortingLayer("ballBack");

			DOTween.Sequence()
				   .Append(piece.transform.DOMove(otherPiecePosition, timeToSwap))
				   .Join(otherPiece.transform.DOMove(piecePosition, timeToSwap))
				   .SetEase(Ease.OutCirc)
				   .OnComplete(() =>
				   {
					   SwapPieces(piece, otherPiece, checkMatches);
				   });
		}

		private void SwapPieces(MatchPiece piece, MatchPiece otherPiece, bool checkMatches = true)
		{
			var pieceRow = piece.row;
			var pieceColumn = piece.column;

			piece.row = otherPiece.row;
			piece.column = otherPiece.column;

			otherPiece.row = pieceRow;
			otherPiece.column = pieceColumn;

			board[piece.column][piece.row] = piece;
			board[otherPiece.column][otherPiece.row] = otherPiece;

			canMove = true;

			if (needCheckMatches || !dragMode && checkMatches)
			{
				needCheckMatches = false;
				StartCoroutine(CheckForMatches());
			}
		}

		public IEnumerator CheckForMatches(bool needSwapBack = true)
		{
			bool hasMatches = false;

			if (gameIsOver) yield break;

			for (int x = 0; x < rows; x++)
			{
				for (int y = 0; y < columns; y++)
				{
					var horizontal = CheckMatches(x, y, MatchesType.HORIZONTAL);
					var vertical = CheckMatches(x, y, MatchesType.VERTICAL);
					var dright = diagonalMatches && CheckMatches(x, y, MatchesType.DIAGONAL_RIGHT);
					var dleft = diagonalMatches && CheckMatches(x, y, MatchesType.DIAGONAL_LEFT);

					if (!hasMatches)
						hasMatches = horizontal || vertical || dright || dleft;
				}
			}

			if (!hasMatches && swapBack && !dragMode && needSwapBack)
			{
				SwapPieces(currentPiece, OppositeDirection(currentDirection), false);
				canMove = true;
			}

			yield return new WaitForSeconds(TIME_TO_EXPLODE);

			if (hasMatches)
				StartCoroutine(ShiftDownPieces());
		}

		private bool CheckMatches(int x, int y, MatchesType type)
		{
			var addX = (type == MatchesType.DIAGONAL_LEFT ? -1 : type == MatchesType.VERTICAL ? 0 : 1);
			var addY = (type == MatchesType.HORIZONTAL ? 0 : 1);
			var pX = x + addX;
			var pY = y + addY;
			var hasMatches = false;

			List<MatchPiece> pieceList = new List<MatchPiece>(){
					board[x][y]
				};
			var currentType = board[x][y].type;

			while (InBounds(pX, pY)
						   && currentType.CompareTo(board[pX][pY].type) == 0
						   && !board[pX][pY].HasMatch(type))
			{
				pieceList.Add(board[pX][pY]);
				pX = pX + addX;
				pY = pY + addY;
			}

			if (pieceList.Count > 2)
			{
				pieceList.ForEach(p => p.SetMatch(type));
				ExplodePieces(pieceList);
				hasMatches = true;
			}

			return hasMatches;
		}

		private IEnumerator ShiftDownPieces()
		{
			float offset = matchPieceObject.GetComponent<SpriteRenderer>().bounds.size.y;
			for (int x = 0; x < rows; x++)
			{
				int shifts = 0;
				for (int y = 0; y < columns; y++)
				{
					if (!board[x][y].inUse)
					{
						shifts++;
						continue;
					}

					if (shifts == 0) continue;

					board[x][y].transform.DOMoveY(board[x][y].transform.position.y - (offset * shifts), TIME_TO_EXPLODE)
						 .SetEase(Ease.InExpo);
					var holder = board[x][y - shifts];

					board[x][y - shifts] = board[x][y];
					board[x][y - shifts].row = y - shifts;

					board[x][y] = holder;
					board[x][y].transform.position = board[x][y - shifts].transform.position;
				}
			}

			yield return new WaitForSeconds(TIME_TO_EXPLODE);

			for (int x = 0; x < rows; x++)
			{
				for (int y = 0; y < columns; y++)
				{
					if (board[x][y].inUse) continue;
					board[x][y].SetupPiece(y, x, pieceTypes[Random.Range(0, pieceTypes.Count)], TIME_TO_EXPLODE);
				}
			}

			yield return new WaitForSeconds(TIME_TO_EXPLODE);

			StartCoroutine(CheckForMatches(false));
		}
        int score=0;
		public Text t;
		private void ExplodePieces(List<MatchPiece> pieceList)
		{
			pieceList.ForEach(x => x.Explode(TIME_TO_EXPLODE));score++;
		}
		public void Update(){
			t.text = score.ToString();
		}

		private bool InBounds(int x, int y)
		{
			return x >= 0 && x < columns && y >= 0 && y < rows;
		}

		private MatchPiece GetPieceByDirection(MatchPiece piece, SwapDirection direction)
		{
			var c = piece.column + (direction == SwapDirection.LEFT ? (-1) : direction == SwapDirection.RIGHT ? 1 : 0);
			var r = piece.row + (direction == SwapDirection.DOWN ? (-1) : direction == SwapDirection.UP ? 1 : 0);

			return board[c][r];
		}

		private bool CheckForValidMove(MatchPiece piece, SwapDirection direction)
		{
			return !(direction == SwapDirection.LEFT && piece.column == 0 ||
					 direction == SwapDirection.RIGHT && piece.column == columns - 1 ||
					 direction == SwapDirection.UP && piece.row == rows - 1 ||
					 direction == SwapDirection.DOWN && piece.row == 0);
		}

		private SwapDirection OppositeDirection(SwapDirection dir)
		{
			switch (dir)
			{
				case SwapDirection.DOWN:
					return SwapDirection.UP;
				case SwapDirection.UP:
					return SwapDirection.DOWN;
				case SwapDirection.LEFT:
					return SwapDirection.RIGHT;
				case SwapDirection.RIGHT:
					return SwapDirection.LEFT;
			}

			return SwapDirection.NULL;
		}
	}
}
