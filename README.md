# ProxyDialogAutoFiller

## 概要

Google ChromeおよびMicrosoft Edge上でプロキシーログインのダイアログが表示されたら、自動でユーザー名とパスワードを入力するWindowsネイティブアプリケーション

## ビルド方法

* Visual Studio 2022
  * .NET Framework 4.6.2 SDKをインストール
    * ProxyDialogAutoFillerがC#の.NET Framework 4.6.2で実装されているため
    * 以下のファイルを参照しているので、特に以下のファイルが存在していることを確認する
      * `C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2\UIAutomationClient.dll`
      * `C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.2\UIAutomationTypes.dll`
  * Windows SDK 10.0の最新版のインストール
* Inno Setup 6.3.3以上をインストールする

### ネイティブアプリのインストーラー作成方法

1. ProxyDialogAutoFiller.sln をVisual Studio 2022で開く
2. 構成Release、プラットフォームAnyCPUでソリューションのビルドを実行する
3. ProxyDialogAutoFiller.iss をInno Setupで開く
4. Build -> Compileからインストーラーをコンパイルする
5. SetupOutput配下にネイティブアプリのインストーラーが作成される

## 検証環境での動作確認

* 作成したネイティブアプリのインストーラをインストールする
* `C:\Program Files\ProxyDialogAutoFiller\ProxySetting.ini` のアクセス権を変更し、ユーザー権限での書き込みを許可した上で、以下のような内容を記載する
  * ```
    [プロキシのホスト名]
    UserName=ユーザー名
    Password=パスワード
    ```
  * 複数のプロキシを設定可能
    例:
    ```
    [hoge.co.jp]
    UserName=hoge
    Password=fuga
    [foo.com]
    UserName=foo
    Password=var
    ```

## 動作解説

* ProxyDialogAutoFiller.exeは、Google ChromeおよびMicrosoft Edgeのウィンドウを0.5秒ごとに監視する
* プロキシログインのダイアログが表示されたら、ログイン先のプロキシが設定ファイルに記載されているプロキシのホスト名と一致するか確認する
  * これはダイアログに表示されている、ログインしようとしているプロキシ名と、セクションに記載されているプロキシのホスト名を比較して確認している
* 自動でユーザー名とパスワードを入力し、ログインする
