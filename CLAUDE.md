# ペンギン大戦争 AI開発ガイドライン

## 1. プロジェクトの基本方針（最重要）
このプロジェクトは「週末の限られた時間で2人で開発する、横スクロール型タワーディフェンス『ペンギン大戦争』」です。
以下の哲学を常に優先して提案やコード生成を行ってください。

- **小さく作る (Keep It Simple):** 過剰な設計（オーバーエンジニアリング）や将来のための複雑な拡張性は不要です。常に最もシンプルで直接的な解決策を提案してください。
- **Playable Build優先:** 「とりあえず遊べる状態」にすることを最優先します。未完成の複雑な機能よりも、動く最小限の機能を提示してください。
- **スコープ制御:** 機能要件を勝手に膨らませないでください。ユーザーが提示した要件を満たす最小のコードを提供してください。
- **仕様書が正:** ルール・数値・画面の流れは `Docs/PENGUIN_WARS_SPEC.md` に書いてあります。挙動を変えたら仕様書も合わせて更新してください。

## 2. 技術スタック & アーキテクチャ
- **エンジン / 言語:** Unity 6000.6.2f1 / C#
- **プラットフォーム:** Android / Windows / ブラウザ（WebGL）。**横画面固定**（縦画面のレイアウトは考えなくてよい）
- **オンライン:** Unity Relay + Netcode for GameObjects（ホストが戦闘を計算し、ゲストへ状態とイベントを送る）
- **アーキテクチャの境界:**
  - `Assets/_Project/Common/`: 共通基盤（Scene, Input, UI, Audio, Online, Profile など）。ゲーム固有の処理はここに書かないこと。
  - `Assets/_Project/Games/100_PenguinWars/`: ペンギン大戦争本体。
  - `Scripts/Battle/`（`PenguinWars.Battle.asmdef`）: **Unity に依存しない純C#**の戦闘ロジック（`noEngineReferences: true`）。`UnityEngine` や ScriptableObject を参照しないこと。乱数は `System.Random`。表示・音・通信は `BattleEvent` を介して外側（View / Game）で行う。
- **フォルダ名・名前空間:** 名前空間は `PenguinWars.*`（共通基盤は `PenguinWars.Common.*`）。フォルダ名 `Games/100_PenguinWars` は勝手にリネームしないこと（Editor スクリプトに直書きパスがある）。

## 3. Unity & C# 実装ガイドライン
- **シーンはコードで生成:** `PenguinWarsScene.unity` は `PenguinWarsSceneBuilder` で生成する（`Tools > PenguinWars > Rebuild PenguinWars`）。UI やオブジェクトを足すときはシーンを手で編集せず、SceneBuilder に追加してください。
- **データも生成:** キャラの数値は `UnitDefinitions` の定義表、ドット絵は `Scripts/Art/` の合成で作ります。アセットを直接編集する提案はしないでください（Rebuild で戻る）。
- **テスト:** `Scripts/Battle/` を変えたら `Tests/Editor/`（EditMode）のテストを追加・更新してください。
- **コンポーネント指向:** 1つのMonoBehaviourに全てを書くのではなく、役割（入力、移動、描画、状態管理）ごとに小さく分割したコンポーネントを提案してください。大きなクラスは `partial`（例: `PenguinWarsGameManager.Stage`）で分けています。
- **コードスタイル:**
  - メソッドは短く、単一の責任を持たせること。
  - マジックナンバーは避け、`[SerializeField]` や定数（調整値は `PenguinWarsBalance`）として定義すること。
  - コメントは「何をしているか（What）」よりも「なぜそうしているか（Why）」を中心に記述すること。

## 4. AIへの指示（回答のスタンス）
- 質問に対する回答は、挨拶や冗長な説明を省き、すぐに要点に入ってください。
- 実装を提案する際は、「ステップバイステップの具体的な手順」と「そのままコピーできる最小限のC#コード」をセットで提供してください。
- 複数のアプローチがある場合は、「この週末で実装し切るのに最も確実で簡単な方法」を第一候補として推奨してください。
