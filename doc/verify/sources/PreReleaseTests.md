# ProxyDialogAutoFiller リリース前検証手順

## 検証環境の用意

* Windows 11
* Google Chrome、Microsoft Edgeをインストール済みである。
* Squidでローカルのプロキシを設定済みである

準備は以下の手順で行う。

1. RepostConfirmationCancelerの最新のインストーラ `RepostConfirmationCancelerSetup.exe` をダウンロードし、実行、インストールする。
2. ユーザー認証が必要なプロキシサーバーを作成するため、[Squid](https://squid.diladele.com/)のWindows版をダウンロード、インストールする
3. [doc\verify\sources\TestTools/.passwd](../TestTools/.passwd)を`C:\Squid`に配置する
4. [doc\verify\sources\TestTools/squid-configure.conf](../TestTools/squid-configure.conf)を`C:\Squid\etc\squid\squid.conf`にこの名前で配置（上書き）する。
5. Windows版の現在の最新のSquid 4.14ではBASIC認証に必要な`cygcrypto-2.dll`が含まれていないため、Cygwinからダウンロードして配置する。
  5-1. [Cygwin](https://www.cygwin.com/install.html)のインストーラーをダウンロードする
  5.2. Select Packages画面でlibcrypt2を選択してインストールする
  5.3. インストールが完了したら`C:\cygwin64\bin\cygcrypt-2.dll`を`C:\Squid\lib\squid`にコピーする
6. SquidのトレイからSquidを再起動する
7. Wubdiwsの「設定」->「ネットワークとインターネット」->「プロキシ」->「手動プロキシセットアップ」のセットアップを開く
8. 「プロキシサーバーを使う」をONにし、プロキシIPアドレスに「http://localhost」、ポートに「3128」を指定する。
9. 端末を再起動する（スタートアッププログラムにより`RepostConfirmationCanceler.exe`を起動させるため）

## 検証

### 設定ファイルのプロキシのユーザー名/パスワードが一つで、内容が正しい場合

#### 準備

以下の通り設定して検証を行う。

* [doc\verify\sources\TestTools/Scenarios/scenario1.ini](../TestTools/Scenarios/scenario1.ini) を `C:\Program Files\ProxyDialogAutoFiller\ProxySetting.ini` に配置する。
* ログオンユーザーのサインアウト/サインインを行う

#### 検証

* Edgeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、サインインボタンが押されること
  * [ ] プロキシ認証に成功し、サイトにアクセスできること
* Chromeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、ログインボタンが押されること
  * [ ] プロキシ認証に成功し、サイトにアクセスできること

### 設定ファイルのプロキシのユーザー名/パスワードが複数で、そのうち一つが正しい場合

#### 準備

以下の通り設定して検証を行う。

* [doc\verify\sources\TestTools/Scenarios/scenario2.ini](../TestTools/Scenarios/scenario2.ini) を `C:\Program Files\ProxyDialogAutoFiller\ProxySetting.ini` に配置する。
* ログオンユーザーのサインアウト/サインインを行う

#### 検証

* Edgeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、サインインボタンが押されること
  * [ ] プロキシ認証に成功し、サイトにアクセスできること
* Chromeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、ログインボタンが押されること
  * [ ] プロキシ認証に成功し、サイトにアクセスできること

### 設定ファイルに対象のプロキシがない場合

#### 準備

以下の通り設定して検証を行う。

* [doc\verify\sources\TestTools/Scenarios/scenario3.ini](../TestTools/Scenarios/scenario3.ini) を `C:\Program Files\ProxyDialogAutoFiller\ProxySetting.ini` に配置する。
* ログオンユーザーのサインアウト/サインインを行う

#### 検証

* Edgeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力されず、サインインボタンも押されないこと
* Chromeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、サインインボタンも押されないこと

### 設定ファイルに対象のプロキシが存在するが、ユーザー名/パスワードが間違えている場合

#### 準備

以下の通り設定して検証を行う。

* [doc\verify\sources\TestTools/Scenarios/scenario4.ini](../TestTools/Scenarios/scenario4.ini) を `C:\Program Files\ProxyDialogAutoFiller\ProxySetting.ini` に配置する。
* ログオンユーザーのサインアウト/サインインを行う

#### 検証

* Edgeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、サインインボタンが押されること
  * [ ] 認証に失敗し、再度プロキシのユーザー認証ダイアログが表示されること
* 15秒程度待つ
  * [ ] 再度プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、サインインボタンが押されること
  * [ ] 認証に失敗し、再度プロキシのユーザー認証ダイアログが表示されること
* Chromeを起動する
* 任意のサイトにアクセスする
  * [ ] プロキシのユーザー認証ダイアログが表示されること
  * [ ] プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、ログインボタンが押されること
  * [ ] 認証に失敗し、再度プロキシのユーザー認証ダイアログが表示されること
* 15秒程度待つ
  * [ ] 再度プロキシのユーザー認証ダイアログに、ユーザー名とパスワードが自動で入力され、ログインボタンが押されること
  * [ ] 認証に失敗し、再度プロキシのユーザー認証ダイアログが表示されること
