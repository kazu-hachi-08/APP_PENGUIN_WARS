using PenguinWars.Common.Editor;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinWars.Editor
{
    /// <summary>
    /// 対戦のリザルト（仕様書 §2.3・§9）。タイトル・城の残りHP・左に自分／右に相手の編成10体・ボタン2つ
    /// </summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private static readonly Vector2 VersusResultTitlePosition = new Vector2(0f, 350f);
        private static readonly Vector2 VersusResultTitleSize = new Vector2(1200f, 150f);
        private const int VersusResultTitleFontSize = 120;
        private static readonly Vector2 VersusResultScorePosition = new Vector2(0f, 220f);
        private static readonly Vector2 VersusResultScoreSize = new Vector2(900f, 100f);
        private const int VersusResultScoreFontSize = 72;
        private static readonly Vector2 VersusResultDetailPosition = new Vector2(0f, 145f);
        private static readonly Vector2 VersusResultDetailSize = new Vector2(900f, 60f);
        private const int VersusResultDetailFontSize = 38;

        // 編成は 5列×2段 を左右に1つずつ。勝った側が跳ねても名前に被らないよう、名前は少し上に置く
        private const int VersusResultDeckColumns = 5;
        private const int VersusResultDeckSlots = 10;
        private const float VersusResultDeckCenterX = 450f;
        private const float VersusResultDeckStepX = 150f;
        private const float VersusResultDeckFirstRowY = -50f;
        private const float VersusResultDeckStepY = -150f;
        private static readonly Vector2 VersusResultIconSize = new Vector2(140f, 140f);
        private const float VersusResultNameY = 85f;
        private static readonly Vector2 VersusResultNameSize = new Vector2(700f, 70f);
        private const int VersusResultNameFontSize = 44;
        private static readonly Vector2 VersusResultVsPosition = new Vector2(0f, -125f);
        private static readonly Vector2 VersusResultVsSize = new Vector2(160f, 120f);
        private const int VersusResultVsFontSize = 80;

        private static readonly Vector2 VersusResultButtonSize = new Vector2(380f, 110f);
        private const float VersusResultButtonY = -380f;
        private const float VersusResultButtonStepX = 210f;

        /// <summary>操作UI・HUDより手前、ポーズ・共通ダイアログより奥に作る</summary>
        private static VersusResultPanel CreateVersusResultPanel(Transform canvas, PenguinUnitCatalog catalog)
        {
            GameObject dimObj = CreateFullScreenPanel(canvas, "VersusResultPanel", DialogDimColor);
            // 箱の大きさはステージのリザルトと揃える
            Image box = CreateImage(dimObj.transform, "Box", CenterAnchor, Vector2.zero, StageResultBoxSize, DialogBoxColor);

            Text title = CreateText(box.transform, "Title", VersusResultTitleFontSize, CenterAnchor, VersusResultTitlePosition, VersusResultTitleSize, MessageColor);
            Text score = CreateText(box.transform, "Score", VersusResultScoreFontSize, CenterAnchor, VersusResultScorePosition, VersusResultScoreSize, Color.white);
            Text detail = CreateText(box.transform, "Detail", VersusResultDetailFontSize, CenterAnchor, VersusResultDetailPosition, VersusResultDetailSize, TitleSubColor);
            Text vs = CreateText(box.transform, "Vs", VersusResultVsFontSize, CenterAnchor, VersusResultVsPosition, VersusResultVsSize, MessageColor);
            vs.text = "VS";

            (Text myName, UnitSpriteAnimator[] myIcons) = CreateVersusResultDeck(box.transform, "MyDeck", -VersusResultDeckCenterX);
            (Text opponentName, UnitSpriteAnimator[] opponentIcons) = CreateVersusResultDeck(box.transform, "OpponentDeck", VersusResultDeckCenterX);

            // もう1戦してほしいので「もういちど対戦」を右に目立つ色で置く（ステージのリザルトの「つぎ」と同じ位置づけ）
            Button titleButton = CreateTitleButton(box.transform, "Btn_Title", "タイトル", VersusResultButtonSize,
                new Vector2(-VersusResultButtonStepX, VersusResultButtonY), TitleSubButtonColor);
            Button retry = CreateTitleButton(box.transform, "Btn_Retry", "もういちど対戦", VersusResultButtonSize,
                new Vector2(VersusResultButtonStepX, VersusResultButtonY), StartButtonColor);

            var panel = dimObj.AddComponent<VersusResultPanel>();
            var so = new UnityEditor.SerializedObject(panel);
            SerializedArray(so, "_myIcons", myIcons);
            SerializedArray(so, "_opponentIcons", opponentIcons);
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRefs(panel, ("_catalog", catalog), ("_title", title.rectTransform), ("_titleLabel", title), ("_scoreLabel", score),
                ("_detailLabel", detail), ("_myNameLabel", myName), ("_opponentNameLabel", opponentName),
                ("_retryButton", retry), ("_titleButton", titleButton));
            dimObj.SetActive(false);
            return panel;
        }

        /// <summary>名前の下に 5列×2段 で並べる。centerX は箱の中心からの横位置</summary>
        private static (Text name, UnitSpriteAnimator[] icons) CreateVersusResultDeck(Transform box, string groupName, float centerX)
        {
            GameObject group = UIDialogBuilder.CreateUIObject(groupName, box);
            UIDialogBuilder.SetStretchAll(group.GetComponent<RectTransform>());
            Text name = CreateText(group.transform, "Name", VersusResultNameFontSize, CenterAnchor, new Vector2(centerX, VersusResultNameY),
                VersusResultNameSize, Color.white);

            var icons = new UnitSpriteAnimator[VersusResultDeckSlots];
            for (int i = 0; i < VersusResultDeckSlots; i++)
            {
                int column = i % VersusResultDeckColumns;
                int row = i / VersusResultDeckColumns;
                float x = centerX + (column - (VersusResultDeckColumns - 1) * 0.5f) * VersusResultDeckStepX;
                float y = VersusResultDeckFirstRowY + row * VersusResultDeckStepY;
                icons[i] = CreateAnimatedIcon(group.transform, CenterAnchor, new Vector2(x, y), VersusResultIconSize);
                icons[i].name = $"Icon{i + 1}";
            }
            return (name, icons);
        }
    }
}
