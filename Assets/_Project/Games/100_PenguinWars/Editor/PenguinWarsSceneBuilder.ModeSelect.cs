using System;
using PenguinWars.Common.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinWars.Editor
{
    /// <summary>
    /// 共通の ModeSelectPanelBuilder が作ったパネルを、ペンギン大戦争の見た目（左右2枚のカード）に組み替える（仕様書 §2.0）。
    /// ボタン・入力欄は作り直さずに移すだけなので、ModeSelectPanel の参照と処理はそのまま使える
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        // 空の戦場が少し透けるくらいにして、タイトルと同じ青みの幕にそろえる
        private static readonly Color ModeSelectDimColor = new Color(0f, 0.08f, 0.2f, 0.8f);
        // 待機中の文字は幕の上に直接出すので、箱は大きくして見えなくする
        private static readonly Vector2 ModeSelectBoxSize = new Vector2(1700f, 900f);

        private const string ModeSelectHeadingText = "どちらで あそぶ？";
        private const int ModeSelectHeadingFontSize = 64;
        private static readonly Vector2 ModeSelectHeadingPosition = new Vector2(0f, 400f);
        private static readonly Vector2 ModeSelectHeadingSize = new Vector2(1200f, 90f);

        private static readonly Vector2 ModeCardSize = new Vector2(720f, 640f);
        private const float ModeCardX = 390f;
        private const float ModeCardY = -10f;
        private static readonly Color SoloCardColor = new Color(0.12f, 0.3f, 0.55f, 0.95f);
        private static readonly Color VersusCardColor = new Color(0.1f, 0.38f, 0.25f, 0.95f);
        private static readonly Vector2 ModeCardOutline = new Vector2(4f, -4f);
        private static readonly Color ModeCardOutlineColor = new Color(1f, 1f, 1f, 0.6f);

        // カードの中の並び（カード中央からの位置）
        private const int ModeCardTitleFontSize = 60;
        private static readonly Vector2 ModeCardTitlePosition = new Vector2(0f, 255f);
        private static readonly Vector2 ModeCardTitleSize = new Vector2(640f, 80f);
        private static readonly Vector2 ModeCardIconSize = new Vector2(160f, 160f);
        private const float ModeCardIconY = 120f;
        private const float VersusIconOffsetX = 90f;
        private const int ModeCardDescFontSize = 32;
        private static readonly Vector2 ModeCardDescPosition = new Vector2(0f, 10f);
        private static readonly Vector2 ModeCardDescSize = new Vector2(640f, 90f);
        private const int ModeCardStarsFontSize = 40;
        private static readonly Vector2 ModeCardStarsPosition = new Vector2(0f, -80f);
        private static readonly Vector2 ModeCardStarsSize = new Vector2(600f, 56f);

        private static readonly Vector2 SoloButtonPosition = new Vector2(0f, -210f);
        private static readonly Vector2 SoloButtonSize = new Vector2(600f, 120f);
        private static readonly Vector2 HostButtonPosition = new Vector2(0f, -90f);
        private static readonly Vector2 HostButtonSize = new Vector2(600f, 100f);
        private const int JoinLabelFontSize = 26;
        private static readonly Vector2 JoinLabelPosition = new Vector2(0f, -165f);
        private static readonly Vector2 JoinLabelSize = new Vector2(600f, 40f);
        private static readonly Vector2 CodeInputPosition = new Vector2(-110f, -235f);
        private static readonly Vector2 CodeInputSize = new Vector2(380f, 80f);
        private static readonly Vector2 JoinButtonPosition = new Vector2(200f, -235f);
        private static readonly Vector2 JoinButtonSize = new Vector2(200f, 80f);
        private static readonly Vector2 ModeSelectTitleButtonPosition = new Vector2(0f, -410f);

        /// <summary>カードの1行説明。何をするモードか、押す前に分かるように</summary>
        private const string SoloCardTitle = "ひとりで";
        private const string SoloCardDesc = "解放したなかまで\n敵の城を攻める";
        private const string VersusCardTitle = "ともだちと";
        private const string VersusCardDesc = "全50体からドラフトして\n1対1の城攻め";
        private const string JoinLabelText = "ともだちのコードで参加";

        private static void DecorateModeSelect(ModeSelectPanel panel, PenguinUnitCatalog catalog)
        {
            Transform root = panel.transform;
            root.GetComponent<Image>().color = ModeSelectDimColor;

            Transform box = FindModeSelectPart(root, "Panel");
            box.GetComponent<Image>().color = Color.clear;
            box.GetComponent<RectTransform>().sizeDelta = ModeSelectBoxSize;

            Transform menu = FindModeSelectPart(box, "Menu");
            Text heading = FindModeSelectPart(menu, "TitleText").GetComponent<Text>();
            heading.text = ModeSelectHeadingText;
            heading.fontSize = ModeSelectHeadingFontSize;
            PlaceCentered(heading.transform, menu, ModeSelectHeadingPosition, ModeSelectHeadingSize);

            (RectTransform soloCard, Image soloIcon, Text stars) = CreateSoloCard(menu);
            (RectTransform versusCard, Image[] versusIcons) = CreateVersusCard(menu);
            PlaceCentered(FindModeSelectPart(menu, "Btn_Title"), menu, ModeSelectTitleButtonPosition, null);

            var intro = root.gameObject.AddComponent<PenguinWarsModeSelectIntro>();
            var so = new UnityEditor.SerializedObject(intro);
            SerializedArray(so, "_cards", new UnityEngine.Object[] { soloCard, versusCard });
            SerializedArray(so, "_cardGroups", new UnityEngine.Object[] { soloCard.GetComponent<CanvasGroup>(), versusCard.GetComponent<CanvasGroup>() });
            SerializedArray(so, "_versusIcons", versusIcons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(intro, ("_catalog", catalog), ("_soloIcon", soloIcon), ("_starsLabel", stars));
        }

        private static (RectTransform card, Image icon, Text stars) CreateSoloCard(Transform menu)
        {
            RectTransform card = CreateModeCard(menu, "Card_Solo", -ModeCardX, SoloCardColor, SoloCardTitle, SoloCardDesc);
            Image icon = CreateModeCardIcon(card, "Icon", 0f);
            Text stars = CreateText(card, "Stars", ModeCardStarsFontSize, CenterAnchor, ModeCardStarsPosition, ModeCardStarsSize, MessageColor);
            MoveModeButton(menu, "Btn_Npc", card, SoloButtonPosition, SoloButtonSize, StartButtonColor);
            return (card, icon, stars);
        }

        private static (RectTransform card, Image[] icons) CreateVersusCard(Transform menu)
        {
            RectTransform card = CreateModeCard(menu, "Card_Versus", ModeCardX, VersusCardColor, VersusCardTitle, VersusCardDesc);
            var icons = new[] { CreateModeCardIcon(card, "IconLeft", -VersusIconOffsetX), CreateModeCardIcon(card, "IconRight", VersusIconOffsetX) };

            MoveModeButton(menu, "Btn_Host", card, HostButtonPosition, HostButtonSize, StartButtonColor);
            Text joinLabel = FindModeSelectPart(menu, "JoinLabel").GetComponent<Text>();
            joinLabel.text = JoinLabelText;
            joinLabel.fontSize = JoinLabelFontSize;
            PlaceCentered(joinLabel.transform, card, JoinLabelPosition, JoinLabelSize);
            PlaceCentered(FindModeSelectPart(menu, "CodeInput"), card, CodeInputPosition, CodeInputSize);
            PlaceCentered(FindModeSelectPart(menu, "Btn_Join"), card, JoinButtonPosition, JoinButtonSize);
            return (card, icons);
        }

        /// <summary>カードは Menu の子にする。接続待ちになると ModeSelectPanel が Menu ごと隠すので、一緒に消える</summary>
        private static RectTransform CreateModeCard(Transform menu, string name, float x, Color color, string title, string desc)
        {
            Image card = CreateImage(menu, name, CenterAnchor, new Vector2(x, ModeCardY), ModeCardSize, color);
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectDistance = ModeCardOutline;
            outline.effectColor = ModeCardOutlineColor;
            // 滑ってくる途中はボタンを押せないようにする（PenguinWarsModeSelectIntro が切り替える）
            card.gameObject.AddComponent<CanvasGroup>();

            Text titleText = CreateText(card.transform, "Title", ModeCardTitleFontSize, CenterAnchor, ModeCardTitlePosition, ModeCardTitleSize, MessageColor);
            titleText.text = title;
            Text descText = CreateText(card.transform, "Desc", ModeCardDescFontSize, CenterAnchor, ModeCardDescPosition, ModeCardDescSize, Color.white);
            descText.text = desc;
            return card.rectTransform;
        }

        private static Image CreateModeCardIcon(Transform card, string name, float x)
        {
            Image icon = CreateImage(card, name, CenterAnchor, new Vector2(x, ModeCardIconY), ModeCardIconSize, Color.white);
            icon.preserveAspect = true;
            return icon;
        }

        private static void MoveModeButton(Transform menu, string name, Transform card, Vector2 position, Vector2 size, Color color)
        {
            Transform button = FindModeSelectPart(menu, name);
            button.GetComponent<Image>().color = color;
            button.GetComponentInChildren<Text>().fontSize = TitleButtonFontSize;
            PlaceCentered(button, card, position, size);
        }

        /// <param name="size">null なら大きさは変えない</param>
        private static void PlaceCentered(Transform target, Transform parent, Vector2 position, Vector2? size)
        {
            target.SetParent(parent, false);
            var rect = target.GetComponent<RectTransform>();
            SetAnchor(rect, CenterAnchor, position);
            if (size.HasValue) rect.sizeDelta = size.Value;
        }

        /// <summary>共通の Builder 側で名前が変わったら、黙って崩れるのではなく Rebuild の時点で気づけるようにする</summary>
        private static Transform FindModeSelectPart(Transform parent, string name)
        {
            Transform part = parent.Find(name);
            if (part == null) throw new InvalidOperationException($"{LogPrefix} モード選択の {parent.name}/{name} が見つかりません（ModeSelectPanelBuilder の名前が変わった？）");
            return part;
        }
    }
}
