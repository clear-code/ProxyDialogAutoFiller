/*
This Source Code Form is subject to the terms of the Mozilla Public
License, v. 2.0. If a copy of the MPL was not distributed with this
file, You can obtain one at http://mozilla.org/MPL/2.0/.

Copyright (c) 2025 ClearCode Inc.
*/
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace ProxyDialogAutoFiller
{
    internal static class ProxyDialogAutoFiller
    {
        internal static void WatchDialog(RuntimeContext context)
        {
            AutomationElement desktop = AutomationElement.RootElement;
            var targetDefinitions = new TargetDefinition[]
            {
                new EdgeTargetDefinition(),
                new ChromeTargetDefinition(),
            };
            while (true)
            {
                foreach (var targetDefinition in targetDefinitions)
                {
                    //ブラウザのプロセスのうち、メインウィンドウがあるものに絞り込み
                    var filterdElements = Process.GetProcessesByName(targetDefinition.ProcessName).Where(_ => _.MainWindowHandle != IntPtr.Zero);
                    foreach (var filterdElement in filterdElements)
                    {
                        var targetPid = filterdElement.Id;
                        var windowCondition = new AndCondition(
                            new PropertyCondition(AutomationElement.ProcessIdProperty, targetPid),
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
                        var targetElement = desktop.FindFirst(TreeScope.Children, windowCondition);
                        if (targetElement == null)
                        {
                            continue;
                        }
                        PrintControlIdentifiers(context, targetElement, 0);
                        LoginToProxy(context, targetElement, targetDefinition);
                    }
                }
                Task.Delay(500).Wait();
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
                element.SetFocus();
                (valuePatternObj as ValuePattern).SetValue(value);
            }
            else
            {
                // フォーカス後にSendKeys（WinFormsやWPF参照）などで文字列を入力
                element.SetFocus();
                System.Windows.Forms.SendKeys.SendWait(value);
            }
        }

        internal static void LoginToProxy(RuntimeContext context, AutomationElement targetRootElement, TargetDefinition dialogDefinition)
        {
            if(context.Config.SectionList == null || context.Config.SectionList.Count == 0)
            {
                return;
            }
            try
            {
                var proxyDialogNameCondition = new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.DialogTitleName);
                var proxyDialogCondition = new AndCondition(
                    proxyDialogNameCondition,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
                var proxyDialogElement = targetRootElement.FindFirst(TreeScope.Descendants, proxyDialogCondition);
                if (proxyDialogElement == null)
                {
                    return;
                }
                context.Logger.Log($"Found proxy dialog");
                var textTypeDescendants = proxyDialogElement.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

                bool isTargetProxy = false;
                string userName = "";
                string password = "";
                foreach(AutomationElement textTypeDescendant in textTypeDescendants)
                {
                    string name = textTypeDescendant.Current.Name;
                    if (!string.IsNullOrEmpty(name) && name.Contains("プロキシ"))
                    {
                        foreach (var section in context.Config.SectionList)
                        {
                            // Section name is proxy host name.
                            // Check if the dialog contains the proxy host name.
                            if (name.Contains(section.Name))
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

                if (!isTargetProxy)
                {
                    return;
                }

                var userNameEditCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.UserNameInputName),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                var userNameEditElement = proxyDialogElement.FindFirst(TreeScope.Descendants, userNameEditCondition);
                if (userNameEditElement == null)
                {
                    return;
                }
                SendValue(userNameEditElement, userName);

                var passwordEditCondition = new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.PasswordInputName),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
                var passwordEditElement = proxyDialogElement.FindFirst(TreeScope.Descendants, passwordEditCondition);
                if (passwordEditElement == null)
                {
                    return;
                }
                SendValue(passwordEditElement, password);

                var loginButtonNameCondition = new PropertyCondition(AutomationElement.NameProperty, dialogDefinition.LoginButtonName);
                var loginButtonCondition = new AndCondition(
                    loginButtonNameCondition,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                var loginButtonElement = proxyDialogElement.FindFirst(TreeScope.Descendants, loginButtonCondition);
                if (loginButtonElement == null)
                {
                    return;
                }
                InvokePattern loginButton = loginButtonElement.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
                if (loginButton == null)
                {
                    return;
                }
                loginButton.Invoke();
                context.Logger.Log($"Logged in proxy");

                // プロキシーダイアログが消えていることを確認する。
                // 最大10秒待つ。
                for (int i = 0; i < 10; i++)
                {
                    proxyDialogElement = targetRootElement.FindFirst(TreeScope.Descendants, proxyDialogCondition);
                    if (proxyDialogElement == null)
                    {
                        break;
                    }
                    Task.Delay(1000).Wait();
                }

                if (proxyDialogElement != null)
                {
                    context.Logger.Log($"Dialog not closed");
                }

                // ユーザー名、パスワードに誤りがあった場合などに、短時間で連続してダイアログの表示とOKが繰り返され、ユーザーが対処できなくなる可能性がある。
                // そのため、ここでさらに10秒間待機し、ダイアログが閉じられなかった場合にユーザーがキャンセルや正しいユーザー名、パスワードが入力できるようにする。
                Task.Delay(10000).Wait();
            }
            catch (Exception ex)
            {
                context.Logger.Log(ex);
            }
        }
    }
}
