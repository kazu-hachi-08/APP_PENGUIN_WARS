# ペンギン大戦争

にゃんこ大戦争風の横スクロール型タワーディフェンス。ペンギンを出撃させて敵の大群を押し返し、敵の城を落とす。

* **ステージ（1人）**: 3章18ステージ。クリアで仲間が増え、★3つを集める
* **オンライン対戦（2人）**: 部屋コードで友達と1対1。ドラフトで編成し、自分で作った「じぶんペンギン」も出せる

遊び方はゲーム内の「あそびかた」、細かいルールは仕様書を参照。

## ▶ ブラウザで遊ぶ

**https://kazu-hachi-08.github.io/penguin-wars-web/**

* PCのブラウザ（Chrome / Edge 推奨）で開く。横画面のゲームなので、スマホは横向きで
* 公開手順: Unityで Web 向けにビルドし、出力（`index.html` / `Build/` / `StreamingAssets/`）を [penguin-wars-web](https://github.com/kazu-hachi-08/penguin-wars-web) の main に push する（GitHub Pages で公開される）

## ▶ Windows版をダウンロード

**https://github.com/kazu-hachi-08/APP_PENGUIN_WARS/releases/latest**

* zipを展開して exe を起動する
* 初回に「WindowsによってPCが保護されました」と出たら「詳細情報 → 実行」で起動できる
* `test-` で始まるものはテスト版（Pre-release）
* 公開手順: `Tools/Release/release-windows.ps1` の先頭コメントを参照

---

## ドキュメント

| ドキュメント | 内容 |
| ---- | ---- |
| [Docs/PENGUIN_WARS_SPEC.md](Docs/PENGUIN_WARS_SPEC.md) | 仕様書（ルール・キャラ・ステージ・オンライン対戦・コード構成） |
| [Docs/DEVELOPMENT_SETUP.md](Docs/DEVELOPMENT_SETUP.md) | 開発環境のセットアップ・テスト |
| [CLAUDE.md](CLAUDE.md) | AI 開発ガイドライン |

---

## 技術スタック

| 項目 | 内容 |
| ---- | ---- |
| ゲームエンジン | Unity 6000.6.2f1（URP 2D） |
| 言語 | C# |
| グラフィック | 2Dドット絵（コードで生成） |
| オンライン | Unity Relay + Netcode for GameObjects |
| 対象プラットフォーム | Android / Windows / ブラウザ（WebGL）。横画面固定 |
| ソース管理 | GitHub |
| 開発人数 | 2人 |

## ディレクトリ構成

```text
Assets/_Project/
├── Common/                  # 共通基盤（Scene / Input / UI / Audio / Online など）
└── Games/100_PenguinWars/   # ペンギン大戦争本体（Scripts / Editor / Data / Sprites / Tests）
Docs/                        # 仕様書・開発手順
Tools/Release/               # Windows版のリリーススクリプト
```

* フォルダ名 `Games/100_PenguinWars` は変えない（Editor スクリプトに直書きパスがあるため）
* 戦闘ロジック（`Scripts/Battle/`）は Unity に依存しない純C#（`PenguinWars.Battle.asmdef`）
* シーン・キャラのデータ・ドット絵はコードから生成する（`Tools > PenguinWars > Rebuild PenguinWars`）

詳しくは [Docs/DEVELOPMENT_SETUP.md](Docs/DEVELOPMENT_SETUP.md) を参照。

---

## 開発ルール

### 基本方針

1. 小さく作る
2. 早く遊べる状態にする
3. 毎週末 Playable Build を残す
4. 必要以上に仕様を増やさない

**Playable Build** = 完成版ではなく、その時点の機能で実際に操作・プレイできる状態。グラフィックや演出が未完成でもよい。

実装の優先順位：

```text
必須機能 → ゲームとして成立する機能 → 操作性 → 演出 → 追加要素
```

### Git運用

```text
main
├── feature/#xx   # Issue番号でブランチを切る
└── fix/xxx
```

featureブランチで実装 → 動作確認 → Pull Request → レビュー → main へ Merge

### Unity開発時の注意

* `PenguinWarsScene.unity` は生成物なので手で編集しない。変えたいときは SceneBuilder を直して Rebuild する
* キャラの数値は定義表（`UnitDefinitions`）で調整する（アセットを Inspector で直しても Rebuild で戻る）
* アセットを追加したら `.meta` も必ず一緒にコミットする
