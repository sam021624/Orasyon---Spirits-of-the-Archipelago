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
        public bool   IsEmpty;         // the tile was used in a word and removed (leaves a gap), or is a Void hole
        public bool   IsStolen;        // the slot is empty because the Duwende pocketed the tile; it comes back later
        public bool   Fresh;           // a bad tile placed during this enemy turn: it does not tick or burn until the next one
        public TileKind Kind;          // Normal, a good special tile, or a bad tile placed by an enemy
        public int    TurnsLeft;       // enemy turns until a Smoked / Locked / Hex tile wears off (0 = no timer)
        public LetterTile View;         // the MonoBehaviour attached to the UI button

        public bool IsSelectable => !IsEmpty && Kind != TileKind.Locked;
    }
}
