using System.Collections.Generic;
using I2.Loc;
using UnityEngine;

namespace BananaItem
{
    // バナナの数値（全員で同じ値でないとズレるので固定）
    internal static class BananaRules
    {
        public const int Damage = 15;
        public const int DurationRounds = 2;
    }

    // 名前・説明（言語に合わせて切り替える）
    internal static class BananaText
    {
        private static bool IsJapanese
        {
            get
            {
                string code = LocalizationManager.CurrentLanguageCode;
                return !string.IsNullOrEmpty(code) && code.StartsWith("ja");
            }
        }

        public static string Name => IsJapanese ? "バナナ" : "Banana";

        public static string Description => IsJapanese
            ? $"使ったマスにバナナを置く。{BananaRules.DurationRounds}ラウンドの間、そのマスに止まった他のプレイヤーは{BananaRules.Damage}ダメージ（1回で消える）。"
            : $"Drops a banana on your space. For {BananaRules.DurationRounds} rounds, the next other player who stops there takes {BananaRules.Damage} damage.";
    }

    // アイテム定義（ItemDetails）とネット用テンプレートを作って、ゲームに登録する
    internal static class BananaDefinition
    {
        // ネット用プレハブの名前（全員で同じ名前で登録する）
        public const string NetPrefabName = "PummelBananaItem";

        public static ItemDetails Details { get; private set; }

        // MOD_Item 付きのテンプレート（装備したときにこれがネットで生成される）
        public static GameObject ItemTemplate { get; private set; }

        // 見た目のテンプレート（手に持つ・入手演出・マスに置く）
        public static GameObject ModelTemplate { get; private set; }

        public static bool IsRegistered => Details != null;

        public static bool IsBanana(ItemDetails details)
        {
            return details != null && Details != null && details == Details;
        }

        // ゲームの準備ができていれば登録する。できていなければ false（次のフレームで再挑戦）
        public static bool TryRegister()
        {
            if (Details != null)
            {
                return true;
            }
            if (GameManager.ItemList == null || GameManager.ItemList.items == null || GameManager.RulesetManager == null)
            {
                return false;
            }

            // 非表示の親の下に置いたテンプレートは動かない。コピーしたもの（親なし）は表示されて動く
            var templateRoot = new GameObject("PummelBananaTemplates");
            templateRoot.SetActive(false);
            templateRoot.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(templateRoot);

            ModelTemplate = BananaVisual.CreateModel("PummelBananaModel");
            ModelTemplate.transform.SetParent(templateRoot.transform, false);

            // 入手したときの演出用は大きめにする
            GameObject receiveTemplate = Object.Instantiate(ModelTemplate, templateRoot.transform, false);
            receiveTemplate.name = "PummelBananaReceive";
            receiveTemplate.transform.localScale = Vector3.one * 2.2f;

            ItemDetails details = ScriptableObject.CreateInstance<ItemDetails>();
            details.name = "PummelBanana";
            details.itemID = FindFreeItemId();
            details.itemNameToken = "Banana";
            details.descriptionToken = "Banana";
            details.netPrefabName = NetPrefabName;
            details.recievePrefab = receiveTemplate;
            details.heldPrefab = ModelTemplate;
            details.iconTexture = BananaVisual.CreateIcon();

            ItemTemplate = new GameObject(NetPrefabName);
            ItemTemplate.transform.SetParent(templateRoot.transform, false);
            MOD_Item item = ItemTemplate.AddComponent<MOD_Item>();
            item.details = details;
            // AIが構えたら自動で使う
            item.aiAutoUse = true;
            details.prefab = ItemTemplate;

            // OnAddItem が空のまま AddItem を呼ぶと落ちるので、何もしない処理を足しておく
            GameManager.OnAddItem += OnItemAdded;
            GameManager.AddItem(details);
            Details = details;

            Plugin.Log.LogInfo($"バナナを登録しました（itemID {details.itemID}）");
            return true;
        }

        private static void OnItemAdded()
        {
        }

        // ActionEquipItem はアイテムIDを byte で送るので、0〜255 の空いているIDを使う
        // （既存アイテムのIDは全員同じなので、全員で同じ値になる）
        private static ulong FindFreeItemId()
        {
            var used = new HashSet<ulong>();
            foreach (ItemDetails item in GameManager.ItemList.items)
            {
                if (item != null)
                {
                    used.Add(item.itemID);
                }
            }

            for (ulong id = 250; id >= 100; id--)
            {
                if (!used.Contains(id))
                {
                    return id;
                }
            }
            return 251;
        }
    }
}
