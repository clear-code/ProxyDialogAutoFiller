/*
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.

Copyright (c) 2025 ClearCode Inc.
*/
using Microsoft.Win32;
using ProxyDialogAutoFiller;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyDialogAutoFiller
{
    class Program
    {

        private static Mutex MyMutex { get; } = new Mutex(false, @"ProxyDialogAutoFiller-4DB06AAB-2148-469F-BA51-24E3E73E4C61");

        [STAThread]
        static void Main()
        {
            bool isMutexAcquired = false;
            try
            {
                isMutexAcquired = MyMutex.WaitOne(1000, false);
            }
            catch (AbandonedMutexException)
            {
                // 既に起動中のプロセスが強制終了した。Mutexの所有権は取得できている。
                // 本プログラムは、単なる同時起動の排他のためだけにMutexを使用しているので、この場合もそのまま
                // 処理を続けて問題ない。
                isMutexAcquired = true;
            }

            if (isMutexAcquired)
            {
                // Mutexの獲得に成功した
                // ダイアログの監視を開始
                try
                {
                    ProxyDialogAutoFiller.WatchDialog(new RuntimeContext());
                }
                finally
                {
                    MyMutex.ReleaseMutex();
                }
            }
        }
    }
}