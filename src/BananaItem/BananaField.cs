using System.Collections.Generic;
using UnityEngine;

namespace BananaItem
{
    // 踏まれたバナナ1本ぶんの結果（だれのバナナで、何ダメージか）
    internal struct BananaHit
    {
        public GamePlayer Owner;
        public int Damage;
    }

    // ボードに置かれたバナナの管理。全員の端末で同じ順番に同じ処理が走るので、全員で同じ状態になる
    internal static class BananaField
    {
        private class PlacedBanana
        {
            public short OwnerId;
            public int PlacedRound;
            // 使ったときに全員へ配られた乱数シード。ダメージ量をこれで決めるので、全員で同じ値になる
            public int Seed;
            public GameObject Visual;
        }

        // マスID → バナナ（1マスに1本まで）
        private static readonly Dictionary<int, PlacedBanana> Bananas = new Dictionary<int, PlacedBanana>();

        // 移動中に倒れてしまうダメージは、止まったときまで持ち越す（プレイヤーID → 持ち越したダメージ）
        private static readonly Dictionary<short, List<BananaHit>> DeferredHits = new Dictionary<short, List<BananaHit>>();

        // どのボードのバナナか（次のゲームになったら前のバナナを捨てる）
        private static GameBoardController _board;

        public static void Place(BoardNode node, GamePlayer owner, int seed, bool quiet = false)
        {
            SyncBoard();
            if (node == null || owner == null || GameManager.Board == null)
            {
                return;
            }

            // 同じマスにすでにあれば置き直す（持ち主と置いたラウンドを更新）
            Remove(node.NodeID);
            Bananas[node.NodeID] = new PlacedBanana
            {
                OwnerId = owner.GlobalID,
                PlacedRound = GameManager.Board.CurTurnNum,
                Seed = seed,
                Visual = BananaVisual.CreatePlaced(node.transform.position),
            };
            if (!quiet)
            {
                Plugin.Log.LogInfo($"バナナを置きました: マス {node.NodeID}、持ち主 {owner.Name}、ラウンド {GameManager.Board.CurTurnNum}");
            }
        }

        public static bool HasBanana(BoardNode node)
        {
            SyncBoard();
            return node != null && Bananas.ContainsKey(node.NodeID);
        }

        // そのマスのバナナを、その人が踏むか。踏むなら持ち主を返す
        public static bool ShouldTrigger(BoardNode node, BoardPlayer lander, out GamePlayer owner)
        {
            owner = null;
            SyncBoard();
            if (node == null || lander == null || !Bananas.TryGetValue(node.NodeID, out PlacedBanana banana))
            {
                return false;
            }

            owner = GameManager.GetPlayerWithID(banana.OwnerId);
            GamePlayer landerPlayer = lander.GamePlayer;

            // 置いた本人は踏まない
            if (owner == null || landerPlayer == null || owner == landerPlayer)
            {
                return false;
            }

            // チーム戦で味方に当てない設定なら、味方は踏まない（他のアイテムと同じ判定）
            bool friendlyFireOff = owner.IsAI || !GameManager.IsBoardItemTeamFriendlyFireEnabled;
            if (!GameManager.PlayingSoloMode && friendlyFireOff && owner.GameTeam != null && owner.GameTeam.IsInTeam(landerPlayer))
            {
                return false;
            }
            return true;
        }

        // 踏むならバナナを消して、持ち主とダメージ量を返す（通過・止まった共通）
        public static bool TryConsume(BoardNode node, BoardPlayer player, out BananaHit hit)
        {
            hit = default;
            if (!ShouldTrigger(node, player, out GamePlayer owner))
            {
                return false;
            }

            hit = new BananaHit
            {
                Owner = owner,
                Damage = RollDamage(Bananas[node.NodeID].Seed, player.GamePlayer.GlobalID),
            };
            Remove(node.NodeID);
            return true;
        }

        // バナナのシードと踏んだ人から、ダメージ量（最小〜最大）を決める
        // 同じシード・同じ人なら、どの端末でも必ず同じ値になる
        private static int RollDamage(int seed, short victimId)
        {
            var random = new System.Random(unchecked(seed * 31 + victimId));
            return random.Next(BananaRules.MinDamage, BananaRules.MaxDamage + 1);
        }

        // 移動の通り道（スプラインの点＝通るマスの位置）にあるバナナのマスを、通る順に返す
        // points[0] は今いる位置なので飛ばす。endExclusive より後ろ（止まるマス）は含めない
        public static List<BoardNode> FindBananaNodesOnPath(List<Vector3> points, int endExclusive)
        {
            SyncBoard();
            var result = new List<BoardNode>();
            if (Bananas.Count == 0 || GameManager.Board == null || GameManager.Board.BoardNodes == null)
            {
                return result;
            }

            BoardNode[] nodes = GameManager.Board.BoardNodes;
            for (int i = 1; i < endExclusive && i < points.Count; i++)
            {
                foreach (int nodeId in Bananas.Keys)
                {
                    if (nodeId < 0 || nodeId >= nodes.Length || nodes[nodeId] == null)
                    {
                        continue;
                    }
                    Vector3 diff = nodes[nodeId].NodePosition - points[i];
                    if (diff.sqrMagnitude < 0.01f && !result.Contains(nodes[nodeId]))
                    {
                        result.Add(nodes[nodeId]);
                    }
                }
            }
            return result;
        }

        public static void AddDeferredHit(BoardPlayer player, BananaHit hit)
        {
            short id = player.GamePlayer.GlobalID;
            if (!DeferredHits.TryGetValue(id, out List<BananaHit> hits))
            {
                hits = new List<BananaHit>();
                DeferredHits[id] = hits;
            }
            hits.Add(hit);
        }

        public static bool HasDeferredHits(BoardPlayer player)
        {
            return player != null && player.GamePlayer != null
                && DeferredHits.TryGetValue(player.GamePlayer.GlobalID, out List<BananaHit> hits) && hits.Count > 0;
        }

        // 持ち越したダメージを取り出す（取り出したら消える）
        public static List<BananaHit> TakeDeferredHits(BoardPlayer player)
        {
            if (!HasDeferredHits(player))
            {
                return new List<BananaHit>();
            }
            List<BananaHit> hits = DeferredHits[player.GamePlayer.GlobalID];
            DeferredHits.Remove(player.GamePlayer.GlobalID);
            return hits;
        }

        public static void Remove(int nodeId)
        {
            if (Bananas.TryGetValue(nodeId, out PlacedBanana banana))
            {
                if (banana.Visual != null)
                {
                    Object.Destroy(banana.Visual);
                }
                Bananas.Remove(nodeId);
            }
        }

        // 置いてから決まったラウンド数が過ぎたバナナを消す
        public static void ExpireOld(int currentRound)
        {
            SyncBoard();
            var expired = new List<int>();
            foreach (KeyValuePair<int, PlacedBanana> pair in Bananas)
            {
                if (currentRound >= pair.Value.PlacedRound + BananaRules.DurationRounds)
                {
                    expired.Add(pair.Key);
                }
            }

            foreach (int nodeId in expired)
            {
                Remove(nodeId);
            }

            if (expired.Count > 0)
            {
                // 全マスに置くテストのときに長くならないよう、1行にまとめる
                string nodes = expired.Count <= 5 ? string.Join(", ", expired) : $"{expired.Count}か所";
                Plugin.Log.LogInfo($"バナナが時間切れで消えました: マス {nodes}（ラウンド {currentRound}）");
            }
        }

        private static void SyncBoard()
        {
            if (_board == GameManager.Board)
            {
                return;
            }

            // ボードが変わった（ゲームが終わった・次のゲーム）ので全部捨てる
            foreach (PlacedBanana banana in Bananas.Values)
            {
                if (banana.Visual != null)
                {
                    Object.Destroy(banana.Visual);
                }
            }
            Bananas.Clear();
            DeferredHits.Clear();
            _board = GameManager.Board;
        }
    }
}
