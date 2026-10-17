using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace MinigameVariety
{
    // F8 で開く一覧表示。押すたびに「優先順位 → 今回の候補 → 閉じる」と切り替わる。Esc ですぐ閉じる
    // ボードではマウスのクリックがダイスに使われるので、ボタンは置かずキーだけで操作する
    internal class PriorityOverlay : MonoBehaviour
    {
        private enum View
        {
            Closed,
            Rank,
            Pool,
        }

        private const float RefreshSeconds = 1f;

        private View _view = View.Closed;
        private readonly List<string> _lines = new List<string>();
        private string _header = "";
        private float _nextRefresh;
        private Vector2 _scroll;
        private GUIStyle _style;
        private GUIStyle _boxStyle;

        // 調査用のログを出したか（1回だけ出す）
        private static bool _loggedBlockingKeys;

        private void Update()
        {
            if (WasPressed(Plugin.OverlayKey.Value))
            {
                _view = _view == View.Closed ? View.Rank : _view == View.Rank ? View.Pool : View.Closed;
                _scroll = Vector2.zero;
                _nextRefresh = 0f;
            }
            else if (_view != View.Closed && WasPressed(Plugin.OverlayCloseKey.Value))
            {
                // 開いているときだけ、閉じるキー（既定は Esc）ですぐ閉じる
                _view = View.Closed;
            }

            // 開いている間だけ、1秒ごとに中身を作り直す
            if (_view != View.Closed && Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }

        // キーが押された瞬間か。BepInEx の IsDown() は「ほかのキーが1つでも押されている」と false になるため
        // （移動キーを押しながら、Esc が押されたままと判定される、など）、メインキーと指定した修飾キーだけを見る
        private static bool WasPressed(KeyboardShortcut shortcut)
        {
            KeyCode mainKey = shortcut.MainKey;
            if (mainKey == KeyCode.None || !UnityInput.Current.GetKeyDown(mainKey))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!UnityInput.Current.GetKey(modifier))
                {
                    return false;
                }
            }

            LogBlockingKeysOnce(shortcut);
            return true;
        }

        // 調査用：IsDown() なら反応しなかった場面で、押されたままと判定されたキーを1回だけログに出す
        private static void LogBlockingKeysOnce(KeyboardShortcut shortcut)
        {
            if (_loggedBlockingKeys || shortcut.IsDown())
            {
                return;
            }
            _loggedBlockingKeys = true;

            var held = new List<string>();
            foreach (KeyCode key in UnityInput.Current.SupportedKeyCodes)
            {
                // IsDown() と同じく、マウスボタンとショートカット自身のキーは数えない
                bool isMouse = key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;
                bool isOwnKey = key == shortcut.MainKey || shortcut.Modifiers.Contains(key);
                if (!isMouse && !isOwnKey && UnityInput.Current.GetKey(key))
                {
                    held.Add(key.ToString());
                }
            }
            Plugin.Log.LogInfo($"{shortcut} を押したとき、ほかに押されたままと判定されたキー: {string.Join(", ", held)}");
        }

        private void Refresh()
        {
            _lines.Clear();
            try
            {
                if (_view == View.Rank)
                {
                    _header = PriorityReport.RankHeader();
                    _lines.AddRange(PriorityReport.RankEntries());
                }
                else
                {
                    _header = PriorityReport.PoolHeader();
                    _lines.AddRange(PriorityReport.PoolEntries());
                }
            }
            catch (System.Exception e)
            {
                // ゲームの準備ができていないタイミングなど。次の更新で再挑戦する
                _header = "まだ表示できません";
                Plugin.Log.LogDebug($"一覧表示の更新に失敗: {e.Message}");
            }
        }

        private void OnGUI()
        {
            if (_view == View.Closed)
            {
                return;
            }

            if (_style == null)
            {
                // <color> などのリッチテキストを有効にする
                _style = new GUIStyle(GUI.skin.label) { richText = true, wordWrap = false, fontSize = 15 };
                _boxStyle = new GUIStyle(GUI.skin.box);
            }

            float width = 520f;
            float height = Mathf.Min(Screen.height * 0.75f, 40f + 24f * (_lines.Count + 2));
            GUILayout.BeginArea(new Rect(20f, 40f, width, height), _boxStyle);
            GUILayout.Label(_header, _style);

            // 長い一覧はマウスホイールでスクロールできる
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (string line in _lines)
            {
                GUILayout.Label(line, _style);
            }
            GUILayout.EndScrollView();

            string next = _view == View.Rank ? "今回の候補" : "閉じる";
            GUILayout.Label($"<color=#aaaaaa>{Plugin.OverlayKey.Value} で{next}　{Plugin.OverlayCloseKey.Value} で閉じる</color>", _style);
            GUILayout.EndArea();
        }
    }
}
