using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Match3
{
	public class MatchPiece : MonoBehaviour
	{
		public int row, column;
		public MatchPieceType type;
		public bool inUse;

		public bool horizontalMatch;
		public bool verticalMatch;
		public bool diagonalMatchRight;
		public bool diagonalMatchLeft;

		private SpriteRenderer mBallColorRender;

		private bool mIsDragging;

		public void Awake()
		{
			mBallColorRender = GetComponent<SpriteRenderer>();

			Explode(0);
		}

		public void SetupPiece(int row, int column, MatchPieceType type, float time)
		{
			this.row = row;
			this.column = column;
			this.type = type;

			inUse = true;

			horizontalMatch = false;
			verticalMatch = false;
			diagonalMatchRight = false;
			diagonalMatchLeft = false;

			mBallColorRender.sprite = type.sprite;

			DOTween.Sequence()
				   .Append(transform.DOScale(1f, time))
				   .Join(mBallColorRender.DOFade(1f, time));
		}

		public void ChangeSortingLayer(string layer)
		{
			mBallColorRender.sortingLayerName = layer;
		}

		public void Explode(float time)
		{
			inUse = false;
			DOTween.Sequence()
				   .Append(transform.DOScale(0.2f, time))
				   .Join(mBallColorRender.DOFade(0f, time));
		}

		public bool HasMatch(MatchesType type)
		{
			switch (type)
			{
				case MatchesType.DIAGONAL_LEFT:
					return diagonalMatchLeft;
				case MatchesType.DIAGONAL_RIGHT:
					return diagonalMatchRight;
				case MatchesType.HORIZONTAL:
					return horizontalMatch;
				case MatchesType.VERTICAL:
					return verticalMatch;
			}
			return false;
		}

		public void SetMatch(MatchesType type, bool value = true)
		{
			switch (type)
			{
				case MatchesType.DIAGONAL_LEFT:
					diagonalMatchLeft = value;
					break;
				case MatchesType.DIAGONAL_RIGHT:
					diagonalMatchRight = value;
					break;
				case MatchesType.HORIZONTAL:
					horizontalMatch = value;
					break;
				case MatchesType.VERTICAL:
					verticalMatch = value;
					break;
			}
		}

		private void Update()
		{
			if (MatchManager.instance.dragMode)
			{
				if (mIsDragging)
				{
					CheckDrag(Input.mousePosition);
				}
			}
		}

		private void OnMouseDown()
		{
			mIsDragging = true;
		}

		private void OnMouseUp()
		{
			if (!MatchManager.instance.dragMode)
			{
				CheckDrag(Input.mousePosition);
			}
			else
			{
				if (MatchManager.instance.canMove)
					StartCoroutine(MatchManager.instance.CheckForMatches());
				else
					MatchManager.instance.needCheckMatches = true;
			}

			mIsDragging = false;
		}

		private void CheckDrag(Vector2 position)
		{
			if (!mIsDragging || !MatchManager.instance.canMove) return;

			position = Camera.main.ScreenToWorldPoint(position);

			var x = transform.position.x;
			var y = transform.position.y;

			var bSize = MatchManager.instance.dragThreshold;

			var diffX = Mathf.Abs(x - position.x);
			var diffY = Mathf.Abs(y - position.y);

			var right = position.x > x + bSize;
			var left = position.x < x - bSize;
			var up = position.y > y + bSize;
			var down = position.y < y - bSize;

			if (up && (right || left))
			{
				up = diffY >= diffX;
				if (up) right = left = false;
			}
			if (down && (right || left))
			{
				down = diffY >= diffX;
				if (down) right = left = false;
			}

			SwapDirection dir = SwapDirection.NULL;
			if (up) dir = SwapDirection.UP;
			if (down) dir = SwapDirection.DOWN;
			if (left) dir = SwapDirection.LEFT;
			if (right) dir = SwapDirection.RIGHT;

			if (dir != SwapDirection.NULL)
				MatchManager.instance.SwapPieces(this, dir);
		}

		private void SetAlpha(float alpha)
		{
			SpriteRenderer[] children = GetComponentsInChildren<SpriteRenderer>();
			Color newColor;
			foreach (SpriteRenderer child in children)
			{
				newColor = child.color;
				newColor.a = alpha;
				child.color = newColor;
			}
		}
	}
}
