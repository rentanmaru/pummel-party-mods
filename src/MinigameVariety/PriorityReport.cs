using System;
using System.Collections.Generic;
using System.Linq;
using ZP.Net;

namespace MinigameVariety
{
    // 優先順位・候補を表示用の文字列にする（チャットとF8の一覧表示で共通）
    // <color> と <b> はチャット（TextMeshPro）でも一覧表示（IMGUI）でも使える
    internal static class PriorityReport
    {
        public const string Prefix = "<b><color=#7fd3ff>MinigameVariety:</color></b> ";

        private const string ColorUnplayed = "#a2d66d";
        private const string ColorPlayed = "#fca040";
        private const string ColorNotPlayable = "#d17564";
        private const string ColorPool = "#ffd84a";
        private const string ColorNote = "#aaaaaa";

        // 有効なミニゲームを優先順位の順に並べた行（1行=1ミニゲーム）
        public static List<string> RankEntries()
        {
            int playerCount = GameManager.PlayerCount;
            HashSet<ulong> poolIds = GetPoolIds();

            // 同じ優先度のものは名前順にしておく（表示が毎回変わらないように）
            List<MinigameDefinition> ranked = MinigamePriority.Sort(
                MinigamePriority.GetActiveMinigames().OrderBy(m => m.minigameName, StringComparer.Ordinal),
                playerCount);

            var lines = new List<string>();
            for (int i = 0; i < ranked.Count; i++)
            {
                lines.Add(FormatEntry(i + 1, ranked[i], playerCount, poolIds));
            }
            return lines;
        }

        // 優先順位の見出し
        public static string RankHeader()
        {
            int activeCount = MinigamePriority.GetActiveMinigames().Count;
            return $"<b>ミニゲームの優先順位</b>（{GameManager.PlayerCount}人・有効 {activeCount}件・" +
                   $"<color={ColorPool}>★</color>=今回の候補）";
        }

        // 今回の候補を優先順位の順に並べた行
        public static List<string> PoolEntries()
        {
            int playerCount = GameManager.PlayerCount;
            HashSet<ulong> poolIds = GetPoolIds();
            List<MinigameDefinition> ranked = MinigamePriority.Sort(
                GetPool().OrderBy(m => m.minigameName, StringComparer.Ordinal),
                playerCount);

            var lines = new List<string>();
            for (int i = 0; i < ranked.Count; i++)
            {
                lines.Add(FormatEntry(i + 1, ranked[i], playerCount, poolIds));
            }
            return lines;
        }

        // 今回の候補の見出し
        public static string PoolHeader()
        {
            List<MinigameDefinition> pool = GetPool();
            int unplayed = pool.Count(m => Plugin.History.GetLastPlayed(m.MinigameID) == 0);
            string header = $"<b>今回の候補</b>（{pool.Count}件・{GameManager.PlayerCount}人）" +
                            $"未プレイ {unplayed}件、プレイ済み {pool.Count - unplayed}件";
            if (!NetSystem.IsServer)
            {
                // 参加者の場合、未プレイかどうかは自分の記録で判定している
                header += $"　<color={ColorNote}>※候補はホストが決めます</color>";
            }
            return header;
        }

        // 今回の候補。ホストは自分で選んだもの、参加者はホストから受け取ったIDから引く
        // GetMinigameById は見つからないと警告ログを出すので、辞書を直接引く（一覧表示で毎秒呼ぶため）
        private static List<MinigameDefinition> GetPool()
        {
            var pool = new List<MinigameDefinition>();
            foreach (ulong id in GameManager.PossibleMinigameIDs)
            {
                if (GameManager.minigame_map.TryGetValue(id, out MinigameDefinition minigame) && minigame != null)
                {
                    pool.Add(minigame);
                }
            }
            return pool;
        }

        // チャット用のヘルプ
        public static List<string> Help(string overlayKey)
        {
            return new List<string>
            {
                Prefix + "コマンド一覧（自分の画面にだけ表示されます）",
                "/mv rank [ページ] … 有効なミニゲームの優先順位",
                "/mv pool [ページ] … 今回のゲームの候補",
                $"<color={ColorNote}>{overlayKey} キーで一覧を表示（ローカルプレイでも使えます）</color>",
                $"<color={ColorNote}>優先順位：①今の人数で遊べる ②未プレイ ③遊んだのが古い。未プレイ同士の順番は毎回ランダム</color>",
            };
        }

        // 長い一覧をページに分ける（チャット用）
        public static List<string> Page(string header, List<string> entries, int page, int pageSize, string command)
        {
            int pages = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            page = Math.Max(1, Math.Min(page, pages));

            var lines = new List<string> { Prefix + header + $"　{page}/{pages}ページ" };
            lines.AddRange(entries.Skip((page - 1) * pageSize).Take(pageSize));
            if (page < pages)
            {
                lines.Add($"<color={ColorNote}>続きは {command} {page + 1}</color>");
            }
            return lines;
        }

        // 1ミニゲームぶんの行。例:「3. ★Snowy Spin  未プレイ」
        private static string FormatEntry(int rank, MinigameDefinition minigame, int playerCount, HashSet<ulong> poolIds)
        {
            string mark = poolIds.Contains(minigame.MinigameID) ? $"<color={ColorPool}>★</color>" : "　";
            return $"{rank,2}. {mark}{minigame.minigameName}  {DescribeStatus(minigame, playerCount)}";
        }

        // 遊べるか・最後に遊んだのはいつか
        private static string DescribeStatus(MinigameDefinition minigame, int playerCount)
        {
            if (!MinigamePriority.IsPlayable(minigame, playerCount))
            {
                return $"<color={ColorNotPlayable}>人数NG（{minigame.minPlayers}〜{minigame.maxPlayers}人）</color>";
            }

            long lastPlayed = Plugin.History.GetLastPlayed(minigame.MinigameID);
            if (lastPlayed == 0)
            {
                return $"<color={ColorUnplayed}>未プレイ</color>";
            }

            // いちばん最近遊んだものが「1個前」
            long ago = Plugin.History.LatestOrder - lastPlayed + 1;
            return $"<color={ColorPlayed}>{ago}個前に遊んだ</color>";
        }

        // 今回の候補のID（ホスト以外は参加時に受け取ったID）
        private static HashSet<ulong> GetPoolIds()
        {
            return new HashSet<ulong>(GameManager.PossibleMinigameIDs);
        }
    }
}
