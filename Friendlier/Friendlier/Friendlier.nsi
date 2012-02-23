;-------------------------------
; Friendlier NSIS Installer
;
; Richard Bown
; February 2012
;-------------------------------

; ReadRegDWORD $0 HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client" Install IntOp $8 $0 & 1
; http://stackoverflow.com/questions/3542496/nsis-installer-with-net-4-0


!include FontReg.nsh
!include FontName.nsh
!include WinMessages.nsh

; The name of the installer
Name "Friendlier-win32-alpha-1"
Caption "Friendlier Windows32 Alpha Build 1"

!define icon "icon.ico"
!define COMPANY "Xylgo"
!define SOFTWARE "Friendlier"

; The file to write
OutFile "friendlier-win32-alpha-1.exe"

; The default installation directory
;
InstallDir $PROGRAMFILES\${COMPANY}\${SOFTWARE}

; Registry key to check for directory (so if you install again, it will
; overwrite the old one automatically)
;
InstallDirRegKey HKLM "Software\${COMPANY}\${SOFTWARE}" "Install_Dir"

; Request application privileges for Windows Vista
RequestExecutionLevel admin

; Application icon
;
Icon "rg-rwb-rose3-128x128.ico"


;--------------------------------

; Pages

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;--------------------------------

; The stuff to install
Section "Friendlier"

    SectionIn RO

    ; Set output path to the installation directory.
    SetOutPath $INSTDIR

    ; The files we are building into the package
    ;
    File "Friendlier\Friendlier\bin\x86\Debug\Friendlier.exe"
	File /r "Friendlier\Friendlier\bin\x86\Debug\Content"

    File "rg-rwb-rose3-128x128.ico"

    ; Write the installation path into the registry
    WriteRegStr HKLM "Software\${COMPANY}\${SOFTWARE}" "Install_Dir" "$INSTDIR"
    WriteRegStr HKCR "${SOFTWARE}\DefaultIcon" "" "$INSTDIR\rg-rwb-rose3-128x128.ico"

    ; Write the uninstall keys for Windows
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "DisplayName" ${SOFTWARE}
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "UninstallString" '"$INSTDIR\uninstall.exe"'

    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}" "NoRepair" 1
    WriteUninstaller "uninstall.exe"

SectionEnd

Section "Fonts"
    ; Using the FontName package this is very easy - works out the install path for us and
    ; we just need to specify the file name of the fonts.
    ;

    ; Copy the FONTS variable into FONT_DIR
    ;
    ;StrCpy $FONT_DIR $FONTS

    ; Remove and then install fonts
    ;
    ;!insertmacro RemoveTTFFont "GNU-LilyPond-feta-design20.ttf"
    ;!insertmacro RemoveTTFFont "GNU-LilyPond-feta-nummer-10.ttf"
    ;!insertmacro RemoveTTFFont "GNU-LilyPond-parmesan-20.ttf"

    ;!insertmacro InstallTTFFont "data\fonts\GNU-LilyPond-feta-design20.ttf"
    ;!insertmacro InstallTTFFont "data\fonts\GNU-LilyPond-feta-nummer-10.ttf"
    ;!insertmacro InstallTTFFont "data\fonts\GNU-LilyPond-parmesan-20.ttf"

    ; Complete font registration without reboot
    ;
    SendMessage ${HWND_BROADCAST} ${WM_FONTCHANGE} 0 0 /TIMEOUT=5000
SectionEnd


; Optional section (can be disabled by the user)
Section "Start Menu Shortcuts"

    CreateDirectory "$SMPROGRAMS\${SOFTWARE}"
    CreateShortCut "$SMPROGRAMS\${SOFTWARE}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
    ;CreateShortCut "$SMPROGRAMS\${SOFTWARE}\${SOFTWARE}.lnk" "$INSTDIR\Friendlier.exe" "" "$INSTDIR\garderobe.nsi" 0
    CreateShortCut "$SMPROGRAMS\${SOFTWARE}\${SOFTWARE}.lnk" "$INSTDIR\Friendlier.exe" "" "$INSTDIR\rg-rwb-rose3-128x128.ico"

SectionEnd

;--------------------------------

; Uninstaller

Section "Uninstall"

    ; Remove registry keys
        ;
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${SOFTWARE}"
    DeleteRegKey HKLM "Software\${COMPANY}\${SOFTWARE}"

    ; Remove files and uninstaller
    ;
    Delete $INSTDIR\uninstall.exe
    Delete "$INSTDIR\Friendlier.exe"
	Delete "$INSTDIR\rg-rwb-rose3-128x128.ico"

	; Remove the data directory and subdirs
	;
	RMDir /r "$INSTDIR\data"
	Delete "$INSTDIR\data"

    ; Remove shortcuts, if any
    Delete "$SMPROGRAMS\${SOFTWARE}\*.*"
    Delete "$INSTDIR\${SOFTWARE}\*.*"

    ; Remove directories used
    RMDir "$SMPROGRAMS\${SOFTWARE}"
    RMDir "$INSTDIR"

SectionEnd

