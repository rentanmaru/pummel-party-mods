using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MinigameVariety
{
    // 遊んだミニゲームの記録。IDごとに「最後に遊んだ順番」を持つ（数字が大きいほど最近）
    internal class PlayHistory
    {
        private readonly string _path;
        private readonly Dictionary<ulong, long> _lastPlayed = new Dictionary<ulong, long>();
        private long _counter;

        public PlayHistory(string path)
        {
            _path = path;
        }

        public int Count => _lastPlayed.Count;

        // いちばん最近に遊んだミニゲームの順番
        public long LatestOrder => _counter;

        // 一度も遊んでいなければ 0 を返す
        public long GetLastPlayed(ulong minigameId)
        {
            return _lastPlayed.TryGetValue(minigameId, out long order) ? order : 0;
        }

        // 遊んだことを記録してすぐ保存する
        public void RecordPlayed(ulong minigameId)
        {
            _counter++;
            _lastPlayed[minigameId] = _counter;
            Save();
        }

        public void Clear()
        {
            _lastPlayed.Clear();
            _counter = 0;
            Save();
        }

        public void Load()
        {
            _lastPlayed.Clear();
            _counter = 0;
            if (!File.Exists(_path))
            {
                return;
            }

            try
            {
                foreach (string line in File.ReadAllLines(_path))
                {
                    // 「ID<TAB>順番」の形式。# で始まる行と壊れた行は読み飛ばす
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string[] parts = line.Split('\t');
                    if (parts.Length != 2
                        || !ulong.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out ulong id)
                        || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out long order))
                    {
                        continue;
                    }

                    _lastPlayed[id] = order;
                    _counter = Math.Max(_counter, order);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"遊んだ記録を読み込めませんでした: {e.Message}");
            }
        }

        private void Save()
        {
            try
            {
                var lines = new List<string> { "# MinigameVariety: minigameID<TAB>playOrder (bigger = more recent)" };
                foreach (KeyValuePair<ulong, long> pair in _lastPlayed)
                {
                    lines.Add(pair.Key.ToString(CultureInfo.InvariantCulture) + "\t" + pair.Value.ToString(CultureInfo.InvariantCulture));
                }
                File.WriteAllLines(_path, lines);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"遊んだ記録を保存できませんでした: {e.Message}");
            }
        }
    }
}
