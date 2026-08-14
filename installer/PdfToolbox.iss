#define MyAppName "PdfToolbox"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Mirin"
#define MyAppExeName "PdfToolbox.exe"

[Setup]
AppId={{7472f3d4.PdfToolbox}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright=Copyright (C) 2026 Mirin
DefaultDirName={localappdata}\Programs\PdfToolbox
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
CloseApplicationsFilter=PdfToolbox.exe
RestartApplications=no
SetupIconFile=..\PdfToolbox\Assets\PdfToolboxIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\artifacts
OutputBaseFilename=PdfToolbox-1.0.1-win-x64-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
Uninstallable=yes
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=PdfToolbox installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
UninstallDisplayName={#MyAppName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[InstallDelete]
Type: files; Name: "{app}\*.pdb"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "Offline PDF tools"

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\PdfToolbox"
Type: filesandordirs; Name: "{localappdata}\PdfToolbox"

; No Startup-folder entry and no Run registry value are intentionally created.

[Code]
function HasDotNet10DesktopRuntime: Boolean;
var
  DotnetPath: String;
  TempFile: String;
  OutputText: AnsiString;
  ResultCode: Integer;
  CommandLine: String;
begin
  Result := False;
  DotnetPath := ExpandConstant('{commonpf64}\dotnet\dotnet.exe');
  if not FileExists(DotnetPath) then
    DotnetPath := 'dotnet.exe';
  TempFile := ExpandConstant('{tmp}\PdfToolbox-dotnet-runtimes.txt');
  DeleteFile(TempFile);
  CommandLine := '/C ""' + DotnetPath + '" --list-runtimes > "' + TempFile + '" 2>&1"';
  if Exec(ExpandConstant('{sys}\cmd.exe'), CommandLine, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TempFile, OutputText) then
      Result := Pos('Microsoft.WindowsDesktop.App 10.', OutputText) > 0;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := HasDotNet10DesktopRuntime;
  if not Result then
    MsgBox('Microsoft .NET 10 Desktop Runtime (x64) が見つかりません。' + #13#10 + #13#10 +
      'Runtimeを同梱しないframework-dependent版のため、先に公式の .NET 10 Desktop Runtime (x64) をインストールしてください。',
      mbError, MB_OK);
end;
