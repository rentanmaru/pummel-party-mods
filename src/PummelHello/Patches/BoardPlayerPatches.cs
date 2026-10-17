using HarmonyLib;

namespace PummelHello.Patches
{
    // ボード上のプレイヤー（BoardPlayer）へのパッチ。挙動は変えずログを出すだけ
    [HarmonyPatch(typeof(BoardPlayer))]
    internal static class BoardPlayerPatches
    {
        // StartTurn() の実行後に呼ばれる
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BoardPlayer.StartTurn))]
        private static void StartTurnPostfix(BoardPlayer __instance)
        {
            Plugin.Log.LogInfo($"ターン開始: {GetPlayerName(__instance)}");
        }

        // EndTurn() の実行後に呼ばれる
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BoardPlayer.EndTurn))]
        private static void EndTurnPostfix(BoardPlayer __instance)
        {
            Plugin.Log.LogInfo($"ターン終了: {GetPlayerName(__instance)}");
        }

        // プレイヤー名を取り出す。player は private だが publicizer で読める
        private static string GetPlayerName(BoardPlayer boardPlayer)
        {
            GamePlayer player = boardPlayer.player;
            return player != null ? player.Name : boardPlayer.name;
        }
    }
}
