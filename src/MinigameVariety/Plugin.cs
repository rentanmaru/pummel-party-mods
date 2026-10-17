using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MinigameVariety
{
    // Modの基本情報（manifest.json の version_number と合わせる）
    internal static class MyPluginInfo
    {
        public const string Guid = "com.renta.minigamevariety";
        public const string Name = "MinigameVariety";
        public const string Version = "0.1.0";
    }

    // 最近遊んでいないミニゲームを優先して候補に入れるMod（ホストだけ導入すれば効く）
    [BepInPlugin(MyPluginInfo.Guid, MyPluginInfo.Name, MyPluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // パッチ側からも使うもの
        internal static ManualLogSource Log;
        internal static PlayHistory History;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> LogPool;
        internal static ConfigEntry<KeyboardShortcut> OverlayKey;
        internal static ConfigEntry<KeyboardShortcut> OverlayCloseKey;

        private Harmony _harmony;
        private GameObject _overlayObject;

        private void Awake()
        {
            Log = Logger;

            // 設定は BepInEx\config\com.renta.minigamevariety.cfg に保存される
            Enabled = Config.Bind(
                "General",
                "Enabled",
                true,
                "Prefer minigames you haven't played recently when hosting. / ホスト時、最近遊んでいないミニゲームを優先する");
            ConfigEntry<bool> resetHistory = Config.Bind(
                "General",
                "ResetHistory",
                false,
                "Set to true to clear the play history on next launch (turns back to false). / true にすると次の起動時に履歴を消す（自動でfalseに戻る）");
            LogPool = Config.Bind(
                "Debug",
                "LogPool",
                true,
                "Write the minigame pool to the BepInEx log when a game starts. / ゲーム開始時に候補をBepInExのログに出す");
            OverlayKey = Config.Bind(
                "Overlay",
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F8),
                "Key to show the priority list (press again: current pool, again: close). / 優先順位の一覧を出すキー（もう一度で今回の候補、さらに押すと閉じる）");
            OverlayCloseKey = Config.Bind(
                "Overlay",
                "CloseKey",
                new KeyboardShortcut(KeyCode.Escape),
                "Key to close the list. / 一覧を閉じるキー");

            // 遊んだ記録は設定と同じフォルダに保存する
            History = new PlayHistory(Path.Combine(Paths.ConfigPath, MyPluginInfo.Guid + ".history.txt"));
            History.Load();
            if (resetHistory.Value)
            {
                History.Clear();
                resetHistory.Value = false;
                Log.LogInfo("遊んだ記録を消しました");
            }

            _harmony = new Harmony(MyPluginInfo.Guid);
            try
            {
                // このDLL内の [HarmonyPatch] をすべて適用する
                _harmony.PatchAll(typeof(Plugin).Assembly);
            }
            catch (Exception e)
            {
                // ゲーム更新でメソッド名が変わった場合などはここに来る
                Log.LogError($"パッチの適用に失敗しました: {e}");
            }

            // 一覧表示は専用のオブジェクトに載せる（シーンが切り替わっても消えないようにする）
            _overlayObject = new GameObject("MinigameVarietyOverlay");
            _overlayObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(_overlayObject);
            _overlayObject.AddComponent<PriorityOverlay>();

            int patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{MyPluginInfo.Name} v{MyPluginInfo.Version} 読み込み完了（パッチ {patched} 件、記録 {History.Count} 件）");
        }

        private void OnDestroy()
        {
            // 自分が当てたパッチと一覧表示だけを片付ける
            _harmony?.UnpatchSelf();
            if (_overlayObject != null)
            {
                Destroy(_overlayObject);
            }
        }
    }
}
