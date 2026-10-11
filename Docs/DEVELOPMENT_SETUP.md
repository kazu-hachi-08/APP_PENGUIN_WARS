# 開発環境・プロジェクトセットアップ手順書

2人開発で Unity 環境をそろえるための手順をまとめる。

---

## 1. Unityバージョン

- **バージョン**: `Unity 6000.6.2f1`（`ProjectSettings/ProjectVersion.txt` と同じものを使う）
- レンダーパイプライン: URP（2D）

バージョンが違うと `ProjectSettings/` や `Packages/packages-lock.json` に差分が出るので、Unity Hub で同じバージョンを入れる。

---

## 2. 必要なUnityモジュール

Unity Hub の「Installs」タブから、対象バージョンに以下のモジュールを追加する。

- **Android Build Support**（Android SDK & NDK Tools / OpenJDK）
- **Web Build Support**（ブラウザ版）
- **Windows Build Support (IL2CPP)**（Windows版を IL2CPP で出す場合。Mono なら不要）

※ iOS (iPhone) 向けビルドは現時点では対象外。

---

## 3. 初回のセットアップ

1. リポジトリを clone し、Unity Hub の「Add」から開く（初回は `Library/` の生成に時間がかかる）
2. `Assets/_Project/Games/100_PenguinWars/Scenes/PenguinWarsScene.unity` を開いて再生する
3. オンライン対戦を試す場合は、Project Settings > Services で Unity Cloud のプロジェクトにリンクする（Relay と匿名サインインを使う）

シーンやキャラのデータ・ドット絵はコードから生成している。手で編集せず、変えたら `Tools > MiniGame > Rebuild PenguinWars` で作り直す（詳しくは [PENGUIN_WARS_SPEC.md](PENGUIN_WARS_SPEC.md) §11.3）。

---

## 4. ディレクトリ構成

```text
Assets/
├── _Project/
│   ├── Common/                  # ミニゲーム集から持ち込んだ共通基盤（MiniGame.* 名前空間）
│   │   ├── Scripts/             # Core / Audio / Input / Online / Profile / Scene / UI
│   │   ├── Editor/              # 共通ダイアログのシーン生成・デバッグメニュー
│   │   ├── Plugins/WebGL/       # ブラウザ版の名前入力・全画面（.jslib）
│   │   └── Resources/           # フォント等
│   │
│   └── Games/
│       └── 100_PenguinWars/     # ペンギン大戦争本体
│           ├── Scripts/         # Battle（純C#）/ Art / Data / Game / Online / UI / View
│           ├── Editor/          # SceneBuilder・データ／ドット絵の生成・ステージ検証
│           ├── Data/ Sprites/ Audio/ Scenes/
│           └── Tests/Editor/    # Battle の EditMode テスト
│
├── Settings/Build Profiles/     # Android / Web / Windows
└── WebGLTemplates/PenguinWars/  # ブラウザ版の index.html
Docs/                            # 仕様書・開発手順
Tools/Release/                   # Windows版のリリーススクリプト
```

フォルダ名・名前空間はミニゲーム集から切り出したときのまま（Editor スクリプトに `Assets/_Project/Games/100_PenguinWars` の直書きパスがあるため、動かすときはまとめて直す）。

---

## 5. テスト

Window > General > Test Runner の EditMode で `MiniGame.PenguinWars.Battle.Tests` を実行する。戦闘ロジック（`Scripts/Battle/`）を変えたら通しておく。

ステージの難しさは `Tools > MiniGame > PenguinWars > Simulate Stages` で bot に全ステージを遊ばせて確認できる（仕様書 §8.9）。

---

## 6. Git・ファイル管理ルール

- **`.gitignore`**: プロジェクトルートに配置済み。`Library/`, `Temp/`, `Logs/`, `Builds/` などは除外される。
- **`.meta` ファイル**: アセットを追加・変更したときに生成される `.meta` は**必ずセットでコミット**する（GUID が変わるとシーン・アセットの参照が切れる）。
- **コンフリクト防止**: `PenguinWarsScene.unity` は生成物なので、2人で同時に Rebuild した場合はどちらかに寄せてから作り直す。
