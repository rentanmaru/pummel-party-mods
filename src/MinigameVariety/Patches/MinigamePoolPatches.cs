using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ZP.Net;

namespace MinigameVariety.Patches
{
    // ミニゲームの候補選びを「最近遊んでいない順」に変えるパッチ。ホストの端末でだけ効く
    [HarmonyPatch(typeof(GameManager))]
    internal static class MinigamePoolPatches
    {
        private static readonly System.Random Random = new System.Random();

        // 候補を選んだときの人数（ロビーで人数が変わったら選び直すために覚えておく）
        internal static int LastPoolPlayerCount = -1;

        // バニラの候補選びのあとに、候補を選び直して上書きする
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameManager.SelectPossibleMinigamesThisGame))]
        private static void SelectPossibleMinigamesThisGamePostfix()
        {
            // バニラと同じく、ホスト以外では何もしない
            if (!Plugin.Enabled.Value || !NetSystem.IsServer)
            {
                return;
            }

            int playerCount = GameManager.PlayerCount;
            LastPoolPlayerCount = playerCount;

            List<MinigameDefinition> active = MinigamePriority.GetActiveMinigames();
            int target = MinigamePriority.GetPoolSize();

            // 有効なミニゲームが全部候補に入るなら、並べ替える意味がない
            if (target <= 0 || active.Count <= target)
            {
                return;
            }

            // 先にシャッフルしておくと、同じ優先度のものはランダムな順になる
            Shuffle(active);
            List<MinigameDefinition> picked = MinigamePriority.Sort(active, playerCount)
                .Take(target)
                .ToList();

            // バニラが使う3つのリストをそろえて書き換える
            GameManager.m_possibleMinigamesThisGameMasterList.Clear();
            GameManager.m_possibleMinigamesThisGameMasterList.AddRange(picked);
            GameManager.m_possibleMinigameIDs.Clear();
            GameManager.m_possibleMinigameIDs.AddRange(picked.Select(m => m.MinigameID));
            // ロビーにいる間はこのフラグを見て、参加者にもIDが送られる
            GameManager.HasPossibleMinigamesChanged = true;
            GameManager.RefreshPossibleMinigamesList();

            // ワークショップのミニゲームを読み込むたびにバニラが呼ぶので、ここは Debug レベル（通常は非表示）
            Plugin.Log.LogDebug($"候補を選び直しました: {picked.Count}/{active.Count}件（{playerCount}人）");
        }

        // ミニゲームが読み込まれたら「遊んだ」と記録する（ホストでも参加者でも記録）
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.LoadMinigame))]
        private static void LoadMinigamePrefix(MinigameDefinition minigame)
        {
            if (minigame != null)
            {
                Plugin.History.RecordPlayed(minigame.MinigameID);
            }
        }

        // リストをランダムに並べ替える（フィッシャー–イェーツ）
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // 実際に使う候補をログに出す（ゲーム開始時に1回だけ呼ぶ）
        internal static void LogPool()
        {
            List<MinigameDefinition> pool = GameManager.m_possibleMinigamesThisGameMasterList;
            int unplayed = pool.Count(m => Plugin.History.GetLastPlayed(m.MinigameID) == 0);
            Plugin.Log.LogInfo(
                $"このゲームの候補: {pool.Count}件（{GameManager.PlayerCount}人）" +
                $" 未プレイ {unplayed}件、プレイ済み {pool.Count - unplayed}件");
            Plugin.Log.LogInfo("  " + string.Join(", ", pool.Select(m => m.minigameName)));
        }
    }

    // ゲーム開始時のパッチ
    [HarmonyPatch(typeof(GameBoardController))]
    internal static class GameBoardControllerPatches
    {
        // StartGame() の直前。2回目以降の呼び出しはバニラが無視するので、ログも出さない
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameBoardController.StartGame))]
        private static void StartGamePrefix(GameBoardController __instance)
        {
            if (Plugin.Enabled.Value && Plugin.LogPool.Value && NetSystem.IsServer && !__instance.gameStarted)
            {
                MinigamePoolPatches.LogPool();
            }
        }
    }

    // ロビーの状態に合わせて候補を選び直すパッチ
    [HarmonyPatch(typeof(MultiplayerLobbyController))]
    internal static class LobbyPatches
    {
        // ロビーが開くたび（2戦目も）に選び直して、最新の記録を反映する
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MultiplayerLobbyController.OnNetInitialize))]
        private static void OnNetInitializePostfix()
        {
            if (Plugin.Enabled.Value && NetSystem.IsServer)
            {
                GameManager.SelectPossibleMinigamesThisGame();
            }
        }

        // ロビーで人が出入りしたりAIを足したりして人数が変わったら選び直す
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MultiplayerLobbyController.UpdatePlayerData))]
        private static void UpdatePlayerDataPostfix()
        {
            if (!Plugin.Enabled.Value || !NetSystem.IsServer)
            {
                return;
            }

            // ゲーム中（ボード・ミニゲーム中）は選び直さない
            if (GameManager.CurState != GameState.MainMenu)
            {
                return;
            }

            if (GameManager.PlayerCount != MinigamePoolPatches.LastPoolPlayerCount)
            {
                GameManager.SelectPossibleMinigamesThisGame();
            }
        }
    }
}
