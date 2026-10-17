using System.Collections.Generic;
using System.Linq;

namespace MinigameVariety
{
    // 優先順位の計算（候補選び・チャットコマンド・一覧表示で共通）
    internal static class MinigamePriority
    {
        // 今のルールセットで有効なミニゲーム
        public static List<MinigameDefinition> GetActiveMinigames()
        {
            return GameManager.MinigameList
                .Where(m => m != null && m.GetIsActive())
                .ToList();
        }

        // 1ゲームで使う候補の数（ボードならターン数、ミニゲームだけのモードならミニゲーム数）
        public static int GetPoolSize()
        {
            return GameManager.partyGameMode == PartyGameMode.BoardGame
                ? GameManager.TurnCount
                : GameManager.MinigameModeCount;
        }

        // 人数が0（ロビーに入る前）のときは人数で絞らない
        public static bool IsPlayable(MinigameDefinition minigame, int playerCount)
        {
            return playerCount <= 0 || (playerCount >= minigame.minPlayers && playerCount <= minigame.maxPlayers);
        }

        // 優先度の高い順に並べる。①今の人数で遊べる → ②最後に遊んだのが古い（未プレイが最優先）
        // OrderBy は安定ソートなので、同じ優先度のものは渡された順のまま残る
        public static List<MinigameDefinition> Sort(IEnumerable<MinigameDefinition> minigames, int playerCount)
        {
            return minigames
                .OrderBy(m => IsPlayable(m, playerCount) ? 0 : 1)
                .ThenBy(m => Plugin.History.GetLastPlayed(m.MinigameID))
                .ToList();
        }
    }
}
