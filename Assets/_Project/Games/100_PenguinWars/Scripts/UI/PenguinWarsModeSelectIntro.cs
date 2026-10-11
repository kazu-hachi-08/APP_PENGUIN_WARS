using System.Collections.Generic;
using PenguinWars.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenguinWars
{
    /// <summary>
    /// 共通のモード選択パネル（ModeSelectPanel）に付ける、ペンギン大戦争の飾り（仕様書 §2.0）。
    /// 開くたびにカードのペンギンと★を選び直し、左右のカードを外から滑り込ませる。
    /// 接続・待機の処理は ModeSelectPanel のまま触らない（共通基盤にゲーム固有の処理を入れないため）
    /// </summary>
    public class PenguinWarsModeSelectIntro : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("左（ステージ）・右（オンライン）の順。登場演出でそれぞれ外側から滑り込む")]
        [SerializeField] private RectTransform[] _cards;
        [SerializeField] private CanvasGroup[] _cardGroups;
        [SerializeField] private Image _soloIcon;
        [Tooltip("左が味方・右が敵の向き。向かい合わせて対戦らしく見せる")]
        [SerializeField] private Image[] _versusIcons;
        [SerializeField] private Text _starsLabel;

        [Header("動き")]
        [SerializeField] private float _hopHeight = 18f;
        [SerializeField] private float _hopSpeed = 6f;
        [SerializeField] private float _hopPhaseStep = 0.8f;

        [Header("登場演出（秒は開いてからの時間）")]
        [SerializeField] private float _slideDistance = 1200f;
        [SerializeField] private float _slideTime = 0.45f;
        [Tooltip("右のカードを遅らせて、左右が順に来るように見せる")]
        [SerializeField] private float _slideStagger = 0.1f;

        // タイトルのロゴと同じ行き過ぎ量。カードが少し行き過ぎて戻る「着地」に見せる
        private const float Overshoot = 1.70158f;

        private Vector2[] _cardBasePositions;
        private Image[] _hopIcons;
        private Vector2[] _hopBasePositions;
        private float _introTime;

        private float IntroEndTime => _slideStagger * (_cards.Length - 1) + _slideTime;

        private void Awake()
        {
            _cardBasePositions = new Vector2[_cards.Length];
            for (int i = 0; i < _cards.Length; i++) _cardBasePositions[i] = _cards[i].anchoredPosition;

            var icons = new List<Image> { _soloIcon };
            icons.AddRange(_versusIcons);
            _hopIcons = icons.ToArray();
            _hopBasePositions = new Vector2[_hopIcons.Length];
            for (int i = 0; i < _hopIcons.Length; i++) _hopBasePositions[i] = _hopIcons[i].rectTransform.anchoredPosition;
        }

        /// <summary>ModeSelectPanel.Show で有効になるたびに流す。待機からメニューに戻るときは有効のままなので流さない</summary>
        private void OnEnable()
        {
            PickIcons();
            _starsLabel.text = PenguinWarsTitlePanel.FormatStarTotal();
            _introTime = 0f;
            // 次の Update を待つと、1フレームだけ登場前の最終形が見えてしまう
            Animate();
        }

        /// <summary>演出の途中で画面を押したら、すぐ最後の形にする（タイトルと同じく待たせない）</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            _introTime = Mathf.Max(_introTime, IntroEndTime);
        }

        private void Update()
        {
            _introTime += Time.unscaledDeltaTime;
            Animate();
        }

        private void Animate()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                float t = Mathf.Clamp01((_introTime - i * _slideStagger) / _slideTime);
                // 左のカードは左から、右のカードは右から
                float direction = _cardBasePositions[i].x < 0f ? -1f : 1f;
                _cards[i].anchoredPosition = _cardBasePositions[i] + Vector2.right * (direction * (1f - EaseOutBack(t)) * _slideDistance);
                // 滑ってくる途中のボタンを押せないようにし、その間のタップは OnPointerClick（早送り）に届ける
                _cardGroups[i].blocksRaycasts = t >= 1f;
            }

            float time = Time.unscaledTime;
            for (int i = 0; i < _hopIcons.Length; i++)
            {
                // Abs で地面に着いたら跳ね返る動きにする
                float hop = Mathf.Abs(Mathf.Sin(time * _hopSpeed + i * _hopPhaseStep)) * _hopHeight;
                _hopIcons[i].rectTransform.anchoredPosition = _hopBasePositions[i] + Vector2.up * hop;
            }
        }

        private static float EaseOutBack(float t)
        {
            float p = t - 1f;
            return 1f + (Overshoot + 1f) * p * p * p + Overshoot * p * p;
        }

        /// <summary>開くたびに違う顔ぶれにする（全キャラから重複なし）</summary>
        private void PickIcons()
        {
            var units = new List<PenguinUnitData>(_catalog.Units);
            // カタログの空き枠を選ぶと絵の無いペンギンが混ざるので除く
            units.RemoveAll(u => u == null);
            SetIcon(_soloIcon, TakeRandom(units), Side.Left);
            for (int i = 0; i < _versusIcons.Length; i++)
            {
                SetIcon(_versusIcons[i], TakeRandom(units), i == 0 ? Side.Left : Side.Right);
            }
        }

        private static PenguinUnitData TakeRandom(List<PenguinUnitData> units)
        {
            if (units.Count == 0) return null;

            int index = Random.Range(0, units.Count);
            PenguinUnitData unit = units[index];
            units.RemoveAt(index);
            return unit;
        }

        private static void SetIcon(Image image, PenguinUnitData unit, Side side)
        {
            Sprite icon = unit != null ? unit.GetSprites(side).Icon : null;
            image.sprite = icon;
            image.enabled = icon != null;
        }
    }
}
