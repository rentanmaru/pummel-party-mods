using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PummelHello
{
    // Modの基本情報（manifest.json の version_number と合わせる）
    internal static class MyPluginInfo
    {
        public const string Guid = "com.renta.pummelhello";
        public const string Name = "PummelHello";
        public const string Version = "0.1.0";
    }

    // BepInExにプラグインとして認識させる
    [BepInPlugin(MyPluginInfo.Guid, MyPluginInfo.Name, MyPluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        // パッチ側からも使えるロガー
        internal static ManualLogSource Log;

        private Harmony _harmony;
        private ConfigEntry<bool> _showModdedLabel;

        private void Awake()
        {
            Log = Logger;

            // 設定は BepInEx\config\com.renta.pummelhello.cfg に保存される
            _showModdedLabel = Config.Bind(
                "General",
                "ShowModdedLabel",
                true,
                "Show a 'Modded' label in the top-left corner. / 画面左上にModded表示を出す");

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

            // 実際にパッチできたメソッド数を出す（0なら何かが壊れている）
            int patched = _harmony.GetPatchedMethods().Count();
            Log.LogInfo($"{MyPluginInfo.Name} v{MyPluginInfo.Version} 読み込み完了（パッチ {patched} 件）");
        }

        private void OnDestroy()
        {
            // 自分が当てたパッチだけを外す
            _harmony?.UnpatchSelf();
        }

        private void OnGUI()
        {
            if (!_showModdedLabel.Value)
            {
                return;
            }

            // Mod使用中であることを画面に表示する
            GUI.Label(new Rect(10, 10, 400, 22), $"Modded: {MyPluginInfo.Name} v{MyPluginInfo.Version}");
        }
    }
}
