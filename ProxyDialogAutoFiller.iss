;ProxyDialogAutoFiller Setup--

[Setup]
AppName=ProxyDialogAutoFiller
AppVerName=ProxyDialogAutoFiller
VersionInfoVersion=1.1.0.0
AppVersion=1.1.0.0
AppMutex=ProxyDialogAutoFillerSetup
DefaultDirName={code:GetProgramFiles}\ProxyDialogAutoFiller
Compression=lzma2
SolidCompression=yes
OutputDir=SetupOutput
OutputBaseFilename=ProxyDialogAutoFillerSetup
AppPublisher=ProxyDialogAutoFiller
WizardImageStretch=no
VersionInfoDescription=ProxyDialogAutoFillerSetup
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DefaultGroupName=ProxyDialogAutoFiller
UninstallDisplayIcon={app}\ProxyDialogAutoFiller.exe

[Registry]
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Path"; ValueData: "{app}\"
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "ClientType"; ValueData: ""
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Version"; ValueData: "1.1.0.0"
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Rulefile"; ValueData: "{app}\ProxySetting.ini"
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "RCAPfile"; ValueData: "{app}\ResourceCap.ini"
Root: HKLM; Subkey: "Software\ProxyDialogAutoFiller"; ValueType: string; ValueName: "ExtensionExecfile"; ValueData: "{app}\ProxyDialogAutoFiller.exe"

Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; Flags: uninsdeletekey
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Path"; ValueData: "{app}\"
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "ClientType"; ValueData: ""
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Version"; ValueData: "1.1.0.0"
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "Rulefile"; ValueData: "{app}\ProxySetting.ini"
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "RCAPfile"; ValueData: "{app}\ResourceCap.ini"
Root: HKLM; Subkey: "Software\WOW6432Node\ProxyDialogAutoFiller"; ValueType: string; ValueName: "ExtensionExecfile"; ValueData: "{app}\ProxyDialogAutoFiller.exe"

[Languages]
Name: jp; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
;exe
Source: "bin\Release\ProxyDialogAutoFiller.exe"; DestDir: "{app}\";Flags: ignoreversion;permissions:users-readexec admins-full system-full
;ini
Source: "Resources\ProxySetting.ini"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Icons]
Name: "{commonstartup}\ProxyDialogAutoFiller"; Filename: "{app}\ProxyDialogAutoFiller.exe"; WorkingDir: "{app}"

[Dirs]
Name: "{app}";Permissions: users-modify

[Run] 
Filename: "{sys}\icacls.exe";Parameters: """{app}\ProxyDialogAutoFiller.exe"" /inheritance:r"; Flags: runhidden shellexec
Filename: "{app}\ProxyDialogAutoFiller.exe"; Description: "インストール後にプログラムを起動する"; Flags: postinstall nowait

[UninstallRun]

[Code]
function GetProgramFiles(Param: string): string;
  begin
    if IsWin64 then Result := ExpandConstant('{pf64}')
    else Result := ExpandConstant('{pf32}')
  end;

procedure TaskKill(FileName: String);
var
  ResultCode: Integer;
begin
    Exec(ExpandConstant('taskkill.exe'), '/f /im ' + '"' + FileName + '"', '', SW_HIDE,ewWaitUntilTerminated, ResultCode);
end;
function InitializeSetup():Boolean;
begin 
	TaskKill('ProxyDialogAutoFiller.exe');
	Result := True; 
end; 
function InitializeUninstall():Boolean;
begin 
	TaskKill('ProxyDialogAutoFiller.exe');
	Result := True; 
end; 
