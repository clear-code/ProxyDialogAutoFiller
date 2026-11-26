/*
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.

Copyright (c) 2025 ClearCode Inc.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace ProxyDialogAutoFiller
{
    internal static class ProxyDialogAutoFiller
    {
        /// <summary>
        /// 処理の区切りごとに経過時間を確認し、時間がかかった処理だけをログに出力する。
        /// 時間がかかった場合に、おおまかにどの処理で時間がかかったかを把握するためのもの。
        /// </summary>
        private sealed class StepTimer
        {
            // この時間以上かかった処理をログに出力する
            private const long SlowThresholdMilliseconds = 3000;

            private readonly RuntimeContext context;
            private readonly string target;
            private readonly Stopwatch stopwatch = Stopwatch.StartNew();

            // 直前に終わった処理の名前（例外発生時にどこまで進んでいたかを示すため）
            internal string LastStep { get; private set; } = "(start)";

            internal StepTimer(RuntimeContext context, string target)
            {
                this.context = context;
                this.target = target;
            }

            /// <summary>
            /// 処理が1つ終わったことを記録する。前回の記録からの経過時間が長ければログに出力する。
            /// </summary>
            internal void Step(string stepName)
            {
                var elapsed = stopwatch.ElapsedMilliseconds;
                if (elapsed >= SlowThresholdMilliseconds)
                {
                    context.Logger.Log($"Slow: {stepName} took {elapsed} ms ({target}).");
                }
                LastStep = stepName;
                stopwatch.Restart();
            }

            /// <summary>
            /// 例外をログに出力する。ウィンドウやダイアログが閉じられたことによる例外は1行で出力する。
            /// </summary>
            internal void LogException(Exception ex)
            {
                if (ex is ElementNotAvailableException)
                {
                    context.Logger.Log($"Window or dialog was closed after '{LastStep}' ({target}): {ex.GetType().Name}: {ex.Message}");
                }
                else
                {
                    context.Logger.Log($"Error after '{LastStep}' ({target}): {ex}");
                }
            }
        }

        internal static void WatchDialog(RuntimeContext context)
        {
            var targetDefinitions = new TargetDefinition[]
            {
                new EdgeTargetDefinition(),
                new ChromeTargetDefinition(),
            };
            // 確認中のウィンドウ（キー：ウィンドウハンドル、値：確認処理のスレッド）
            var checkingThreads = new Dictionary<IntPtr, Thread>();
            while (true)
            {
                var timer = new StepTimer(context, "watch loop");
                try
                {
                    WatchDialogOnce(context, targetDefinitions, checkingThreads, timer);
                }
                catch (Exception ex)
                {
                    // 想定外の例外で監視が止まらないよう、ログに出力して監視を続ける
                    timer.LogException(ex);
                }
                Task.Delay(500).Wait();
            }
        }

        private static void WatchDialogOnce(RuntimeContext context, TargetDefinition[] targetDefinitions, Dictionary<IntPtr, Thread> checkingThreads, StepTimer timer)
        {
            foreach (var targetDefinition in targetDefinitions)
            {
                //ブラウザのプロセスのうち、メインウィンドウがあるものに絞り込み
                var filteredElements = Process.GetProcessesByName(targetDefinition.ProcessName).Where(_ => _.MainWindowHandle != IntPtr.Zero).ToList();
                timer.Step($"Find {targetDefinition.ProcessName} windows");

                foreach (var filteredElement in filteredElements)
                {
                    // 終了処理中のブラウザーのウィンドウにUI Automationで問い合わせると、そのプロセスが
                    // 終了するまで（数十秒～数分）応答が返らない。1つのスレッドで順に確認すると、その間は
                    // 新しく起動したブラウザーのプロキシダイアログも確認できなくなるため、ウィンドウごとに
                    // 別スレッドで確認する。前回の確認が終わっていないウィンドウは、その周回では確認しない。
                    var windowHandle = filteredElement.MainWindowHandle;
                    if (checkingThreads.TryGetValue(windowHandle, out var checkingThread) && checkingThread.IsAlive)
                    {
                        continue;
                    }
                    var target = $"{targetDefinition.ProcessName} pid={filteredElement.Id} hwnd=0x{windowHandle.ToInt64():X}";
                    checkingThread = new Thread(() => CheckWindow(context, windowHandle, targetDefinition, target))
                    {
                        IsBackground = true,
                    };
                    // 従来のメインスレッドと同じくSTAで動かす（SendKeysを使うため）
                    checkingThread.SetApartmentState(ApartmentState.STA);
                    checkingThread.Start();
                    checkingThreads[windowHandle] = checkingThread;
                }
            }
            foreach (var finishedWindow in checkingThreads.Where(_ => !_.Value.IsAlive).Select(_ => _.Key).ToList())
            {
                checkingThreads.Remove(finishedWindow);
            }
            timer.Step("Start checking windows");

            // 使い終わったUI Automationの要素を、ブラウザーがまだ動いているうちに解放する。
            // 要素の解放がブラウザーの終了処理中に行われると、そのプロセスが終了するまで解放処理が
            // ブロックし、それに巻き込まれて新しいウィンドウへのUI Automationの呼び出しも待たされるため。
            GC.Collect();
            timer.Step("GC.Collect");
        }

        private static void CheckWindow(RuntimeContext context, IntPtr windowHandle, TargetDefinition targetDefinition, string target)
        {
            var timer = new StepTimer(context, target);
            try
            {
                // デスクトップ全体をUI Automationで検索すると、終了処理中のブラウザーのウィンドウへの
                // 問い合わせに巻き込まれることがあるため、ウィンドウハンドルから直接取得する。
                AutomationElement targetElement;
                try
                {
                    targetElement = AutomationElement.FromHandle(windowHandle);
                }
                catch (Exception)
                {
                    // 確認の前にウィンドウが閉じられた場合など。次の周回で再確認するため、例外のログは出力しない。
                    // （時間がかかった場合のみログを出力する）
                    timer.Step("Get window element (failed)");
                    return;
                }
                timer.Step("Get window element");
                PrintControlIdentifiers(context, targetElement, 0);
                LoginToProxy(context, targetElement, targetDefinition, timer);
            }
            catch (Exception ex)
            {
                // スレッド内の例外でプロセス全体が終了しないよう、ここで受け止める
                timer.LogException(ex);
            }
        }

        [Conditional("DEBUG")]
        internal static void PrintControlIdentifiers(RuntimeContext context, AutomationElement element, int indent)
        {
            try
            {
                var ind = new string(' ', indent * 2);
                Console.WriteLine($"{ind}- {element.Current.ControlType.ProgrammaticName} : {element.Current.Name}");
                var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
                foreach (AutomationElement child in children)
                {
                    PrintControlIdentifiers(context, child, indent + 1);
                }
            }
            catch (ElementNotAvailableException)
            {
                // 出力中に要素が消えることはよくあるので（タブの切り替えなど）、ログは出力しない
            }
            catch (Exception ex)
            {
                context.Logger.Log(ex);
            }
        }

        private static void SendValue(AutomationElement element, string value)
        {
            // ValuePatternが使える場合
            object valuePatternObj;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out valuePatternObj))
            {
                (valuePatternObj as ValuePattern).SetValue(value);
            }
            else
            {
                // フォーカス後にSendKeys（WinFormsやWPF参照）などで文字列を入力
                try
                {
                    element.SetFocus();
                }
                catch
                {
                    // ignore set focus error
                }
                System.Windows.Forms.SendKeys.SendWait(value);
            }
        }

        private static void LoginToProxy(RuntimeContext context, AutomationElement targetRootElement, TargetDefinition dialogDefinition, StepTimer timer)
        {
            if (context.Config.SectionList == null || context.Config.SectionList.Count == 0)
            {
                context.Logger.Log($"Empty config.");
                return;
            }
            try
            {
                var proxyDialogNameCondition = new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.DialogTitleName);
                var targetControlTypeCondition = new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Custom));
                var proxyDialogCondition = new AndCondition(
                    targetControlTypeCondition,
                    proxyDialogNameCondition);
                var proxyDialogElement = targetRootElement.FindFirst(TreeScope.Subtree, proxyDialogCondition);
                timer.Step("Find proxy dialog");
                if (proxyDialogElement == null)
                {
                    return;
                }
                context.Logger.Log($"Found proxy dialog.");
                var textTypeDescendants = proxyDialogElement.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

                bool isTargetProxy = false;
                string userName = "";
                string password = "";
                foreach (AutomationElement textTypeDescendant in textTypeDescendants)
                {
                    string name = textTypeDescendant.Current.Name;
                    if (!string.IsNullOrEmpty(name) && name.Contains("プロキシ"))
                    {
                        foreach (var section in context.Config.SectionList)
                        {
                            // Section name is proxy host name.
                            // Check if the dialog contains the proxy host name.
                            string lowerName = name.ToLowerInvariant();
                            string lowerSectionName = section.Name.ToLowerInvariant();
                            if (lowerName.Contains(lowerSectionName))
                            {
                                isTargetProxy = true;
                                userName = section.UserName;
                                password = section.Password;
                                break;
                            }
                        }
                    }
                    if (isTargetProxy)
                    {
                        break;
                    }
                }
                timer.Step("Read proxy dialog text");

                if (!isTargetProxy)
                {
                    return;
                }
                context.Logger.Log($"Found proxy dialog matching to config.");
                var userNameEditCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.UserNameInputName),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                var userNameEditElement = proxyDialogElement.FindFirst(TreeScope.Descendants, userNameEditCondition);
                if (userNameEditElement == null)
                {
                    context.Logger.Log($"User name edit not found.");
                    return;
                }
                SendValue(userNameEditElement, userName);
                timer.Step("Set username");

                context.Logger.Log($"Set username.");
                var passwordEditCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.PasswordInputName),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                var passwordEditElement = proxyDialogElement.FindFirst(TreeScope.Descendants, passwordEditCondition);
                if (passwordEditElement == null)
                {
                    context.Logger.Log($"Password edit not found.");
                    return;
                }
                SendValue(passwordEditElement, password);
                timer.Step("Set password");
                context.Logger.Log($"Set password.");
                var loginButtonNameCondition = new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.LoginButtonName);
                var loginButtonCondition = new AndCondition(
                    loginButtonNameCondition,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                var loginButtonElement = proxyDialogElement.FindFirst(TreeScope.Descendants, loginButtonCondition);
                if (loginButtonElement == null)
                {
                    context.Logger.Log($"Login button not found.");
                    return;
                }
                InvokePattern loginButton = loginButtonElement.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                if (loginButton == null)
                {
                    return;
                }

                Task.Delay(50).Wait();
                loginButton.Invoke();
                timer.Step("Click login button");
                context.Logger.Log($"Click login button.");

                // プロキシーダイアログが消えていることを確認する。
                // ユーザー名、パスワードに誤りがあった場合などに、短時間で連続してダイアログの表示とOKが繰り返され、ユーザーが対処できなくなる可能性がある。
                // そのため、ダイアログが表示されている場合、ここで15秒間待機し、ダイアログが閉じられなかった場合にユーザーがキャンセルや正しいユーザー名
                // パスワードが入力できるようにする。
                bool isProxyDialogClosed = false;
                for (int i = 0; i < 30; i++)
                {
                    Task.Delay(500).Wait();
                    // ログインボタンが消えていたらダイアログも消えていると判断する。
                    proxyDialogElement = targetRootElement.FindFirst(TreeScope.Descendants, proxyDialogCondition);
                    timer.Step($"Check if proxy dialog is closed (#{i + 1})");
                    if (proxyDialogElement == null)
                    {
                        context.Logger.Log($"Proxy dialog not found.");
                        isProxyDialogClosed = true;
                        break;
                    }
                    loginButtonElement = proxyDialogElement.FindFirst(TreeScope.Descendants, loginButtonCondition);
                    if (loginButtonElement == null)
                    {
                        context.Logger.Log($"Login button not found.");
                        isProxyDialogClosed = true;
                        break;
                    }
                    loginButton = loginButtonElement.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                    if (loginButton == null)
                    {
                        context.Logger.Log($"Cannot cast to InvokePattern.");
                        isProxyDialogClosed = true;
                        break;
                    }

                    // ChromeでloginButton.Invoke()の実行までが早すぎて応答しないことがあるので
                    // ここで二回までリトライする。
                    if (i < 2)
                    {
                        try
                        {
                            loginButton.Invoke();
                            context.Logger.Log($"Retry to click login button.");
                        }
                        catch
                        {
                            // ログインボタンのInvokeに失敗した場合、ログインボタンが消えていると判断してループを抜ける。
                            context.Logger.Log($"Failed to invoke button");
                            isProxyDialogClosed = true;
                            break;
                        }
                    }
                }

                if (isProxyDialogClosed)
                {
                    context.Logger.Log($"Success to login to the proxy.");
                }
                else
                {
                    context.Logger.Log($"Dialog not closed. Maybe failed to login to the proxy.");
                }
            }
            catch (Exception ex)
            {
                timer.LogException(ex);
            }
        }
    }
}
