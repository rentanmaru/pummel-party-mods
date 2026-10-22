using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace BananaItem
{
    // Modの基本情報（manifest.json の version_number と合わせる）
    internal static class MyPluginInfo
    {
        public const string Guid = "com.renta.bananaitem";
        public const string Name = "BananaItem";
        public const string Version = "0.2.0";
    }

    // 新しいアイテム「バナナ」を追加するMod（全員導入が必要）
    [BepInPlugin(MyPluginInfo.Guid, MyPluginInfo.Name, MyPluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> DebugGiveKey;
        internal static ConfigEntry<KeyboardShortcut> DebugFillKey;
        internal static ConfigEntry<KeyboardShortcut> DebugRewardKey;

        private Harmony _harmony;
        private GameObject _runnerObject;

        private void Awake()
        {
            Log = Logger;

            // 設定は BepInEx\config\com.renta.bananaitem.cfg に保存される
            DebugGiveKey = Config.Bind(
                "Debug",
                "DebugGiveKey",
                new KeyboardShortcut(KeyCode.F10),
                "Key to get a banana for testing (local play only). / テスト用にバナナを1個もらうキー（ローカルプレイのみ）");
            DebugFillKey = Config.Bind(
                "Debug",
                "DebugFillKey",
                new KeyboardShortcut(KeyCode.F10, KeyCode.LeftControl),
                "Key to put bananas on every space for testing (local play only). / テスト用に全マスにバナナを置くキー（ローカルプレイのみ）");
            DebugRewardKey = Config.Bind(
                "Debug",
                "DebugRewardKey",
                new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift),
                "Key to toggle 'every minigame item reward is a banana' for testing (local play only). / テスト用に、ミニゲームのアイテム報酬を全部バナナにする切り替えキー（ローカルプレイのみ）");

            _harmony = new Harmony(MyPluginInfo.Guid);
            try
            {
                _harmony.PatchAll(typeof(Plugin).Assembly);
            }
            catch (Exception e)
            {
                // ゲーム更新でメソッド名が変わった場合などはここに来る
                Log.LogError($"パッチの適用に失敗しました: {e}");
            }

            // アイテム登録・テスト用キー・全員導入チェックは専用のオブジェクトで動かす（シーンが変わっても消えない）
            _runnerObject = new GameObject("BananaItemRunner");
            _runnerObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(_runnerObject);
            _runnerObject.AddComponent<BananaRunner>();
            _runnerObject.AddComponent<MultiplayerGuard>();

            int patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{MyPluginInfo.Name} v{MyPluginInfo.Version} 読み込み完了（パッチ {patched} 件）");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            if (_runnerObject != null)
            {
                Destroy(_runnerObject);
            }
        }
    }
}
