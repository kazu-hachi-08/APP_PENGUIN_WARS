using UnityEngine;

namespace PenguinWars.Common.Core
{
    /// <summary>
    /// アプリケーション全体のグローバルライフサイクルおよび設定マネージャー
    /// </summary>
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        [Header("Application Settings")]
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private bool _neverSleep = true;

        protected override void Awake()
        {
            base.Awake();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // ブラウザは -1（ブラウザの描画タイミングに任せる）にする。
            // 値を指定するとフレームの間隔が不揃いになり、通信の受け取りも乱れるため（Unity の推奨も -1）
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : _targetFrameRate;

            // スリープ抑制（スマホ実機向け）
            if (_neverSleep)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }
    }
}
