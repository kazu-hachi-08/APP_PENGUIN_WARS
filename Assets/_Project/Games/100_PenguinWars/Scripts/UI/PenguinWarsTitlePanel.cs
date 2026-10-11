using System;
using System.Collections.Generic;
using MiniGame.Common.Profile;
using MiniGame.Common.UI;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のタイトル画面（仕様書 §2.0）。ロゴがふわふわ揺れ、ランダムなペンギンがぴょこぴょこ跳ねる。
    /// 何を押したらどこへ進むかは GameManager が決める（「終了」だけはアプリを閉じるのでここで完結させる）
    /// </summary>
    public class PenguinWarsTitlePanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private RectTransform _logo;
        [SerializeField] private Image[] _paradeIcons;
        [Tooltip("ステージの★の合計（例: ★ 32 / 54）")]
        [SerializeField] private Text _starsLabel;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _zukanButton;
        [SerializeField] private Button _customButton;
        [SerializeField] private Button _guideButton;
        [SerializeField] private Button _settingsButton;
        [Tooltip("iOS（ガイドライン上非推奨）とブラウザ版（閉じられない）では隠す")]
        [SerializeField] private Button _quitButton;
        [SerializeField] private PenguinWarsSettingsPanel _settingsPanel;
        [SerializeField] private PenguinZukanPanel _zukanPanel;
        [SerializeField] private CustomUnitPanel _customPanel;
        [Tooltip("タイトルの子ではなく Canvas 直下に置く。タイトルの幕を閉じて戦場を見せるため")]
        [SerializeField] private PenguinGuidePanel _guidePanel;
        [Tooltip("対戦で名前を表示するため、未設定ならタイトルで必ず決めさせる")]
        [SerializeField] private NameEntryDialog _nameEntryDialog;

        [Header("動き")]
        [SerializeField] private float _logoBobHeight = 12f;
        [SerializeField] private float _logoBobSpeed = 2f;
        [SerializeField] private float _hopHeight = 28f;
        [SerializeField] private float _hopSpeed = 6f;
        [Tooltip("隣のペンギンと跳ねるタイミングをずらす量。揃うと機械的に見えるため")]
        [SerializeField] private float _hopPhaseStep = 0.8f;

        private Action _onStart;
        private Vector2 _logoBasePosition;
        private Vector2[] _iconBasePositions;

        private void Awake()
        {
            _startButton.onClick.AddListener(() => Close(_onStart));
            _quitButton.onClick.AddListener(ConfirmQuit);
            _quitButton.gameObject.SetActive(CanQuit);
            _settingsButton.onClick.AddListener(OpenSettings);
            _zukanButton.onClick.AddListener(OpenZukan);
            _customButton.onClick.AddListener(OpenCustom);
            _guideButton.onClick.AddListener(OpenGuide);

            _logoBasePosition = _logo.anchoredPosition;
            _iconBasePositions = new Vector2[_paradeIcons.Length];
            for (int i = 0; i < _paradeIcons.Length; i++) _iconBasePositions[i] = _paradeIcons[i].rectTransform.anchoredPosition;
        }

#if UNITY_IOS || UNITY_WEBGL
        private const bool CanQuit = false;
#else
        private const bool CanQuit = true;
#endif

        public void Show(Action onStart)
        {
            _onStart = onStart;
            PickParade();
            ShowStars();
            _settingsPanel.Hide();
            _zukanPanel.Hide();
            _customPanel.Hide();
            gameObject.SetActive(true);
            if (!UserProfile.HasName) _nameEntryDialog.Show(onDecided: null);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            _logo.anchoredPosition = _logoBasePosition + Vector2.up * (Mathf.Sin(time * _logoBobSpeed) * _logoBobHeight);
            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                // Abs で地面に着いたら跳ね返る動きにする
                float hop = Mathf.Abs(Mathf.Sin(time * _hopSpeed + i * _hopPhaseStep)) * _hopHeight;
                _paradeIcons[i].rectTransform.anchoredPosition = _iconBasePositions[i] + Vector2.up * hop;
            }
        }

        /// <summary>毎回違う顔ぶれを見せたいので、開くたびに全キャラから重複なしで選び直す</summary>
        private void PickParade()
        {
            var units = new List<PenguinUnitData>(_catalog.Units);
            // カタログの空き枠を選ぶと絵の無いペンギンが混ざるので除く
            units.RemoveAll(u => u == null);
            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                Sprite icon = null;
                if (units.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, units.Count);
                    icon = units[index].GetSprites(Side.Left).Icon;
                    units.RemoveAt(index);
                }
                _paradeIcons[i].sprite = icon;
                _paradeIcons[i].enabled = icon != null;
            }
        }

        /// <summary>ステージをクリアしてタイトルに戻るたびに増えて見えるよう、開くたびにセーブから数え直す</summary>
        private void ShowStars()
        {
            CampaignProgress progress = CampaignSave.Load();
            int stars = 0;
            foreach (StageDefinition stage in StageDefinitions.All) stars += StarRule.Count(progress.GetStars(stage.Id));
            _starsLabel.text = $"★ {stars} / {StageDefinitions.All.Count * StarRule.MaxStars}";
        }

        private void OpenSettings()
        {
            PenguinUiSound.Click();
            _settingsPanel.Show();
        }

        private void OpenZukan()
        {
            PenguinUiSound.Click();
            _zukanPanel.Show();
        }

        private void OpenCustom()
        {
            PenguinUiSound.Click();
            _customPanel.Show();
        }

        /// <summary>デモの戦場が幕で暗くならないよう、タイトルごと隠してから開き、閉じたら戻す</summary>
        private void OpenGuide()
        {
            PenguinUiSound.Click();
            gameObject.SetActive(false);
            _guidePanel.Show(() => gameObject.SetActive(true));
        }

        private void ConfirmQuit()
        {
            PenguinUiSound.Click();
            UIManager.Instance.ShowConfirmDialog("終了確認", "ゲームを終了しますか？", onConfirm: Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Close(Action next)
        {
            PenguinUiSound.Click();
            gameObject.SetActive(false);
            next?.Invoke();
        }
    }
}
