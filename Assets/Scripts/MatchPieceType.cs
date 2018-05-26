using System;
using UnityEngine;

namespace Match3
{
	[Serializable]
	public class MatchPieceType : IComparable
	{
		public string name;
		public Sprite sprite;

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;

			MatchPieceType otherPiece = obj as MatchPieceType;
			if (otherPiece != null)
				return string.Compare(this.name, otherPiece.name, StringComparison.CurrentCulture);
			else
				throw new ArgumentException("Object is not a MatcPieceType");
		}
	}
}