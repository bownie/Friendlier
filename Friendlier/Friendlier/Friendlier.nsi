;-------------------------------
; Friendlier NSIS Installer
;
; Richard Bown
; February 2012
;-------------------------------

; ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" Install IntOp $8 $0 & 1
; http://stackoverflow.com/questions/3542496/nsis-installer-with-net-4-0

; We're using the modern UI
;
!include "MUI.nsh"

; No fonts required.  Yet.
;!include FontReg.nsh
;!include FontName.nsh

!include WinMessages.nsh
!include LogicLib.nsh
!include Sections.nsh

; The name of the installer
Name "Friendlier-win32-alpha-1"
Caption "Friendlier Windows32 Alpha Build 1"

!define ICON "Xyglo.ico"
!define COMPANY "Xyglo"
!define SOFTWARE "Friendlier"
!define VERSION "1.0.0"

!insertmacro MUI_PAGE_LICENSE "Licence.txt"
!insertmacro MUI_LANGUAGE "English"

; .NET installer
;
!define NETVersion "4.0"
!define NETInstaller "dotNetFx40_Full_setup.exe"
!define XNAVersion "4.0"
!define XNAInstaller "xnafx40_redist.msi"

; The file to write
OutFile "friendlier-win32-alpha-1.exe"

; The default installation directory
;
InstallDir $PROGRAMFILES\${COMPANY}\${SOFTWARE}

; Registry key to check for directory (so if you install again, it will
; overwrite the old one automatically)
;
InstallDirRegKey HKLM "Software\${COMPANY}\${SOFTWARE}" "Install_Dir"

; Request application privileges for Windows Vista/7 etc
;
RequestExecutionLevel admin

; Application icon
;
Icon ${ICON}

;--------------------------------
; Pages
Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------
; .NET 4.0 check and installer
;
; Non-optional section starts with a '-'
;
Section "-MS .NET Framework v${NETVersion}" NETFramework

  ; Read the registry string where .NET should be
  ;
  ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Install"

  ;MessageBox MB_OK $R0
  ;IfErrors installNET NETFrameworkInstalled

  ${if} $R0 != 1

    ;MessageBox MB_OK "Will install .NET 4.0"
    DetailPrint "Starting Microsoft .NET 4.0 Framework v${NETVersion} installer."
	
	File /oname=$TEMP\${NETInstaller} ${NETInstaller}

    ExecWait "msiexec /i $TEMP\${NETInstaller} /q" $0

	; check for errors
	${if} $0 != 0
	  MessageBox MB_OK|MB_ICONSTOP "Failed to install .NET - please investigate"
	  Abort
	${endif}
 
  ${else}

    ;MessageBox MB_OK "Microsoft .NET 4.0 Framework is already installed."
    DetailPrint "Microsoft .NET 4.0 Framework is already installed."

  ${endif}
 
SectionEnd

;--------------------------------
; XNA 4.0 check and installer
;
; Non-optional section starts with a '-'
;
Section "-XNA Framework v${XNAVersion}" XNAFramework

  ; Read the registry string where .NET should be
  ;
  ReadRegStr $R0 HKLM "SOFTWARE\Microsoft\XNA\Framework\v4.0" "Installed"

  ;MessageBox MB_OK $R0
  ${if} $R0 != 1 

    ;MessageBox MB_OK "Will install XNA 4.0"
    DetailPrint "Starting Microsoft XNA Framework v${XNAVersion} installer."
    File /oname=$TEMP\${XNAInstaller} ${XNAInstaller}
    ExecWait "msiexec /i $TEMP\${XNAInstaller}" $0

    ; check for errors
	${if} $0 != 0
	  MessageBox MB_OK|MB_ICONSTOP "Failed to install XNA - please investigate"
	  Abort
	${endif}
 
  ${else}

    ;MessageBox MB_OK "XNA Framework is already installed."
    DetailPrint "Microsoft XNA Framework is already installed."

  ${endif}
 
SectionEnd


; The stuff we want to install
;
Section "Friendlier"

    SectionIn RO

    ; Set output path to the installation directory.
    SetOutPath $INSTDIR

    ; The files we are building into the package
    ;
    File "Friendlier\Friendlier\bin\x86\Release\Friendlier.exe"
	File /r "Friendlier\Friendlier\bin\x86\Release\Content"

	; Include the third party installers
	;
	;File ${NETINSTALLER}
	;File ${XNAINSTALLER}

    File "Xyglo.ico"

    ; Write the installation path into the registry
    WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}" "Install_Dir" "$INSTDIR"
    WriteRegStr HKCR "${SOFTWARE}\DefaultIcon" "" "$INSTDIR\${ICON}"

    ; Write the uninstall keys for Windows
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "DisplayName" ${SOFTWARE}
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "UninstallString" '"$INSTDIR\uninstall.exe"'

    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "NoRepair" 1
    WriteUninstaller "uninstall.exe"


	; Now write a key for the software licencing
	;
	WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}\CurrentVersion" "User Email" "none"
	WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}\CurrentVersion" "User Organisation" "none"
	WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}\CurrentVersion" "Product Name" ${SOFTWARE}
	WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}\CurrentVersion" "Product Version" ${VERSION}
	WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}\CurrentVersion" "Licence Key" "nonel"


SectionEnd

; Non-optional section starts with a '-'
;
Section "-Start Menu Shortcuts"

    ; Fix for Windows 7
	;
    SetShellVarContext all

    CreateDirectory "$SMPROGRAMS\${COMPANY}\${SOFTWARE}"
    CreateShortCut "$SMPROGRAMS\${COMPANY}\${SOFTWARE}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
    ;CreateShortCut "$SMPROGRAMS\${COMPANY}\${SOFTWARE}\${SOFTWARE}.lnk" "$INSTDIR\Friendlier.exe" "" "$INSTDIR\Friendlier.nsi" 0
    CreateShortCut "$SMPROGRAMS\${COMPANY}\${SOFTWARE}\${SOFTWARE}.lnk" "$INSTDIR\Friendlier.exe" "" "$INSTDIR\${ICON}"

SectionEnd

;--------------------------------
; Uninstaller
;
Section "Uninstall"

    ; fix for Windows 7
	;
    SetShellVarContext all

    ; Remove registry keys
        ;
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}"
    DeleteRegKey HKLM "Software\${COMPANY}\${SOFTWARE}"

    ; Remove files and uninstaller
    ;
    Delete "$INSTDIR\uninstall.exe"
    Delete "$INSTDIR\Friendlier.exe"
	Delete "$INSTDIR\${ICON}"

	; Delete bundled installers
	;
	;Delete "$INSTDIR\${NETInstaller}"
	;Delete "$INSTDIR\${XNAInstaller}"

	; Remove the data directory and subdirs
	;
	RMDir /r "$INSTDIR\Content"
	Delete "$INSTDIR\Content"

    ; Remove shortcuts, if any
    Delete "$SMPROGRAMS\${COMPANY}\${SOFTWARE}\*.*"
    Delete "$INSTDIR\${SOFTWARE}\*.*"

    ; Remove directories used
	;
    RMDir "$SMPROGRAMS\${COMPANY}\${SOFTWARE}"
    RMDir "$INSTDIR"

SectionEnd

