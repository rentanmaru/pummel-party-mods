using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using ZP.Net;

namespace BananaItem
{
    // 全員導入チェック（オンラインのロビーだけ）
    // ・導入している人は自分の「メンバーデータ」に Mod のバージョンを書く
    // ・ホストは全員のデータを見て、全員そろっていればロビーデータに "1"、欠けていれば "0" を書く
    // ・ボード開始時、全員がロビーデータを見て "1" のときだけバナナを候補に入れる
    internal class MultiplayerGuard : MonoBehaviour
    {
        public const string MemberKey = "pp.banana";
        public const string LobbyKey = "pp.banana.enabled";
        private const float CheckIntervalSeconds = 2f;

        private float _nextCheck;
        private string _warning;
        private GUIStyle _style;

        // オンラインのロビーにいるか（ローカルプレイはチェック不要）
        private static bool IsOnlineLobby => GameManager.CurrentLobby != 0 && !(NetSystem.IsServer && NetGameServer.IsLocal);

        public static bool IsBananaAllowed()
        {
            if (!IsOnlineLobby)
            {
                return true;
            }
            try
            {
                return SteamMatchmaking.GetLobbyData((CSteamID)GameManager.CurrentLobby, LobbyKey) == "1";
            }
            catch (Exception)
            {
                // Steam が使えないなど。安全のため無効にする
                return false;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextCheck)
            {
                return;
            }
            _nextCheck = Time.unscaledTime + CheckIntervalSeconds;

            if (!IsOnlineLobby)
            {
                _warning = null;
                return;
            }

            try
            {
                var lobby = (CSteamID)GameManager.CurrentLobby;

                // 自分が導入していることを知らせる（変わったときだけ書く）
                if (SteamMatchmaking.GetLobbyMemberData(lobby, SteamUser.GetSteamID(), MemberKey) != MyPluginInfo.Version)
                {
                    SteamMatchmaking.SetLobbyMemberData(lobby, MemberKey, MyPluginInfo.Version);
                }

                if (NetSystem.IsServer)
                {
                    CheckMembersAsHost(lobby);
                }
                else
                {
                    _warning = null;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug($"全員導入チェックに失敗: {e.Message}");
            }
        }

        // ホストだけ：ロビーにいる間、全員が同じバージョンを入れているか調べる
        private void CheckMembersAsHost(CSteamID lobby)
        {
            // ゲーム中は決定を変えない（途中で変わるとズレるため）
            if (GameManager.CurState != GameState.MainMenu)
            {
                return;
            }

            var missing = new List<string>();
            int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (SteamMatchmaking.GetLobbyMemberData(lobby, member, MemberKey) != MyPluginInfo.Version)
                {
                    missing.Add(SteamFriends.GetFriendPersonaName(member));
                }
            }

            string value = missing.Count == 0 ? "1" : "0";
            if (SteamMatchmaking.GetLobbyData(lobby, LobbyKey) != value)
            {
                SteamMatchmaking.SetLobbyData(lobby, LobbyKey, value);
                Plugin.Log.LogInfo(missing.Count == 0
                    ? "全員が BananaItem を導入しているので、バナナを有効にします"
                    : $"BananaItem v{MyPluginInfo.Version} を入れていない人がいるので、バナナを無効にします: {string.Join(", ", missing)}");
            }

            _warning = missing.Count == 0
                ? null
                : $"BananaItem: 未導入の人がいるためバナナは無効 / Banana disabled (missing: {string.Join(", ", missing)})";
        }

        private void OnGUI()
        {
            if (_warning == null || GameManager.CurState != GameState.MainMenu)
            {
                return;
            }
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                _style.normal.textColor = new Color(1f, 0.75f, 0.3f);
            }
            GUI.Label(new Rect(10f, 34f, 900f, 24f), _warning, _style);
        }
    }
}
