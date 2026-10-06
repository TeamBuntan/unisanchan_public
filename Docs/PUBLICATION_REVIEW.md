# 公開版の範囲と確認結果

確認日：2026-10-06  
確認対象：提供された `unisanchan(7).zip` 内のunisanchanフォルダ。
元リポジトリ全体、U-tanフォルダ、Packages、ProjectSettingsは今回の確認対象に含まれていません。

## 構成の変更

- Runtime73ファイル、Editor14ファイル、既存テスト1ファイルのC#内容を変更せず収録。
- Runtime・Editorの元の `.meta` を維持。
- NUnitを使う既存テストは `Tests/Editor` へ移し、通常のAssets導入から分離。
- READMEを現在の名称と収録範囲に合わせて書き直し、構成・導入準備の資料を追加。
- 外部素材を使わないExcel・JSONの記述例を新規作成。
- Gitの履歴と接続先情報は収録しない。

## 元のZIPから除いたもの

| 対象 | 確認内容・理由 |
|---|---|
| `Scenes` | MasterSceneには172種類、TitleSceneには9種類のZIP外GUID参照。Unity標準・パッケージ由来の参照も含み得るため、すべてを第三者アセットと断定していない。参照先を特定できない状態で公開用シーンとして収録しない |
| `Data` | Audio・Voice・Image・Preset・Scenarioの登録データにZIP外参照。VoiceRegistryだけで392種類。素材名・パスを含む登録済みデータは収録しない |
| `Prefab` | ChoiceItemにZIP外参照あり。外部依存のないPrefabも含め、今回はソース紹介に範囲を揃える |
| `Sprite` | 画像・CLIPの出所と公開条件をこのZIPから確認できない |
| 元の `Docs` | 過去の画像資料。公開用説明は現在の収録コードに合わせて新規作成 |
| 元の `LICENSE` | Unity-chanのREADME2点で、本体のライセンス指定ではなかった。対象素材を同梱しないこの版には収録しない |
| 空のUIフォルダ・未対応のmeta | ソース版で不要な空フォルダと孤立したmetaを収録しない |

## 依存関係の確認

収録コードのusing・型参照・パス参照を確認した範囲では、宴の型やNewtonsoft.Json、Unity Toon Shaderへの直接依存は見つからなかった。
RuntimeはUnityのAPI、uGUI、TextMeshPro、標準.NET APIを利用する。
EditorはUnityEditorと標準.NET APIを利用する。既存テストはNUnitを利用する。
`BackToADV` の警告文にu-tanのSceneFlowController名があるが、型への直接参照ではなくイベント通知を利用している。

## 確認したことと未確認のこと

確認済み：C#全88ファイルのバイト一致、元metaの一致、ZIP内容、参照状況、サンプルXLSXとJSONの整合性、同梱拡張子の確認。
未確認：Unityでのコンパイル、Editorの設定画面、Play Modeの実行、ビルド、同梱していない素材・アダプターの権利条件。
公開用のライセンスは未設定。ライセンス本文を自動で選択・追加していない。

公開用デモを追加する際は、利用できる素材でシーンを組み、コンパイル・実行確認を行ってからREADMEの状態を更新する。
