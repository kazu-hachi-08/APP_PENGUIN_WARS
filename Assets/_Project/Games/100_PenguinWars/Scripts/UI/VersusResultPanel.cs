using System;
using System.Collections.Generic;
using PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PenguinWars
{
    public enum VersusOutcome
    {
        Win,
        Lose,
        Draw,
    }

    /// <summary>対戦のリザルトに出す中身。引数が多くなりすぎないよう1つにまとめる</summary>
    public class VersusResultContent
    {
        public VersusOutcome Outcome;
        public string Title;
        public string Detail;
        public string MyName;
        public string OpponentName;
        /// <summary>城の残りHP（0〜100）。0 から数え上げて見せる</summary>
        public int MyPercent;
        public int OpponentPercent;
        /// <summary>ドラフト中に切断されたときは空（編成の列を出さない）</summary>
        public IReadOnlyList<UnitStats> MyDeck = Array.Empty<UnitStats>();
        public IReadOnlyList<UnitStats> OpponentDeck = Array.Empty<UnitStats>();
    }

    /// <summary>
    /// 対戦のリザルト（仕様書 §2.3・§9）。共通の ResultDialog は文字だけなので、左に自分・右に相手の編成を並べ、
    /// 勝った側は跳ねて喜び、負けた側は暗くしてやられたコマにする。タイトルはポンと出て、城の残りHPは 0% から数え上げる
    /// </summary>
    public class VersusResultPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private RectTransform _title;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _scoreLabel;
        [SerializeField] private Text _detailLabel;
        [SerializeField] private Text _myNameLabel;
        [SerializeField] private Text _opponentNameLabel;
        [Tooltip("枠の並び順が編成の順")]
        [SerializeField] private UnitSpriteAnimator[] _myIcons;
        [SerializeField] private UnitSpriteAnimator[] _opponentIcons;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        [Header("演出")]
        [SerializeField] private float _titlePopDuration = 0.35f;
        [SerializeField] private float _titlePopStartScale = 2.5f;
        [SerializeField] private float _countUpDuration = 1f;
        [SerializeField] private float _hopHeight = 30f;
        [SerializeField] private float _hopCycleSeconds = 0.5f;
        [Tooltip("隣どうしの跳ねるタイミングのずれ（周期に対する割合）。揃って跳ねると機械的に見えるため")]
        [SerializeField] private float _hopPhaseStep = 0.15f;
        [SerializeField] private Color _loserTint = new Color(0.45f, 0.45f, 0.5f);
        [SerializeField] private Color _winTitleColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _loseTitleColor = new Color(0.6f, 0.7f, 0.9f);
        [SerializeField] private Color _drawTitleColor = Color.white;

        private static readonly PenguinFrame[] DefeatedFrames = { PenguinFrame.Knockback };

        private VersusResultContent _content;
        private float _shownTime;
        private Vector2[] _myBasePositions;
        private Vector2[] _opponentBasePositions;
        private Action _onRetry;
        private Action _onTitle;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => Choose(_onRetry));
            _titleButton.onClick.AddListener(() => Choose(_onTitle));
        }

        public void Show(VersusResultContent content, Action onRetry, Action onTitle)
        {
            _content = content;
            _onRetry = onRetry;
            _onTitle = onTitle;
            _titleLabel.text = content.Title;
            _titleLabel.color = TitleColor(content.Outcome);
            _detailLabel.text = content.Detail;
            _myNameLabel.text = content.MyName;
            _opponentNameLabel.text = content.OpponentName;
            // 非表示のまま Show されると Awake より先にここへ来るので、最初の1回で元の位置を覚える
            _myBasePositions ??= CapturePositions(_myIcons);
            _opponentBasePositions ??= CapturePositions(_opponentIcons);
            ShowDeck(_myIcons, content.MyDeck, Side.Left, content.Outcome == VersusOutcome.Lose);
            ShowDeck(_opponentIcons, content.OpponentDeck, Side.Right, content.Outcome == VersusOutcome.Win);
            _shownTime = Time.unscaledTime;
            gameObject.SetActive(true);
            Animate();
        }

        private void Update()
        {
            Animate();
        }

        private void Animate()
        {
            float elapsed = Time.unscaledTime - _shownTime;
            AnimateTitle(elapsed);
            AnimateScore(elapsed);
            if (_content.Outcome == VersusOutcome.Win) Hop(_myIcons, _myBasePositions, elapsed);
            else if (_content.Outcome == VersusOutcome.Lose) Hop(_opponentIcons, _opponentBasePositions, elapsed);
        }

        /// <summary>大きい文字が上から叩きつけられるように縮んで止まる</summary>
        private void AnimateTitle(float elapsed)
        {
            float rate = Mathf.Clamp01(elapsed / _titlePopDuration);
            // 少し行き過ぎてから戻る（EaseOutBack）と「ドン」と止まった感じになる
            const float overshoot = 1.7f;
            float eased = 1f + (overshoot + 1f) * Mathf.Pow(rate - 1f, 3f) + overshoot * Mathf.Pow(rate - 1f, 2f);
            _title.localScale = Vector3.one * Mathf.LerpUnclamped(_titlePopStartScale, 1f, eased);
        }

        /// <summary>タイトルが止まってから数え上げる（同時に動くとどちらも見てもらえないため）</summary>
        private void AnimateScore(float elapsed)
        {
            float rate = Mathf.Clamp01((elapsed - _titlePopDuration) / _countUpDuration);
            int mine = Mathf.RoundToInt(_content.MyPercent * rate);
            int theirs = Mathf.RoundToInt(_content.OpponentPercent * rate);
            _scoreLabel.text = $"{mine}% - {theirs}%";
        }

        private void Hop(UnitSpriteAnimator[] icons, Vector2[] basePositions, float elapsed)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                float cycle = elapsed / _hopCycleSeconds + i * _hopPhaseStep;
                float height = Mathf.Abs(Mathf.Sin(cycle * Mathf.PI)) * _hopHeight;
                ((RectTransform)icons[i].transform).anchoredPosition = basePositions[i] + Vector2.up * height;
            }
        }

        private void ShowDeck(UnitSpriteAnimator[] icons, IReadOnlyList<UnitStats> deck, Side side, bool isLoser)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                PenguinUnitData data = i < deck.Count ? _catalog.Get(deck[i].UnitNo) : null;
                icons[i].gameObject.SetActive(data != null);
                if (data == null) continue;

                icons[i].Play(data.GetSprites(side), isLoser ? DefeatedFrames : UnitSpriteAnimator.WalkFrames, i);
                icons[i].SetTint(isLoser ? _loserTint : Color.white);
            }
        }

        private static Vector2[] CapturePositions(UnitSpriteAnimator[] icons)
        {
            var positions = new Vector2[icons.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                positions[i] = ((RectTransform)icons[i].transform).anchoredPosition;
            }
            return positions;
        }

        private Color TitleColor(VersusOutcome outcome)
        {
            switch (outcome)
            {
                case VersusOutcome.Win: return _winTitleColor;
                case VersusOutcome.Lose: return _loseTitleColor;
                default: return _drawTitleColor;
            }
        }

        private void Choose(Action action)
        {
            PenguinUiSound.Click();
            gameObject.SetActive(false);
            action?.Invoke();
        }
    }
}
