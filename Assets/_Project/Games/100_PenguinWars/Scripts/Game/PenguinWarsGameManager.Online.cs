using System.Collections;
using PenguinWars.Common.Audio;
using PenguinWars.Common.Core;
using PenguinWars.Common.Online;
using PenguinWars.Common.Profile;
using PenguinWars.Common.UI;
using PenguinWars.Battle;
using UnityEngine;

namespace PenguinWars
{
    /// <summary>
    /// オンライン対戦（仕様書 §2.2・§10）の接続後の流れと決着。
    /// ホスト: ゲストの準備完了 → ドラフト（.Draft.cs）→ じぶんペンギン選択（.CustomPick.cs）→ 編成を送る → 編成確認。
    /// ゲスト: ドラフト → じぶんペンギン選択 → 編成が届く → 編成確認。
    /// 決着（城崩壊・時間切れ）はホストの BattleWorld で決まり、ゲストにもイベントとして届くので、両者とも同じ HandleBattleEvent で終わる
    /// </summary>
    public partial class PenguinWarsGameManager
    {
        private enum MatchMode
        {
            Stage,
            Host,
            Guest,
        }

        private const string TimeUpMessage = "TIME UP!";
        private const string WinTitle = "VICTORY!";
        private const string LoseTitle = "GAME OVER";
        private const string DrawTitle = "DRAW";
        private const string WinDetail = "勝利！";
        private const string LoseDetail = "敗北...";
        private const string DrawDetail = "引き分け";
        private const string DisconnectedDetail = "相手が切断しました";
        private const int HostSeat = 0;
        private const int GuestSeat = 1;
        private const float PercentScale = 100f;

        [Header("オンライン対戦")]
        [Tooltip("未設定ならモード選択を出さずステージ選択から始める")]
        [SerializeField] private ModeSelectPanel _modeSelectPanel;
        [SerializeField] private OnlineSession _onlineSession;
        [SerializeField] private PenguinWarsOnlineLink _onlineLink;
        [Tooltip("お互いの編成を見せる時間（仕様書 §2.2）")]
        [SerializeField] private float _versusDeckIntroDuration = 3f;
        [Tooltip("TIME UP! を見せてからリザルトを出すまでの秒数")]
        [SerializeField] private float _timeUpDuration = 1.5f;
        [SerializeField] private VersusResultPanel _versusResultPanel;

        private MatchMode _mode = MatchMode.Stage;
        // オンラインでは相手の端末が止まらないので、ポーズ画面を出しても試合は止めない
        private bool _isOnlinePauseOpen;

        private bool IsOnline => _mode != MatchMode.Stage;
        private int MySeat => _mode == MatchMode.Guest ? GuestSeat : HostSeat;
        private int OpponentSeat => _mode == MatchMode.Guest ? HostSeat : GuestSeat;

        private void SubscribeOnline()
        {
            if (_onlineSession != null) _onlineSession.OnPeerDisconnected += HandlePeerDisconnected;
            if (_onlineLink == null) return;

            _onlineLink.GuestReady += HandleGuestReady;
            _onlineLink.DecksReceived += HandleDecksReceived;
        }

        private void UnsubscribeOnline()
        {
            if (_onlineSession != null) _onlineSession.OnPeerDisconnected -= HandlePeerDisconnected;
            if (_onlineLink == null) return;

            _onlineLink.GuestReady -= HandleGuestReady;
            _onlineLink.DecksReceived -= HandleDecksReceived;
        }

        /// <summary>相手と接続できた。ドラフトはホストが仕切るので、ゲストは最初の候補が届くまで待つ</summary>
        private void StartOnline(bool isHost)
        {
            _mode = isHost ? MatchMode.Host : MatchMode.Guest;
            _onlineLink.Begin(isHost);
        }

        /// <summary>ホストのみ。ゲストが受け取れるようになってからドラフトを始める（先に送ると候補が捨てられるため）</summary>
        private void HandleGuestReady()
        {
            if (_mode != MatchMode.Host || Phase != PenguinWarsPhase.ModeSelect) return;

            BeginDraftAsHost();
        }

        /// <summary>
        /// ゲストのみ。ドラフトが終わってホストから届いた編成を受け取り、自分（ホストの右）を左に入れ替えて持つ。
        /// 最終ラウンドの候補より先に届いても困らないよう、ドラフト前（ModeSelect）でも受け付ける
        /// </summary>
        private void HandleDecksReceived(int[] hostLeftDeck, int[] hostRightDeck, int stageIndex, string hostCustomJson,
            string guestCustomJson)
        {
            if (_mode != MatchMode.Guest) return;
            if (Phase != PenguinWarsPhase.ModeSelect && Phase != PenguinWarsPhase.Draft) return;

            _isCustomPicking = false;
            HideDraftPanels();
            // 編成にじぶんペンギンの No が入っているので、World を作る前にカタログへ登録しておく
            RegisterCustomUnitsAsGuest(hostCustomJson, guestCustomJson);
            _battleRunner.InitializeGuest(hostRightDeck, hostLeftDeck, stageIndex, _onlineLink);
            BeginIntro(_versusDeckIntroDuration);
        }

        /// <summary>時間切れは崩れる演出がないので、TIME UP! を少し見せてからリザルトへ</summary>
        private void BeginTimeUp(Side loser, bool isDraw)
        {
            BeginFinish();
            _hud.ShowMessage(TimeUpMessage);
            StartCoroutine(EndAfterTimeUp(loser, isDraw));
        }

        private IEnumerator EndAfterTimeUp(Side loser, bool isDraw)
        {
            yield return new WaitForSeconds(_timeUpDuration);
            _hud.HideMessage();
            EndVersus(loser, isDraw);
        }

        /// <summary>ゲストも左右反転した向きで受け取っているので、どちらの端末でも「自分 = Left」で勝敗を判定できる</summary>
        private void EndVersus(Side loser, bool isDraw)
        {
            if (isDraw) ShowVersusResult(VersusOutcome.Draw, DrawTitle, DrawDetail);
            else if (loser == Side.Right) ShowVersusResult(VersusOutcome.Win, WinTitle, WinDetail);
            else ShowVersusResult(VersusOutcome.Lose, LoseTitle, LoseDetail);
        }

        /// <summary>
        /// BaseGameManager.FinishGame と同じ順で状態を進め、表示だけ編成つきの専用リザルトにする（仕様書 §2.3）。
        /// 音も FinishGame と同じ（勝ちは GameClear・それ以外は GameOver）
        /// </summary>
        private void ShowVersusResult(VersusOutcome outcome, string title, string detail)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(outcome == VersusOutcome.Win ? SeId.GameClear : SeId.GameOver);
            EnterResultState(outcome == VersusOutcome.Win);
            _versusResultPanel.Show(BuildVersusContent(outcome, title, detail), RestartGame, ReturnToTitle);
        }

        /// <summary>城の残りHP割合は時間切れの判定と同じもの。ドラフト中の切断では World が無いので 0% と空の編成になる</summary>
        private VersusResultContent BuildVersusContent(VersusOutcome outcome, string title, string detail)
        {
            var content = new VersusResultContent
            {
                Outcome = outcome,
                Title = title,
                Detail = detail,
                MyName = SeatNames.Get(MySeat),
                OpponentName = SeatNames.Get(OpponentSeat),
            };
            BattleWorld world = _battleRunner.World;
            if (world == null) return content;

            content.MyPercent = CastlePercent(world.GetCastle(Side.Left));
            content.OpponentPercent = CastlePercent(world.GetCastle(Side.Right));
            content.MyDeck = world.GetDeck(Side.Left);
            content.OpponentDeck = world.GetDeck(Side.Right);
            return content;
        }

        /// <summary>切り上げにして、少しでも残っていれば 0% と出さない（落ちた城と見分けがつくように）</summary>
        private static int CastlePercent(CastleState castle)
        {
            return Mathf.CeilToInt(castle.HpRatio * PercentScale);
        }

        /// <summary>
        /// 試合中に相手が抜けたら勝ち扱い（仕様書 §10.4）。
        /// 既に決着して崩れる演出・TIME UP を見せている間なら、そのまま本来の結果を出す
        /// </summary>
        private void HandlePeerDisconnected()
        {
            if (!IsOnline || Phase == PenguinWarsPhase.Finished || CurrentState == GameState.Result) return;

            Phase = PenguinWarsPhase.Finished;
            if (_battleRunner.World != null) _battleRunner.SetRunning(false);
            _deckIntroPanel.Hide();
            HideDraftPanels();
            _hud.HideMessage();
            _audio.StopBgm();
            ShowVersusResult(VersusOutcome.Win, WinTitle, DisconnectedDetail);
        }

        protected override void OnGameOver(bool isVictory)
        {
            if (!IsOnline) return;

            _onlineLink.Stop();
            CloseOnlinePause();
        }

        // ---- ポーズ ----

        public override void PauseGame()
        {
            if (!IsOnline)
            {
                base.PauseGame();
                return;
            }

            // ポーズキーをもう一度押したら閉じる（オンラインでは状態が Paused にならず ResumeGame が呼ばれないため）
            if (_isOnlinePauseOpen)
            {
                CloseOnlinePause();
                return;
            }
            if (!UIManager.HasInstance) return;

            _isOnlinePauseOpen = true;
            UIManager.Instance.ShowPauseDialog(onResume: CloseOnlinePause, onRestart: RestartGame, onTitle: ReturnToTitle);
        }

        private void CloseOnlinePause()
        {
            if (!_isOnlinePauseOpen) return;

            _isOnlinePauseOpen = false;
            if (UIManager.HasInstance) UIManager.Instance.HidePauseDialog();
        }
    }
}
