# PdfToolbox

PDFに対する5つの基本操作(回転・中央で切る・分割・結合・PNG変換)をオフラインで行える、Windows用のシンプルなデスクトップツールです。インターネット接続やクラウドサービスを一切使わず、ローカルだけで完結します。

## 機能一覧

- **回転**: 指定したページ範囲を90度単位で回転します。
- **中央で切る**: 見開きページなどを中央で左右2ページに分割します。
- **分割**: 指定した複数のページ範囲ごとにPDFファイルを分けて保存します。
- **結合**: 開いているPDFの末尾に、別のPDFファイル(複数選択可)を追加して1つのPDFにまとめます。
- **PNG変換**: 指定したページ範囲をPNG画像として書き出します。

## 使い方

1. 「PDFを開く」で対象のPDFファイルを開きます(ドラッグ&ドロップでも開けます)。
2. 左側のメニューから行いたい機能(回転/中央で切る/分割/結合/PNG変換)を選びます。
3. 各機能ごとの設定(ページ範囲や回転角度など)を入力します。
4. 実行ボタンを押して処理を行います。
5. 必要に応じて「保存」でPDFファイルとして保存します。

処理を実行してもファイルを開き直す必要はなく、続けて別の操作を行ったり、プレビューを確認しながら繰り返し編集できます。

### 結合について

「結合」機能だけは他の4機能と操作が異なります。専用のUIで追加したいPDFファイルを複数選択し、現在開いているPDFの末尾に順番に結合します。

## 動作環境

- Windows 10 / 11 (win-x64)
- Microsoft .NET 10 Desktop Runtime (x64) が別途必要です。
- .NET 10 Desktop Runtimeは、[公式ダウンロードページ](https://dotnet.microsoft.com/download/dotnet/10.0)から「Windows Desktop Runtime」のx64版をインストールしてください。
- 配布用EXEには.NET Runtimeを同梱しません。Runtimeが見つからない場合は、.NETの実行環境からインストールを要求されます。

## ビルド方法

```
dotnet build
```

## Publish方法(ランタイム別途要求の単一exeを作成)

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

`publish` フォルダに、.NET 10 Desktop Runtimeを含まない `PdfToolbox.exe` が生成されます。実行するPCには、あらかじめ.NET 10 Desktop Runtime (x64)をインストールしてください。

## ユーザー単位インストーラー

`Build-PdfToolbox.ps1` は `win-x64` の framework-dependent publish を作成し、Inno Setupで管理者権限を要求しないユーザー単位インストーラーを生成します。インストール先は `%LOCALAPPDATA%\Programs\PdfToolbox`、スタートメニューには通常起動用ショートカットだけを作成します。ログイン時の自動起動は登録しません。

```
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build-PdfToolbox.ps1
```

インストーラーは「インストールされているアプリ」に登録され、そこからアンインストールできます。インストール前に .NET 10 Desktop Runtime (x64) の存在を確認し、Runtimeは同梱しません。

## 既知の制約

PDF処理のコアロジック(回転/中央で切る/分割/結合/PNG変換/保存)はE2Eテストで動作確認済みです。一方で、ドラッグ&ドロップやファイル選択ダイアログなどのGUI操作については、別途手動での確認を進めている段階です。
