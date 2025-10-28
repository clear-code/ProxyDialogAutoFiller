using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDialogAutoFiller
{
    internal class TargetDefinition
    {
        public virtual string ProcessName { get; }
        public virtual string DialogTitleName { get; }
        public virtual string LoginButtonName { get; }
        public virtual string UserNameInputName { get; }
        public virtual string PasswordInputName { get; }
    }

    internal class ChromeTargetDefinition : TargetDefinition
    {
        public override string ProcessName => "chrome";
        public override string DialogTitleName => "ログイン";
        public override string LoginButtonName => "ログイン";
        public override string UserNameInputName => "ユーザー名";
        public override string PasswordInputName => "パスワード";
    }

    internal class EdgeTargetDefinition : TargetDefinition
    {
        public override string ProcessName => "msedge";
        public override string DialogTitleName => "このサイトにアクセスするにはサインインしてください";
        public override string LoginButtonName => "サインイン";
        public override string UserNameInputName => "ユーザー名";
        public override string PasswordInputName => "パスワード";
    }
}
