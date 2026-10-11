using System;
using System.Collections.Generic;
using PenguinWars.Common.Profile;
using PenguinWars.Common.UI;
using PenguinWars.Battle;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PenguinWars
{
    /// <summary>
    /// ペンギン大戦争のタイトル画面（仕様書 §2.0）。開くとロゴが落ちてきて、ペンギンが走って並び、ボタンがせり上がる。
    /// 幕の後ろではパレードの5体が本物の戦場で戦う。
    /// 何を押したらどこへ進むかは GameManager が決める（「終了」だけはアプリを閉じるのでここで完結させる）
    /// </summary>
    public class PenguinWarsTitlePanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private RectTransform _logo;
        [SerializeField] private Image[] _paradeIcons;
        [Tooltip("★・スタート・サブボタンの入れ物。登場演出でまとめてせり上げる")]
        [SerializeField] private RectTransform _menu;
        [SerializeField] private CanvasGroup _menuGroup;
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
        [Tooltip("後ろの戦場の小競り合い係。あそびかたとは別に、タイトルに付ける（タイトルを隠すと一緒に止まるように）")]
        [SerializeField] private GuideDemoDirector _backdropDirector;
        [Tooltip("対戦で名前を表示するため、未設定ならタイトルで必ず決めさせる")]
        [SerializeField] private NameEntryDialog _nameEntryDialog;

        [Header("動き")]
        [SerializeField] private float _logoBobHeight = 12f;
        [SerializeField] private float _logoBobSpeed = 2f;
        [SerializeField] private float _hopHeight = 28f;
        [SerializeField] private float _hopSpeed = 6f;
        [Tooltip("隣のペンギンと跳ねるタイミングをずらす量。揃うと機械的に見えるため")]
        [SerializeField] private float _hopPhaseStep = 0.8f;

        [Header("登場演出（秒は開いてからの時間）")]
        [SerializeField] private float _logoDropHeight = 500f;
        [SerializeField] private float _logoDropTime = 0.6f;
        [SerializeField] private float _paradeDelay = 0.4f;
        [Tooltip("1体ずつ遅らせて、列になって走ってくるように見せる")]
        [SerializeField] private float _paradeStagger = 0.12f;
        [SerializeField] private float _paradeRunTime = 0.5f;
        [SerializeField] private float _paradeRunDistance = 1400f;
        [SerializeField] private float _menuDelay = 1f;
        [SerializeField] private float _menuRiseTime = 0.4f;
        [SerializeField] private float _menuRiseHeight = 120f;

        // EaseOutBack の行き過ぎ量。ロゴが一度沈んで戻る「着地」に見せる
        private const float LogoOvershoot = 1.70158f;

        private Action _onStart;
        private Vector2 _logoBasePosition;
        private Vector2 _menuBasePosition;
        private Vector2[] _iconBasePositions;
        private List<int> _allNos;
        private readonly List<int> _paradeNos = new List<int>();
        private float _introTime;

        private float IntroEndTime => _menuDelay + _menuRiseTime;

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
            _menuBasePosition = _menu.anchoredPosition;
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
            _introTime = 0f;
            // 次の Update を待つと、1フレームだけ登場前の最終形が見えてしまう
            Animate();
            PlayBackdrop();
            if (!UserProfile.HasName) _nameEntryDialog.Show(onDecided: null);
        }

        /// <summary>登場演出の途中で画面を押したら、すぐ最後の形にする（何度も見る画面なので待たせない）</summary>
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
            float time = Time.unscaledTime;
            float bob = Mathf.Sin(time * _logoBobSpeed) * _logoBobHeight;
            float drop = (1f - EaseOutBack(Progress(0f, _logoDropTime))) * _logoDropHeight;
            _logo.anchoredPosition = _logoBasePosition + Vector2.up * (bob + drop);

            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                // Abs で地面に着いたら跳ね返る動きにする
                float hop = Mathf.Abs(Mathf.Sin(time * _hopSpeed + i * _hopPhaseStep)) * _hopHeight;
                float run = (1f - EaseOut(Progress(_paradeDelay + i * _paradeStagger, _paradeRunTime))) * _paradeRunDistance;
                _paradeIcons[i].rectTransform.anchoredPosition = _iconBasePositions[i] + new Vector2(-run, hop);
            }

            float rise = EaseOut(Progress(_menuDelay, _menuRiseTime));
            _menu.anchoredPosition = _menuBasePosition + Vector2.down * ((1f - rise) * _menuRiseHeight);
            _menuGroup.alpha = rise;
            // 出てくる途中のボタンを押せないようにし、その間のタップは OnPointerClick（演出の早送り）に届ける
            _menuGroup.blocksRaycasts = rise >= 1f;
        }

        private float Progress(float delay, float duration) => Mathf.Clamp01((_introTime - delay) / duration);

        private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            float p = t - 1f;
            return 1f + (LogoOvershoot + 1f) * p * p * p + LogoOvershoot * p * p;
        }

        /// <summary>パレードの5体が後ろでも戦う。敵はループのたびに選び直して、見るたびに違う戦いにする</summary>
        private void PlayBackdrop()
        {
            _backdropDirector.PlayShuffled(() => TitleBattleScript.Create(_paradeNos, _allNos));
        }

        /// <summary>毎回違う顔ぶれを見せたいので、開くたびに全キャラから重複なしで選び直す</summary>
        private void PickParade()
        {
            var units = new List<PenguinUnitData>(_catalog.Units);
            // カタログの空き枠を選ぶと絵の無いペンギンが混ざるので除く
            units.RemoveAll(u => u == null);
            _allNos ??= units.ConvertAll(u => u.No);
            _paradeNos.Clear();
            for (int i = 0; i < _paradeIcons.Length; i++)
            {
                Sprite icon = null;
                if (units.Count > 0)
                {
                    int index = UnityEngine.Random.Range(0, units.Count);
                    icon = units[index].GetSprites(Side.Left).Icon;
                    _paradeNos.Add(units[index].No);
                    units.RemoveAt(index);
                }
                _paradeIcons[i].sprite = icon;
                _paradeIcons[i].enabled = icon != null;
            }
        }

        /// <summary>ステージをクリアしてタイトルに戻るたびに増えて見えるよう、開くたびにセーブから数え直す</summary>
        private void ShowStars()
        {
            _starsLabel.text = FormatStarTotal();
        }

        /// <summary>例: ★ 32 / 54。モード選択の「ステージ」カードでも同じ表記を使う</summary>
        public static string FormatStarTotal()
        {
            CampaignProgress progress = CampaignSave.Load();
            int stars = 0;
            foreach (StageDefinition stage in StageDefinitions.All) stars += StarRule.Count(progress.GetStars(stage.Id));
            return $"★ {stars} / {StageDefinitions.All.Count * StarRule.MaxStars}";
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

        /// <summary>
        /// デモの戦場が幕で暗くならないよう、タイトルごと隠してから開き、閉じたら戻す。
        /// あそびかたは閉じるときに戦場を空にするので、戻ったら後ろの小競り合いを流し直す
        /// </summary>
        private void OpenGuide()
        {
            PenguinUiSound.Click();
            gameObject.SetActive(false);
            _guidePanel.Show(() =>
            {
                gameObject.SetActive(true);
                PlayBackdrop();
            });
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

        /// <summary>小競り合いを片付けてから次へ進む（モード選択の後ろにデモのペンギンが残らないように）</summary>
        private void Close(Action next)
        {
            PenguinUiSound.Click();
            _backdropDirector.Stop();
            gameObject.SetActive(false);
            next?.Invoke();
        }
    }
}
