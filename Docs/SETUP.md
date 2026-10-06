# 導入準備

この配布物はソース公開版です。以下はコードから確認した導入準備で、配布構成でのUnity起動・コンパイル・実行は未確認です。

## コードを取り込む

1. Unity 6の作業用プロジェクトを用意します。
2. uGUI・TextMeshProを利用できる状態にします。TMP文字表示に必要なリソースと日本語対応フォントは利用者側で用意します。
3. `Assets/unisanchan` を `.meta` ファイルごとコピーします。Editorの設定画面には固定パス参照があるため、この配置名を維持してください。
4. Consoleでコンパイル結果を確認します。
5. 内蔵キー入力は `UnityEngine.Input.GetKeyDown` を使用します。利用時はPlayer SettingsのActive Input Handlingで旧Input Managerを有効にしてください。

`Tests/Editor` は閲覧用にAssetsの外へ配置しています。実行する場合はUnity Test Frameworkとテスト用アセンブリの設定、対象のRuntimeコードのアセンブリ構成を確認する必要があります。この版ではその設定を追加していません。

## 登録データとシーンを用意する

元の登録済みデータは含めていないため、必要なScriptableObjectを新しく作成します。
`Assets/unisanchan/Data` に `ScenarioRegistry`、`PresetRegistry`、`VariableRegistry` などを配置し、シーン内の `ScenarioController` に割り当てます。
`PresetRegistry` と `VariableRegistry` は、プリセットや変数を使わない場合でも `ScenarioController` の起動時に参照を要求します。

セリフ表示にはCanvas、ダイアログ用GameObject、TMP_Text、必要に応じて話者名表示を用意し、対応するSerializeFieldを設定します。
入力には `ScenarioInputController` を接続し、Advanceなどの操作にキーを割り当てます。
選択肢、カメラ、音、パーティクル、Modal、セーブ画面は使う機能に応じて参照・登録データを追加します。
これは導入の準備項目であり、完成したシーンの手順書ではありません。

## Excelを読み込む

`Examples/sample.xlsx` は入力書式の新規記述例です。シーンを組んでから使用します。

- シナリオは最初のワークシートを読み込む実装です。サンプルでは `Event` を先頭にしています。
- `EventSystemSettings` のScenario Excel FilesにXLSXを登録します。
- `Tools > Excel Event System > Convert Xlsx To Json` でJSONへ変換します。
- XLSXのインポート時にも変換が予約されます。Play Mode中は自動変換しません。
- 出力されたJSONを `ScenarioRegistry` に登録し、ScenarioControllerの参照を確認します。
- `Examples/sample.json` は書式比較用のJSONです。C#変換器を実行して生成したものではありません。

サンプルは3行のDialogのみを含み、モデル・音声・画像を参照しません。実行確認済みデモではありません。

## 参考

- [Unity 6：ランタイムUIイベントシステムと入力処理](https://docs.unity3d.com/ja/6000.0/Manual/UIE-Runtime-Event-System.html)
- [Unity：テスト用アセンブリの作成](https://docs.unity3d.com/ja/current/Manual/assembly-definitions-creating.html)
