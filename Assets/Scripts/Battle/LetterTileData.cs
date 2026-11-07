using UnityEngine;

namespace Orasyon.Battle
{
    /// <summary>
    /// Shared data container for a single letter tile on the battle grid.
    /// </summary>
    public class LetterTileData
    {
        public char   Letter;
        public int    Row;
        public int    Col;
        public bool   IsSelected;
        public bool   IsHighlighted;   // used by tutorial to mark pre-chosen letters
        public bool   IsEmpty;         // the tile was used in a word and removed (leaves a gap)
        public LetterTile View;         // the MonoBehaviour attached to the UI button
    }
}
