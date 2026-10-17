using HarmonyLib;

namespace PummelHello.Patches
{
    // ゲーム全体の管理クラス（GameManager）へのパッチ。挙動は変えずログを出すだけ
    [HarmonyPatch(typeof(GameManager))]
    internal static class GameManagerPatches
    {
        // ミニゲーム読み込みの直前に呼ばれる（どのミニゲームが選ばれたか記録）
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameManager.LoadMinigame))]
        private static void LoadMinigamePrefix(MinigameDefinition minigame, int alternateIndex)
        {
            string name = minigame != null ? minigame.minigameName : "(null)";
            // MinigameID はワークショップ製ならワークショップのID、それ以外は内部ID
            Plugin.Log.LogInfo($"ミニゲーム読み込み: {name}（ID {minigame?.MinigameID}、バリエーション {alternateIndex}）");
        }
    }
}
