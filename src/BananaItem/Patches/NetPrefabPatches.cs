using HarmonyLib;
using UnityEngine.ResourceManagement.AsyncOperations;
using ZP.Net;

namespace BananaItem.Patches
{
    // ネット用プレハブ「PummelBananaItem」を登録して、生成のときに自前のテンプレートを渡す
    // プレハブはネットで「番号」でやり取りされる。番号がずれると他のプレハブまで壊れるので、
    // バナナは必ず一番最後に登録する（未導入の人とも、ワークショップのプレハブの番号が一致する）
    [HarmonyPatch(typeof(NetObjectManager))]
    internal static class NetObjectManagerPatches
    {
        // セッションごとに作られるプレハブ表の初期化が終わったら、最後尾に登録
        [HarmonyPostfix]
        [HarmonyPatch(nameof(NetObjectManager.InitiateNetSendObjects))]
        private static void InitiateNetSendObjectsPostfix(NetObjectManager __instance)
        {
            NetPrefabRegistry.RegisterLast(__instance);
        }

        // 生成のとき：Addressables を探しに行くと失敗ログが出るので、先に自前のテンプレートを返す
        [HarmonyPrefix]
        [HarmonyPatch(nameof(NetObjectManager.GetNetPrefab))]
        private static bool GetNetPrefabPrefix(NetObjectManager __instance, string name, ref NetPrefab __result)
        {
            if (name != BananaDefinition.NetPrefabName || BananaDefinition.ItemTemplate == null)
            {
                return true;
            }
            if (!__instance.net_prefab_map.TryGetValue(name, out NetPrefabDefinition definition))
            {
                return true;
            }
            __result = new NetPrefab(BananaDefinition.ItemTemplate, (ushort)definition.prefab_id, default(AsyncOperationHandle<UnityEngine.GameObject>));
            return false;
        }
    }

    // ワークショップのプレハブを登録し直したあと（実行時のプレハブは一度全部消される）にも、最後尾に登録し直す
    [HarmonyPatch(typeof(WorkshopController), nameof(WorkshopController.SetupNetPrefabs))]
    internal static class WorkshopSetupNetPrefabsPatch
    {
        private static void Postfix()
        {
            NetObjectManager manager = NetSystem.IsServer ? NetGameServer.NetObjectManager : NetGameClient.NetObjectManager;
            if (manager != null)
            {
                NetPrefabRegistry.RegisterLast(manager);
            }
        }
    }

    internal static class NetPrefabRegistry
    {
        public static void RegisterLast(NetObjectManager manager)
        {
            // 二重登録すると番号の空き枠だけ増えてずれるので、すでにあれば何もしない
            if (manager.net_prefab_map.ContainsKey(BananaDefinition.NetPrefabName))
            {
                return;
            }

            // 毎回新しい定義を作る（prefab_id = -1 のものだけに番号が振られる）
            var definition = new NetPrefabDefinition
            {
                prefab_name = BananaDefinition.NetPrefabName,
                resource_location = "",
                runTimeCreated = true,
            };
            manager.AddNetPrefabs(new[] { definition }, giveId: true);
            Plugin.Log.LogDebug($"ネット用プレハブを登録しました: {definition.prefab_name}（番号 {definition.prefab_id}）");
        }
    }
}
