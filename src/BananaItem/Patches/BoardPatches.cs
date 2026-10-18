using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace BananaItem.Patches
{
    // マスに止まったときのイベント（全員の端末で ActionDoNodeEvent から呼ばれる）
    [HarmonyPatch(typeof(BoardNode), nameof(BoardNode.PerformOnLandEvent))]
    internal static class BoardNodeLandPatch
    {
        // 元のコルーチンを包んで、先にバナナの判定と演出を入れる
        private static void Postfix(BoardNode __instance, BoardPlayer player, ref IEnumerator __result)
        {
            if (BananaField.ShouldTrigger(__instance, player, out GamePlayer owner))
            {
                __result = SlipThenContinue(__instance, player, owner, __result);
            }
        }

        private static IEnumerator SlipThenContinue(BoardNode node, BoardPlayer player, GamePlayer owner, IEnumerator original)
        {
            // 先に消しておく（二重に当たらないように）
            BananaField.Remove(node.NodeID);
            Plugin.Log.LogInfo($"{player.GamePlayer.Name} がバナナを踏みました（持ち主 {owner.Name}）");
            yield return new WaitForSeconds(0.3f);

            // ダメージマスと同じ形で、全員の端末でダメージを与える（転ぶ演出つき）
            var damage = new DamageInstance
            {
                damage = BananaRules.Damage,
                origin = node.transform.position,
                blood = true,
                ragdoll = true,
                ragdollVel = 10f,
                bloodVel = 3f,
                bloodAmount = 1f,
                details = "Banana",
                killer = owner.BoardObject,
                removeKeys = false,
                sound = true,
                volume = 0.5f,
            };
            player.ApplyDamage(damage);
            yield return new WaitForSeconds(2f);

            // 元のマスのイベント（コインがもらえる等）をそのまま続ける
            while (original.MoveNext())
            {
                yield return original.Current;
            }
        }
    }

    // ターン開始のたびに、時間切れのバナナを消す（ラウンドが進むのは全員同じタイミング）
    [HarmonyPatch(typeof(BoardPlayer), nameof(BoardPlayer.StartTurn))]
    internal static class BoardPlayerStartTurnPatch
    {
        private static void Postfix()
        {
            if (GameManager.Board != null)
            {
                BananaField.ExpireOld(GameManager.Board.CurTurnNum);
            }
        }
    }
}
