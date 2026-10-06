# unisanchan（ユニさんちゃん）

Excelでシナリオと演出を記述し、Unityで実行するADV・3Dイベントシステムです。
シナリオ制作と演出実装をつなぐために、企画・仕様設計・Unity/C#実装を進めている個人開発プロジェクトです。

**開発途中のソース公開版です。** 現時点では設計と実装を紹介するための構成で、起動済みのデモシーンや完成したUnityプロジェクト一式は含んでいません。

## 制作の目的

セリフ、選択肢、条件分岐だけでなく、カメラ・キャラクター・音・画面演出を同じ表で記述できる制作環境を目指しています。
Excelの記述からJSONへの変換、シナリオ実行、Unity上の各演出処理を分担する構成です。

## 実装している主な機能

| 領域 | 内容 | 主なソース |
|---|---|---|
| データ変換 | XLSX→JSON、Excel更新時の変換 | `Editor/Importers/Excel` |
| シナリオ進行 | Event・Block・Triggerの構造化、複数JSONの読み込み、Jump | `Runtime/Core`、`Runtime/Data` |
| 演出の進行制御 | 完了待ち、入力待ち、非同期演出の完了管理 | `CommandFlowResolver`、`BlockCompletionTracker` |
| キャラクター | 表示・非表示、移動、アニメーション、表情 | `ActorCommandHandler`、`FaceController` |
| カメラ | 移動、イージング、複数カメラの生成・切替・破棄 | `CameraCommandHandler`、`Runtime/Camera` |
| UI | セリフ、選択肢、Tips、ガイド、Modal、フェード | `Runtime/UI` |
| 音・エフェクト | ボイス、BGM、環境音、SE、パーティクル | `Runtime/Commands`、`Runtime/Registry` |
| 変数 | 登録、条件評価、計算式、保存区分 | `Runtime/Variables` |
| セーブ・ロード | チェックポイント、オートセーブ、手動30スロット、開始・ロードのシーン遷移 | `Runtime/Save` |
| 制作支援 | Project Settings、シナリオ構造の検証、登録データ管理 | `Editor/Settings`、`ScenarioPlayModeValidator` |

表のパスは `Assets/unisanchan` を基準にしています。機能の列挙は収録ソースの実装に基づくもので、この公開構成での動作保証を表すものではありません。

## ソースの読み方

1. `Runtime/Data/ScenarioRow.cs` — Excelの1行に対応するデータ。
2. `Editor/Importers/Excel/XlsxToJsonConverter.cs` — Excelの読み込みとJSONの出力。
3. `Runtime/Core/ScenarioController.cs` — 登録データの読み込みと進行。
4. `Runtime/Core/ScenarioCommandExecutor.cs` — 演出を各ハンドラーへ委譲。
5. `Runtime/Core/CommandFlowResolver.cs` — コマンドごとの待機方針。
6. `Runtime/Save/ScenarioSaveCoordinator.cs` — チェックポイントと変数の保存。

[構成と設計](Docs/ARCHITECTURE.md) / [導入準備](Docs/SETUP.md) / [公開版の範囲と確認結果](Docs/PUBLICATION_REVIEW.md)

## 収録内容

- `Assets/unisanchan/Runtime`：実行時コード73ファイル。
- `Assets/unisanchan/Editor`：制作支援コード14ファイル。
- `Tests/Editor`：既存の変数・式評価テスト1ファイル。通常の導入時にはコピーしません。
- `Examples`：公開用に新規作成した記述例（XLSX・JSON）。Unityでの実行は未確認です。
- `Docs`：構成、導入準備、公開範囲の説明。

元の開発環境のシーン、Prefab、登録済みデータ、モデル、音声、画像、フォント、外部アセットは同梱していません。

## 必要環境

対象はUnity 6、uGUI、TextMeshProです。具体的なEditor・Packageのバージョンは、公開用Unityプロジェクトを整える際に確定します。
収録コードには宴（Utage）の型への直接参照はなく、本体の導入に宴は必須ではありません。
`BackToADV` は復帰要求イベントを通知する仕組みで、宴への連携アダプター（u-tan）はこの公開版に含めていません。
旧READMEに記載されていたNewtonsoft.Json・Unity Toon Shaderは、収録C#コードの直接依存としては見つかっていません。

## 開発状況

セーブ・ロードとシーン遷移を含めて実装を進めています。公開用デモシーンの整備と、素材を除いた構成でのUnityコンパイル・実行確認はこれから行います。
セーブはチェックポイント基準です。任意の瞬間の3D空間全体をそのまま復元する機能としては紹介していません。

## 制作者

カバかもん / チームぶんたん  
[Webサイト](https://buntan-works.jp)

公開ソースの利用条件は、別途ライセンスを設定する際に明記します。
