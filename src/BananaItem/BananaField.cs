using System.Collections.Generic;
using UnityEngine;

namespace BananaItem
{
    // ボードに置かれたバナナの管理。全員の端末で同じ順番に同じ処理が走るので、全員で同じ状態になる
    internal static class BananaField
    {
        private class PlacedBanana
        {
            public short OwnerId;
            public int PlacedRound;
            public GameObject Visual;
        }

        // マスID → バナナ（1マスに1本まで）
        private static readonly Dictionary<int, PlacedBanana> Bananas = new Dictionary<int, PlacedBanana>();

        // どのボードのバナナか（次のゲームになったら前のバナナを捨てる）
        private static GameBoardController _board;

        public static void Place(BoardNode node, GamePlayer owner, bool quiet = false)
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

        // 止まった人がバナナを踏むか。踏むなら持ち主を返す
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
            _board = GameManager.Board;
        }
    }
}
