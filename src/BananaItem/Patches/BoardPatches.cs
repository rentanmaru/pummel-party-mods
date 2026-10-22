using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ZP.Net;

namespace BananaItem.Patches
{
    // バナナのダメージ（通過・止まった共通）
    internal static class BananaHits
    {
        public static DamageInstance CreateDamage(Vector3 origin, BananaHit hit, bool ragdoll)
        {
            return new DamageInstance
            {
                damage = hit.Damage,
                origin = origin,
                blood = true,
                ragdoll = ragdoll,
                ragdollVel = 10f,
                bloodVel = 3f,
                bloodAmount = 1f,
                details = "Banana",
                killer = hit.Owner?.BoardObject,
                removeKeys = false,
                sound = true,
                volume = 0.5f,
            };
        }

        // 移動中にバナナのマスを通過した（全員の端末で同じマスについて呼ばれる）
        public static void Pass(BoardNode node, BoardPlayer player)
        {
            if (player.PlayerState == BoardPlayerState.Dying || player.PlayerState == BoardPlayerState.Dead)
            {
                return;
            }
            // すでに倒れるダメージを持ち越している（止まったら倒れる）なら、この先のバナナは消さずに残す
            if (BananaField.HasDeferredHits(player))
            {
                return;
            }
            if (!BananaField.TryConsume(node, player, out BananaHit hit))
            {
                return;
            }

            if (player.LocalHealth > hit.Damage)
            {
                // 移動は止めずにダメージだけ与える（転ぶと移動が止まってしまうので転ばない）
                player.ApplyDamage(CreateDamage(node.NodePosition, hit, ragdoll: false));
                Plugin.Log.LogInfo($"{player.GamePlayer.Name} がバナナのマスを通過しました（{hit.Damage}ダメージ、持ち主 {hit.Owner.Name}）");
            }
            else
            {
                // 倒れるダメージは移動中に与えると進行が止まるおそれがあるので、止まったときに与える
                BananaField.AddDeferredHit(player, hit);
                Plugin.Log.LogInfo($"{player.GamePlayer.Name} がバナナのマスを通過しました（{hit.Damage}ダメージで倒れるので、止まったときに与えます）");
            }
        }

        // 持ち越したダメージを与える
        public static IEnumerator ApplyDeferred(BoardPlayer player, Vector3 origin)
        {
            foreach (BananaHit hit in BananaField.TakeDeferredHits(player))
            {
                player.ApplyDamage(CreateDamage(origin, hit, ragdoll: true));
                yield return new WaitForSeconds(0.4f);
            }
        }
    }

    // 移動中のプレイヤーについて、通過するバナナのマスを順番に処理する
    // 近づいたときに発動し、移動が終わった時点で残っていれば全部発動する（全員の端末で結果がそろう）
    internal class BananaPassTracker : MonoBehaviour
    {
        private const float TriggerDistanceSqr = 1.0f;

        private BoardPlayer _player;
        private readonly List<BoardNode> _pending = new List<BoardNode>();

        public static void Enqueue(BoardPlayer player, List<BoardNode> nodes)
        {
            if (nodes.Count == 0)
            {
                return;
            }
            BananaPassTracker tracker = player.GetComponent<BananaPassTracker>();
            if (tracker == null)
            {
                tracker = player.gameObject.AddComponent<BananaPassTracker>();
            }
            tracker._player = player;
            tracker._pending.AddRange(nodes);
        }

        private void Update()
        {
            while (_pending.Count > 0 && _player != null)
            {
                BoardNode next = _pending[0];
                if (next != null && IsMoving(_player))
                {
                    Vector3 diff = _player.transform.position - next.NodePosition;
                    diff.y = 0f;
                    if (diff.sqrMagnitude > TriggerDistanceSqr)
                    {
                        break; // まだ届いていない
                    }
                }
                _pending.RemoveAt(0);
                if (next != null)
                {
                    BananaHits.Pass(next, _player);
                }
            }
        }

        private static bool IsMoving(BoardPlayer player)
        {
            BoardPlayerState state = player.PlayerState;
            return state == BoardPlayerState.Moving || state == BoardPlayerState.MoveJump || state == BoardPlayerState.MoveArc;
        }
    }

    [HarmonyPatch(typeof(BoardPlayer))]
    internal static class BoardPlayerMovePatches
    {
        // 歩いて進むとき：StartMove で通るマスの位置がスプラインに入るので、そこからバナナのマスを探す
        [HarmonyPostfix]
        [HarmonyPatch("StartMove")]
        private static void StartMovePostfix(BoardPlayer __instance)
        {
            List<Vector3> points = __instance.moveSpline.points;
            // 歩数を使い切ったなら最後の点は止まるマス（止まったときの処理に任せる）。残っていれば最後も通過
            int end = __instance.MoveStepsRemaining > 0 ? points.Count : points.Count - 1;
            BananaPassTracker.Enqueue(__instance, BananaField.FindBananaNodesOnPath(points, end));
        }

        // ジャンプ・ワープで次のマスへ移るとき：歩数が残っていれば通過
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BoardPlayer.MoveJump))]
        private static void MoveJumpPostfix(BoardPlayer __instance, BoardNode node)
        {
            EnqueueIfPassing(__instance, node);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(BoardPlayer.MoveTeleport))]
        private static void MoveTeleportPostfix(BoardPlayer __instance, BoardNode node)
        {
            EnqueueIfPassing(__instance, node);
        }

        private static void EnqueueIfPassing(BoardPlayer player, BoardNode node)
        {
            if (player.MoveStepsRemaining > 0 && BananaField.HasBanana(node))
            {
                BananaPassTracker.Enqueue(player, new List<BoardNode> { node });
            }
        }

        // ターンの終わり：止まったときの処理が走らなかった場合に備えて、持ち越しダメージをここで与える
        [HarmonyPostfix]
        [HarmonyPatch(nameof(BoardPlayer.EndTurn))]
        private static void EndTurnPostfix(BoardPlayer __instance)
        {
            if (BananaField.HasDeferredHits(__instance))
            {
                __instance.StartCoroutine(BananaHits.ApplyDeferred(__instance, __instance.transform.position));
            }
        }
    }

    // マスに止まったときのイベント（全員の端末で ActionDoNodeEvent から呼ばれる）
    [HarmonyPatch(typeof(BoardNode), nameof(BoardNode.PerformOnLandEvent))]
    internal static class BoardNodeLandPatch
    {
        private const float MaxRagdollWaitSeconds = 6f;

        // 元のコルーチンを包んで、先にバナナの判定と演出を入れる
        private static void Postfix(BoardNode __instance, BoardPlayer player, ref IEnumerator __result)
        {
            // 倒れるダメージを持ち越していれば、止まったマスのバナナは残す（先に倒れるため）
            bool hasDeferred = BananaField.HasDeferredHits(player);
            BananaHit hit = default;
            bool landedOnBanana = !hasDeferred && BananaField.TryConsume(__instance, player, out hit);
            if (landedOnBanana || hasDeferred)
            {
                // 止まった時点でゲームが「ターン終了」を予約したかを、ここで（状態が変わる前に）調べておく
                bool endTurnQueued = BoardQueuesEndTurnOnLand(__instance);
                __result = SlipThenContinue(__instance, player, landedOnBanana, hit, endTurnQueued, __result);
            }
        }

        private static IEnumerator SlipThenContinue(BoardNode node, BoardPlayer player, bool landedOnBanana, BananaHit hit, bool endTurnQueued, IEnumerator original)
        {
            yield return new WaitForSeconds(0.3f);

            // 通過で持ち越したダメージ（倒れる分）を先に与える
            yield return BananaHits.ApplyDeferred(player, node.transform.position);

            if (landedOnBanana && !IsDown(player))
            {
                Plugin.Log.LogInfo($"{player.GamePlayer.Name} がバナナを踏みました（{hit.Damage}ダメージ、持ち主 {hit.Owner.Name}）");
                // ダメージマスと同じ形で、全員の端末でダメージを与える（転ぶ演出つき）
                player.ApplyDamage(BananaHits.CreateDamage(node.transform.position, hit, ragdoll: true));
            }
            yield return new WaitForSeconds(2f);

            if (IsDown(player))
            {
                // 倒れた人にはマスのイベント（死神の選択など）を始めない。始めると選べずに止まってしまう
                Plugin.Log.LogInfo($"{player.GamePlayer.Name} がバナナで倒れたので、マスのイベントは行いません");
                node.onLandEventFinished = true;
                if (NetSystem.IsServer && !endTurnQueued)
                {
                    // このマスはイベントの中でターンを終える作りなので、代わりにターンを終える
                    GameManager.Board.QueueAction(new ActionSimple(SimpleBoardAction.EndTurn));
                }
                yield break;
            }

            // 転んだ状態からまだ起き上がっていなければ待つ（起き上がる前にイベントが始まるとぶつかる）
            float waited = 0f;
            while (player.PlayerState == BoardPlayerState.Ragdolling && waited < MaxRagdollWaitSeconds)
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // 元のマスのイベント（コインがもらえる等）をそのまま続ける
            while (original.MoveNext())
            {
                yield return original.Current;
            }
        }

        private static bool IsDown(BoardPlayer player)
        {
            BoardPlayerState state = player.PlayerState;
            return state == BoardPlayerState.Dying || state == BoardPlayerState.Dead || player.LocalHealth <= 0;
        }

        // GameBoardController の ActionDoNodeEvent（4192〜4206行目）と同じ判定：止まったときにターン終了を予約したか
        private static bool BoardQueuesEndTurnOnLand(BoardNode node)
        {
            if (node.nodeEventType == BoardNodeEventType.NodeType && node.CurrentNodeType == BoardNodeType.Recruit)
            {
                return node.nodeEvent != null && node.nodeEvent.EndTurnAfterEvent(node);
            }
            if (!node.EventTypeIsSimple() && node.nodeEventType == BoardNodeEventType.Custom && node.modBoardSpace != null
                && node.modBoardSpace.HasBoardSpaceEvent() && node.EnabledState)
            {
                return false;
            }
            return node.nodeEventType != BoardNodeEventType.Custom || node.nodeEvent == null || node.nodeEvent.EndTurnAfterEvent(node);
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
