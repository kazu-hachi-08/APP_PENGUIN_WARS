using PenguinWars.Common.Online;
using PenguinWars.Common.Editor;
using UnityEngine;

namespace PenguinWars.Editor
{
    /// <summary>オンライン対戦（接続・同期）とモード選択パネル（見た目の組み替えは .ModeSelect）。NetworkManager は OnlineSession が実行時に作るので Scene には置かない</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const string OfflineModeLabel = "ステージ";

        private struct OnlineParts
        {
            public OnlineSession Session;
            public PenguinWarsOnlineLink Link;
            public ModeSelectPanel ModeSelectPanel;
        }

        /// <summary>モード選択パネルは canvas 直下の後ろの方に作り、操作UI・編成発表より手前に出す</summary>
        private static OnlineParts CreateOnline(Transform canvas, BattleRunner battleRunner, PenguinUnitCatalog catalog)
        {
            var onlineObj = new GameObject("Online");
            var parts = new OnlineParts
            {
                Session = onlineObj.AddComponent<OnlineSession>(),
                Link = onlineObj.AddComponent<PenguinWarsOnlineLink>(),
            };
            SetRefs(parts.Link, ("_battleRunner", battleRunner));

            var guestView = onlineObj.AddComponent<GuestBattleView>();
            SetRefs(guestView, ("_link", parts.Link), ("_battleRunner", battleRunner));

            parts.ModeSelectPanel = ModeSelectPanelBuilder.Create(canvas, parts.Session, OfflineModeLabel);
            DecorateModeSelect(parts.ModeSelectPanel, catalog);
            return parts;
        }
    }
}
