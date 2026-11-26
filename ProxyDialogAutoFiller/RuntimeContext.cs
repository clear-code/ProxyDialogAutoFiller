/*
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.

Copyright (c) 2025 ClearCode Inc.
*/
using System;

namespace ProxyDialogAutoFiller
{
    internal class RuntimeContext
    {
        internal Logger Logger { get; }

        internal Config Config { get; }

        internal RuntimeContext()
        {
            // 設定ファイルの読み込みに失敗した場合もログに出力できるよう、先にLoggerを作成する
            Logger = new Logger();
            try
            {
                Config = ConfigLoader.LoadConfig();
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load the config file: {ex}");
                throw;
            }
        }
    }
}
