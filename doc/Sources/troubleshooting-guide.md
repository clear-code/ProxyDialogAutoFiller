---
CJKmainfont: Noto Sans CJK JP
CJKoptions:
  - BoldFont=Noto Sans CJK JP Bold
titlepage-logo: ./troubleshooting-guide/media/image1.png
title: |
  ProxyDialogAutoFiller
  トラブルシューティングガイド
subject: ProxyDialogAutoFillerトラブルシューティングガイド
date: 2025/11/14
author: 株式会社クリアコード
keywords: [ProxyDialogAutoFiller, Troubleshooting guide]
titlepage: true
toc-title: 目次
toc-own-page: true
---

更新履歴

| 日付       | Version | 備考                              |
|------------|---------|-----------------------------------|
| 2025/11/14 | 1.0.0    | 第1版                             |

**本書について**

本書は、株式会社クリアコードが、ProxyDialogAutoFillerを御利用いただく管理者向けに作成した資料となります。2025年9月時点のデータにより作成されており、それ以降の状況の変動によっては、本書の内容と事実が異なる場合があります。また、本書の内容に基づく運用結果については責任を負いかねますので、予めご了承下さい。

本書で使用するシステム名、製品名は、それぞれの各社の商標、または登録商標です。なお、本文中ではTM、®、©マークは省略しています。

\newpage

# ログ採取手順

以下の手順でProxyDialogAutoFillerのログを採取できます。
障害発生時には以下の手順でログを採取いただき、サポート窓口までご送付ください。

## ProxyDialogAutoFiller.exeのログ採取手順

1. `%AppData%\ProxyDialogAutoFiller`配下の以下のファイルを採取する。
  * `ProxyDialogAutoFiller.log`
  * `ProxyDialogAutoFiller.log.N`（Nは1から10までの整数）
