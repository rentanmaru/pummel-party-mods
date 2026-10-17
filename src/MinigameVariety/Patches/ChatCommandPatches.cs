using System;
using System.Collections.Generic;
using HarmonyLib;

namespace MinigameVariety.Patches
{
    // チャットで「/mv ...」と打ったときに、送信せず自分の画面にだけ結果を表示するパッチ
    // ※バニラのチャットはオンラインのロビーでしか開けない（ローカルプレイでは F8 の一覧表示を使う）
    [HarmonyPatch(typeof(ChatBox))]
    internal static class ChatCommandPatches
    {
        private const string Command = "/mv";
        private const int PageSize = 10;

        // Enterで送信される直前。false を返すと元の処理（送信）をしない
        [HarmonyPrefix]
        [HarmonyPatch(nameof(ChatBox.OnInputFieldSubmit))]
        private static bool OnInputFieldSubmitPrefix(ChatBox __instance, string inputFieldText)
        {
            List<string> reply;
            try
            {
                if (!TryHandle(inputFieldText, out reply))
                {
                    return true; // 普通のチャットはそのまま送る
                }
            }
            catch (Exception e)
            {
                // コマンドの処理で失敗しても、チャット自体は壊さない
                Plugin.Log.LogError($"チャットコマンドの処理に失敗しました: {e}");
                reply = new List<string> { PriorityReport.Prefix + "エラーが起きました（BepInExのログを見てください）" };
            }

            // 打ったコマンドを灰色で残してから結果を出す（自分の画面だけ）
            __instance.AddMessage($"<color=#888888><noparse>> {inputFieldText}</noparse></color>");
            foreach (string line in reply)
            {
                __instance.AddMessage(line);
            }
            __instance.CloseChatBox();
            return false;
        }

        // 「/mv」で始まる入力ならコマンドとして処理する
        private static bool TryHandle(string text, out List<string> reply)
        {
            reply = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] args = text.Trim().Split(new[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
            if (!string.Equals(args[0], Command, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string sub = args.Length >= 2 ? args[1].ToLowerInvariant() : "help";
            int page = args.Length >= 3 && int.TryParse(args[2], out int p) ? p : 1;

            switch (sub)
            {
                case "rank":
                    reply = PriorityReport.Page(PriorityReport.RankHeader(), PriorityReport.RankEntries(), page, PageSize, Command + " rank");
                    break;
                case "pool":
                    reply = PriorityReport.Page(PriorityReport.PoolHeader(), PriorityReport.PoolEntries(), page, PageSize, Command + " pool");
                    break;
                default:
                    reply = PriorityReport.Help(Plugin.OverlayKey.Value.ToString());
                    break;
            }
            return true;
        }
    }
}
