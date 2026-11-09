using Orasyon.Battle;
using UnityEditor;
using UnityEngine;

namespace Orasyon.EditorTools
{
    /// <summary>
    /// Play-mode helpers for testing tiles without waiting for an enemy to place them.
    /// Tools > Orasyon > Debug > ... places a tile of that kind on a random plain tile of the current board.
    /// </summary>
    public static class TileDebugMenu
    {
        private const string Root = "Tools/Orasyon/Debug/";

        [MenuItem(Root + "Place Gold (Ginto)")]     private static void Gold()    => Place(TileKind.Gold);
        [MenuItem(Root + "Place Heal (Dahon)")]     private static void Heal()    => Place(TileKind.Heal);
        [MenuItem(Root + "Place Silver (Pilak)")]   private static void Silver()  => Place(TileKind.Silver);
        [MenuItem(Root + "Place Shield (Agimat)")]  private static void Shield()  => Place(TileKind.Shield);
        [MenuItem(Root + "Place Smoked (Usok)")]    private static void Smoked()  => Place(TileKind.Smoked, 3);
        [MenuItem(Root + "Place Locked (Gapos)")]   private static void Locked()  => Place(TileKind.Locked, 2);
        [MenuItem(Root + "Place Burning (Alab)")]   private static void Burning() => Place(TileKind.Burning);
        [MenuItem(Root + "Place Hex (Sumpa)")]      private static void Hex()     => Place(TileKind.Hex, 3);
        [MenuItem(Root + "Place Void (Wala)")]      private static void Void()    => Place(TileKind.Void);

        [MenuItem(Root + "Clear Bad Tiles")]
        private static void ClearBad()
        {
            var grid = Grid();
            if (grid != null) Debug.Log($"[Orasyon] Cleared {grid.ClearBadTiles(smokeAndLockOnly: false)} bad tiles.");
        }

        [MenuItem(Root + "Fill Orasyon Meter")]
        private static void FillMeter()
        {
            var meter = Object.FindAnyObjectByType<OrasyonMeter>();
            if (meter != null) meter.FillFully(); else Debug.LogWarning("[Orasyon] No Orasyon meter in this scene.");
        }

        // Every item needs Play mode and a battle scene
        [MenuItem(Root + "Place Gold (Ginto)", true)]
        [MenuItem(Root + "Place Heal (Dahon)", true)]
        [MenuItem(Root + "Place Silver (Pilak)", true)]
        [MenuItem(Root + "Place Shield (Agimat)", true)]
        [MenuItem(Root + "Place Smoked (Usok)", true)]
        [MenuItem(Root + "Place Locked (Gapos)", true)]
        [MenuItem(Root + "Place Burning (Alab)", true)]
        [MenuItem(Root + "Place Hex (Sumpa)", true)]
        [MenuItem(Root + "Place Void (Wala)", true)]
        [MenuItem(Root + "Clear Bad Tiles", true)]
        [MenuItem(Root + "Fill Orasyon Meter", true)]
        private static bool NeedsPlayMode() => Application.isPlaying && Object.FindAnyObjectByType<LetterGrid>() != null;

        private static LetterGrid Grid()
        {
            var grid = Object.FindAnyObjectByType<LetterGrid>();
            if (grid == null) Debug.LogWarning("[Orasyon] Enter Play mode in a battle scene first.");
            return grid;
        }

        private static void Place(TileKind kind, int turns = 0)
        {
            var grid = Grid();
            if (grid == null) return;
            var picked = grid.PickTiles(1, t => !t.IsEmpty && t.Kind == TileKind.Normal && !t.IsSelected);
            if (picked.Count == 0) { Debug.LogWarning("[Orasyon] No plain tile left to change."); return; }
            grid.SetKind(picked[0], kind, turns);
            Debug.Log($"[Orasyon] {kind} ({TileKindInfo.FolkName(kind)}) placed on tile {picked[0]}.");
        }
    }
}
