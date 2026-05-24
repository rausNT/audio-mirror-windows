namespace AudioMirrorApp;

internal static class AppText
{
    public sealed record Language(string Code, string Name);

    private static readonly Dictionary<string, string> English = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Language"] = "Language",
        ["Version"] = "Version",
        ["File"] = "File",
        ["Actions"] = "Actions",
        ["Help"] = "Help",
        ["Start"] = "Start",
        ["Stop"] = "Stop",
        ["Save"] = "Save",
        ["SaveSettings"] = "Save settings",
        ["Autostart"] = "Autostart",
        ["Exit"] = "Exit",
        ["Refresh"] = "Refresh",
        ["RefreshDevices"] = "Refresh devices",
        ["Sound"] = "Sound",
        ["SoundSettings"] = "Sound settings",
        ["Sync"] = "Safe levels",
        ["Test"] = "Test",
        ["TestSpeakers"] = "Test speakers",
        ["SplitLR"] = "Split L/R",
        ["UserHelp"] = "User help",
        ["AboutAudioMirror"] = "About AudioMirror",
        ["OpenAudioMirror"] = "Open AudioMirror",
        ["Source"] = "Source",
        ["Target1"] = "Target 1",
        ["Target2"] = "Target 2",
        ["Target3"] = "Target 3",
        ["Third"] = "Third",
        ["Gain"] = "Gain",
        ["DelayMs"] = "Delay ms",
        ["AutoRestart"] = "Auto restart",
        ["Hint"] = "Choose the Windows default output as Source, choose real speakers as Targets, then press Start.",
        ["Close"] = "Close",
        ["Left"] = "Left",
        ["Right"] = "Right",
        ["Both"] = "Both",
        ["Loop"] = "Loop",
        ["TestTitle"] = "AudioMirror Test",
        ["TestHint"] = "Left plays Target 1, Right plays Target 2, Third plays Target 3 when enabled, Both plays all targets.",
        ["HelpTitle"] = "AudioMirror Help",
        ["HelpHeading"] = "Help",
        ["HelpBody"] = "Quick setup\n\n1. Set an unused playback device as the Windows default output.\n2. Choose that device as Source in AudioMirror.\n3. Choose two real speakers or monitors as Target 1 and Target 2.\n4. Optionally enable Target 3 for a USB soundbar or another output.\n5. Press Start.\n\nControls\n\nGain is a volume multiplier. 1.0 means unchanged.\nDelay ms adds delay to one target to reduce echo.\nSplit L/R sends the left channel to Target 1 and the right channel to Target 2; Target 3 stays stereo.\nSafe levels lowers gains above 1.0 back to 1.0.\nAuto restart recreates audio streams after sleep, display power-off, or endpoint reset.\nTest opens a built-in speaker test.\n\nIf no audio reaches Source, set Source as the Windows default output and restart the browser or player.",
        ["AboutTitle"] = "About AudioMirror",
        ["AboutHeading"] = "AudioMirror",
        ["AboutBody"] = "AudioMirror\n\nA small Windows WASAPI utility for mirroring one playback stream to two output devices with per-target gain, delay, channel split, and built-in speaker testing.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepository\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicense\nMIT License\n\nThe software is provided as is, without warranty of any kind.",
        ["DefaultSuffix"] = "default",
        ["UnpluggedSuffix"] = "unplugged",
        ["NotPresentSuffix"] = "not present",
        ["DevicesRefreshed"] = "Devices refreshed: {0} playback endpoints.",
        ["SelectDevices"] = "Select source and target devices to check formats.",
        ["Stopped"] = "Stopped.",
        ["RunningLine"] = "Running: {0} -> {1}, {2}",
        ["RunningTargetsLine"] = "Running: {0} -> {1}",
        ["FormatLine"] = "Format: {0} Hz, {1} ch, {2} bit. Mode: {3}",
        ["PacketsLine"] = "Packets {0}, captured {1}, T1 written {2}, dropped {3}, T2 written {4}, dropped {5}",
        ["ModeSplit"] = "Split L/R",
        ["ModeStereo"] = "Stereo mirror",
        ["ErrorLine"] = "Error: {0}",
        ["Saved"] = "Saved: {0}",
        ["AutostartRegistered"] = "Autostart registered. Settings saved.",
        ["StartFailed"] = "Start failed",
        ["AutostartFailed"] = "Autostart failed",
        ["TestFailed"] = "Test failed",
        ["PausedSleep"] = "Paused for system sleep. AudioMirror will refresh devices after resume.",
        ["WaitingDevices"] = "Waiting for audio devices after {0}...",
        ["DevicesRefreshedAfter"] = "Devices refreshed after {0}: {1} playback endpoints.",
        ["WaitingSelectedAfter"] = "Waiting for selected audio devices after {0}... {1} endpoint(s) visible.",
        ["DeviceRefreshFailed"] = "Device refresh after {0} failed: {1}",
        ["Restarting"] = "Restarting AudioMirror: {0}...",
        ["Restarted"] = "AudioMirror restarted: {0}.",
        ["RestartedAfter"] = "AudioMirror restarted after {0}.",
        ["AutoRestartFailed"] = "Auto restart failed: {0}",
        ["StreamError"] = "audio stream error",
        ["StreamStalled"] = "audio stream stalled",
        ["DeviceChange"] = "audio device change",
        ["SystemResume"] = "system resume",
        ["DisplayChange"] = "display change",
        ["NotActiveSelected"] = "Some selected devices are visible but not active yet. Wake the display, then press Refresh or wait for auto refresh.",
        ["DeviceActive"] = "{0} is active",
        ["DeviceUnplugged"] = "{0} is unplugged",
        ["DeviceNotPresent"] = "{0} is not present",
        ["DeviceNotActive"] = "{0} is not active",
        ["EnsureActive"] = "{0} is visible in Windows but is not active yet: {1}. Wake the display, then press Refresh or wait for auto refresh.",
        ["FormatMatch"] = "Formats match: {0}",
        ["TargetsMatch"] = "Targets match: {0}. Source differs: {1}; Windows will resample.",
        ["TargetFormatsDiffer"] = "Target formats differ. T1 {0}; T2 {1}. Set both targets to 48000 Hz 16/24 bit.",
        ["CouldNotReadFormat"] = "Could not read device formats: {0}",
        ["CouldNotReadFormatShort"] = "Could not read format",
        ["FormatTooltip"] = "{0}: {1}",
        ["OpenSoundTooltip"] = "Click to open Windows sound settings.",
        ["SyncApplied"] = "Safe levels applied: gains above 1.0 were lowered to 1.0.",
        ["SelectAllDevices"] = "Select all devices first.",
        ["NotifyRunning"] = "AudioMirror - running",
        ["NotifyWaiting"] = "AudioMirror - waiting for devices",
        ["NotifyStopped"] = "AudioMirror - stopped",
        ["SetupWizard"] = "Setup wizard",
        ["SetupWizardTitle"] = "AudioMirror setup",
        ["WizardStepSourceTitle"] = "Step 1: choose Source",
        ["WizardStepSourceBody"] = "Source must be the playback device that Windows sends sound to. In Windows sound settings, set this same device as the default output.",
        ["WizardStepTargetsTitle"] = "Step 2: choose speakers",
        ["WizardStepTargetsBody"] = "Targets are the real speakers, monitors, or USB sound devices where AudioMirror will play sound.",
        ["WizardStepTestTitle"] = "Step 3: test and start",
        ["WizardStepTestBody"] = "Use Test speakers to verify left/right/third output. Press Start when the routing is correct.",
        ["WizardSelectSource"] = "Choose a Source device first.",
        ["WizardSelectTargets"] = "Choose Target 1 and Target 2 first.",
        ["WizardDistinctDevices"] = "Source and Targets must be different devices.",
        ["WizardApplied"] = "Setup applied. Press Start or use Test speakers to verify output.",
        ["Back"] = "Back",
        ["Next"] = "Next",
        ["Finish"] = "Finish",
        ["NoAudioReachingSource"] = "No audio is reaching Source. Set Source as the Windows default output, then restart the browser or player.",
        ["TrayHintTitle"] = "AudioMirror is still running",
        ["TrayHintBody"] = "The window was closed to the tray. Use the tray icon to open, stop, or exit AudioMirror."
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Languages = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = English,
        ["ru"] = Merge(("Language", "Язык"), ("File", "Файл"), ("Actions", "Действия"), ("Help", "Справка"), ("Start", "Старт"), ("Stop", "Стоп"), ("Save", "Сохранить"), ("SaveSettings", "Сохранить настройки"), ("Autostart", "Автозапуск"), ("Exit", "Выход"), ("Refresh", "Обновить"), ("RefreshDevices", "Обновить устройства"), ("Sound", "Звук"), ("SoundSettings", "Параметры звука"), ("Sync", "Синхр."), ("Test", "Тест"), ("TestSpeakers", "Тест колонок"), ("UserHelp", "Справка"), ("AboutAudioMirror", "О AudioMirror"), ("OpenAudioMirror", "Открыть AudioMirror"), ("Source", "Источник"), ("Target1", "Цель 1"), ("Target2", "Цель 2"), ("Gain", "Усиление"), ("DelayMs", "Задержка, мс"), ("AutoRestart", "Автоперезапуск"), ("Hint", "Сделайте выбранный Source устройством вывода Windows по умолчанию; если captured frames = 0, перезапустите плеер."), ("Close", "Закрыть"), ("Left", "Лево"), ("Right", "Право"), ("Both", "Оба"), ("Loop", "Цикл"), ("TestTitle", "Тест AudioMirror"), ("TestHint", "Left играет Target 1, Right играет Target 2, Both играет оба выхода."), ("HelpHeading", "Справка"), ("DefaultSuffix", "по умолчанию"), ("UnpluggedSuffix", "отключено"), ("NotPresentSuffix", "нет устройства"), ("Stopped", "Остановлено."), ("ModeStereo", "Стерео-зеркало")),
        ["de"] = Merge(("Language", "Sprache"), ("File", "Datei"), ("Actions", "Aktionen"), ("Help", "Hilfe"), ("Start", "Start"), ("Stop", "Stopp"), ("Save", "Speichern"), ("SaveSettings", "Einstellungen speichern"), ("Autostart", "Autostart"), ("Exit", "Beenden"), ("Refresh", "Aktualisieren"), ("RefreshDevices", "Geräte aktualisieren"), ("Sound", "Audio"), ("SoundSettings", "Audioeinstellungen"), ("Sync", "Sync"), ("Test", "Test"), ("TestSpeakers", "Lautsprecher testen"), ("UserHelp", "Benutzerhilfe"), ("AboutAudioMirror", "Über AudioMirror"), ("OpenAudioMirror", "AudioMirror öffnen"), ("Source", "Quelle"), ("Target1", "Ziel 1"), ("Target2", "Ziel 2"), ("Gain", "Pegel"), ("DelayMs", "Verzögerung ms"), ("AutoRestart", "Auto-Neustart"), ("Hint", "Windows-Standardausgabe auf die Quelle setzen; Player neu starten, wenn captured frames bei 0 bleibt."), ("Close", "Schließen"), ("Left", "Links"), ("Right", "Rechts"), ("Both", "Beide"), ("Loop", "Schleife"), ("DefaultSuffix", "Standard"), ("UnpluggedSuffix", "getrennt"), ("NotPresentSuffix", "nicht vorhanden"), ("Stopped", "Gestoppt.")),
        ["fr"] = Merge(("Language", "Langue"), ("File", "Fichier"), ("Actions", "Actions"), ("Help", "Aide"), ("Start", "Démarrer"), ("Stop", "Arrêter"), ("Save", "Enregistrer"), ("SaveSettings", "Enregistrer les réglages"), ("Autostart", "Démarrage auto"), ("Exit", "Quitter"), ("Refresh", "Actualiser"), ("RefreshDevices", "Actualiser les périphériques"), ("Sound", "Son"), ("SoundSettings", "Paramètres audio"), ("Sync", "Sync"), ("Test", "Test"), ("TestSpeakers", "Tester les haut-parleurs"), ("UserHelp", "Aide utilisateur"), ("AboutAudioMirror", "À propos d'AudioMirror"), ("OpenAudioMirror", "Ouvrir AudioMirror"), ("Source", "Source"), ("Target1", "Sortie 1"), ("Target2", "Sortie 2"), ("Gain", "Gain"), ("DelayMs", "Délai ms"), ("AutoRestart", "Redémarrage auto"), ("Hint", "Définissez la Source comme sortie Windows par défaut; redémarrez le lecteur si captured frames reste à 0."), ("Close", "Fermer"), ("Left", "Gauche"), ("Right", "Droite"), ("Both", "Les deux"), ("Loop", "Boucle"), ("DefaultSuffix", "par défaut"), ("UnpluggedSuffix", "débranché"), ("NotPresentSuffix", "absent"), ("Stopped", "Arrêté.")),
        ["es"] = Merge(("Language", "Idioma"), ("File", "Archivo"), ("Actions", "Acciones"), ("Help", "Ayuda"), ("Start", "Iniciar"), ("Stop", "Detener"), ("Save", "Guardar"), ("SaveSettings", "Guardar ajustes"), ("Autostart", "Inicio auto"), ("Exit", "Salir"), ("Refresh", "Actualizar"), ("RefreshDevices", "Actualizar dispositivos"), ("Sound", "Sonido"), ("SoundSettings", "Ajustes de sonido"), ("Sync", "Sincronizar"), ("Test", "Prueba"), ("TestSpeakers", "Probar altavoces"), ("UserHelp", "Ayuda"), ("AboutAudioMirror", "Acerca de AudioMirror"), ("OpenAudioMirror", "Abrir AudioMirror"), ("Source", "Fuente"), ("Target1", "Destino 1"), ("Target2", "Destino 2"), ("Gain", "Ganancia"), ("DelayMs", "Retardo ms"), ("AutoRestart", "Reinicio auto"), ("Hint", "Configure Source como salida predeterminada de Windows; reinicie el reproductor si captured frames queda en 0."), ("Close", "Cerrar"), ("Left", "Izq."), ("Right", "Der."), ("Both", "Ambos"), ("Loop", "Bucle"), ("DefaultSuffix", "predeterminado"), ("UnpluggedSuffix", "desconectado"), ("NotPresentSuffix", "no presente"), ("Stopped", "Detenido.")),
        ["it"] = Merge(("Language", "Lingua"), ("File", "File"), ("Actions", "Azioni"), ("Help", "Aiuto"), ("Start", "Avvia"), ("Stop", "Stop"), ("Save", "Salva"), ("SaveSettings", "Salva impostazioni"), ("Autostart", "Avvio auto"), ("Exit", "Esci"), ("Refresh", "Aggiorna"), ("RefreshDevices", "Aggiorna dispositivi"), ("Sound", "Audio"), ("SoundSettings", "Impostazioni audio"), ("Sync", "Sync"), ("Test", "Test"), ("TestSpeakers", "Test altoparlanti"), ("UserHelp", "Guida"), ("AboutAudioMirror", "Informazioni su AudioMirror"), ("OpenAudioMirror", "Apri AudioMirror"), ("Source", "Sorgente"), ("Target1", "Uscita 1"), ("Target2", "Uscita 2"), ("Gain", "Guadagno"), ("DelayMs", "Ritardo ms"), ("AutoRestart", "Riavvio auto"), ("Hint", "Imposta Source come uscita predefinita di Windows; riavvia il player se captured frames resta a 0."), ("Close", "Chiudi"), ("Left", "Sinistra"), ("Right", "Destra"), ("Both", "Entrambi"), ("Loop", "Ciclo"), ("DefaultSuffix", "predefinito"), ("UnpluggedSuffix", "scollegato"), ("NotPresentSuffix", "non presente"), ("Stopped", "Fermo.")),
        ["pt"] = Merge(("Language", "Idioma"), ("File", "Ficheiro"), ("Actions", "Ações"), ("Help", "Ajuda"), ("Start", "Iniciar"), ("Stop", "Parar"), ("Save", "Guardar"), ("SaveSettings", "Guardar definições"), ("Autostart", "Início auto"), ("Exit", "Sair"), ("Refresh", "Atualizar"), ("RefreshDevices", "Atualizar dispositivos"), ("Sound", "Som"), ("SoundSettings", "Definições de som"), ("Sync", "Sincronizar"), ("Test", "Teste"), ("TestSpeakers", "Testar colunas"), ("UserHelp", "Ajuda"), ("AboutAudioMirror", "Sobre o AudioMirror"), ("OpenAudioMirror", "Abrir AudioMirror"), ("Source", "Fonte"), ("Target1", "Saída 1"), ("Target2", "Saída 2"), ("Gain", "Ganho"), ("DelayMs", "Atraso ms"), ("AutoRestart", "Reinício auto"), ("Hint", "Defina Source como saída predefinida do Windows; reinicie o leitor se captured frames ficar em 0."), ("Close", "Fechar"), ("Left", "Esq."), ("Right", "Dir."), ("Both", "Ambos"), ("Loop", "Ciclo"), ("DefaultSuffix", "predefinido"), ("UnpluggedSuffix", "desligado"), ("NotPresentSuffix", "não presente"), ("Stopped", "Parado.")),
        ["pl"] = Merge(("Language", "Język"), ("File", "Plik"), ("Actions", "Akcje"), ("Help", "Pomoc"), ("Start", "Start"), ("Stop", "Stop"), ("Save", "Zapisz"), ("SaveSettings", "Zapisz ustawienia"), ("Autostart", "Autostart"), ("Exit", "Wyjdź"), ("Refresh", "Odśwież"), ("RefreshDevices", "Odśwież urządzenia"), ("Sound", "Dźwięk"), ("SoundSettings", "Ustawienia dźwięku"), ("Sync", "Sync"), ("Test", "Test"), ("TestSpeakers", "Test głośników"), ("UserHelp", "Pomoc"), ("AboutAudioMirror", "O AudioMirror"), ("OpenAudioMirror", "Otwórz AudioMirror"), ("Source", "Źródło"), ("Target1", "Cel 1"), ("Target2", "Cel 2"), ("Gain", "Wzmocn."), ("DelayMs", "Opóźn. ms"), ("AutoRestart", "Auto restart"), ("Hint", "Ustaw Source jako domyślne wyjście Windows; uruchom odtwarzacz ponownie, jeśli captured frames = 0."), ("Close", "Zamknij"), ("Left", "Lewy"), ("Right", "Prawy"), ("Both", "Oba"), ("Loop", "Pętla"), ("DefaultSuffix", "domyślne"), ("UnpluggedSuffix", "odłączone"), ("NotPresentSuffix", "brak"), ("Stopped", "Zatrzymano.")),
        ["nl"] = Merge(("Language", "Taal"), ("File", "Bestand"), ("Actions", "Acties"), ("Help", "Help"), ("Start", "Start"), ("Stop", "Stop"), ("Save", "Opslaan"), ("SaveSettings", "Instellingen opslaan"), ("Autostart", "Autostart"), ("Exit", "Afsluiten"), ("Refresh", "Vernieuwen"), ("RefreshDevices", "Apparaten vernieuwen"), ("Sound", "Geluid"), ("SoundSettings", "Geluidsinstellingen"), ("Sync", "Sync"), ("Test", "Test"), ("TestSpeakers", "Speakers testen"), ("UserHelp", "Gebruikershulp"), ("AboutAudioMirror", "Over AudioMirror"), ("OpenAudioMirror", "AudioMirror openen"), ("Source", "Bron"), ("Target1", "Doel 1"), ("Target2", "Doel 2"), ("Gain", "Gain"), ("DelayMs", "Vertraging ms"), ("AutoRestart", "Auto herstart"), ("Hint", "Stel Source in als standaarduitvoer van Windows; herstart de speler als captured frames op 0 blijft."), ("Close", "Sluiten"), ("Left", "Links"), ("Right", "Rechts"), ("Both", "Beide"), ("Loop", "Lus"), ("DefaultSuffix", "standaard"), ("UnpluggedSuffix", "losgekoppeld"), ("NotPresentSuffix", "niet aanwezig"), ("Stopped", "Gestopt.")),
        ["zh-Hans"] = Merge(("Language", "语言"), ("File", "文件"), ("Actions", "操作"), ("Help", "帮助"), ("Start", "开始"), ("Stop", "停止"), ("Save", "保存"), ("SaveSettings", "保存设置"), ("Autostart", "开机启动"), ("Exit", "退出"), ("Refresh", "刷新"), ("RefreshDevices", "刷新设备"), ("Sound", "声音"), ("SoundSettings", "声音设置"), ("Sync", "同步"), ("Test", "测试"), ("TestSpeakers", "测试扬声器"), ("UserHelp", "用户帮助"), ("AboutAudioMirror", "关于 AudioMirror"), ("OpenAudioMirror", "打开 AudioMirror"), ("Source", "源"), ("Target1", "目标 1"), ("Target2", "目标 2"), ("Gain", "增益"), ("DelayMs", "延迟 ms"), ("AutoRestart", "自动重启"), ("Hint", "将所选 Source 设为 Windows 默认输出；如果 captured frames 保持 0，请重启播放器。"), ("Close", "关闭"), ("Left", "左"), ("Right", "右"), ("Both", "两者"), ("Loop", "循环"), ("DefaultSuffix", "默认"), ("UnpluggedSuffix", "未插入"), ("NotPresentSuffix", "不存在"), ("Stopped", "已停止。")),
        ["ja"] = Merge(("Language", "言語"), ("File", "ファイル"), ("Actions", "操作"), ("Help", "ヘルプ"), ("Start", "開始"), ("Stop", "停止"), ("Save", "保存"), ("SaveSettings", "設定を保存"), ("Autostart", "自動起動"), ("Exit", "終了"), ("Refresh", "更新"), ("RefreshDevices", "デバイス更新"), ("Sound", "サウンド"), ("SoundSettings", "サウンド設定"), ("Sync", "同期"), ("Test", "テスト"), ("TestSpeakers", "スピーカーテスト"), ("UserHelp", "ヘルプ"), ("AboutAudioMirror", "AudioMirror について"), ("OpenAudioMirror", "AudioMirror を開く"), ("Source", "ソース"), ("Target1", "出力 1"), ("Target2", "出力 2"), ("Gain", "ゲイン"), ("DelayMs", "遅延 ms"), ("AutoRestart", "自動再起動"), ("Hint", "選択した Source を Windows の既定出力にしてください。captured frames が 0 のままならプレーヤーを再起動してください。"), ("Close", "閉じる"), ("Left", "左"), ("Right", "右"), ("Both", "両方"), ("Loop", "ループ"), ("DefaultSuffix", "既定"), ("UnpluggedSuffix", "未接続"), ("NotPresentSuffix", "存在しません"), ("Stopped", "停止中。"))
    };

    public static readonly IReadOnlyList<Language> Options =
    [
        new("en", "English"),
        new("ru", "Русский"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("es", "Español"),
        new("it", "Italiano"),
        new("pt", "Português"),
        new("pl", "Polski"),
        new("nl", "Nederlands"),
        new("zh-Hans", "中文"),
        new("ja", "日本語")
    ];

    public static string CurrentCode { get; private set; } = "en";

    static AppText()
    {
        Add("ru",
            ("HelpTitle", "Справка AudioMirror"),
            ("HelpBody", "Быстрая настройка\n\n1. Сделайте свободное устройство вывода устройством Windows по умолчанию.\n2. Выберите его как Source в AudioMirror.\n3. Выберите реальные колонки или мониторы как Target 1 и Target 2.\n4. Нажмите Старт.\n\nЭлементы управления\n\nУсиление - множитель громкости. 1.0 означает без изменений.\nЗадержка, мс добавляет задержку к одному выходу, чтобы уменьшить эхо.\nSplit L/R отправляет левый канал на Target 1, а правый канал на Target 2.\nSync применяет безопасные настройки в приложении, но не меняет драйвер Windows.\nАвтоперезапуск пересоздает аудиопотоки после сна, отключения экрана или сброса устройства.\nТест открывает встроенную проверку колонок.\n\nЕсли captured frames остается 0, Source не получает звук. Сделайте его выводом Windows по умолчанию и перезапустите плеер."),
            ("AboutTitle", "О AudioMirror"),
            ("AboutBody", "AudioMirror\n\nНебольшая WASAPI-утилита для Windows, которая зеркалирует один аудиопоток на два устройства вывода с отдельным усилением, задержкой, разделением каналов и встроенным тестом колонок.\n\nCopyright (c) 2026 AudioMirror contributors\n\nРепозиторий\nhttps://github.com/rausNT/audio-mirror-windows\n\nЛицензия\nMIT License\n\nПрограмма предоставляется как есть, без каких-либо гарантий."),
            ("TestHint", "Left играет Target 1, Right играет Target 2, Both играет оба выхода."));

        Add("de",
            ("HelpTitle", "AudioMirror Hilfe"),
            ("HelpHeading", "Hilfe"),
            ("HelpBody", "Schnelle Einrichtung\n\n1. Legen Sie ein ungenutztes Wiedergabegerät als Windows-Standardausgabe fest.\n2. Wählen Sie dieses Gerät in AudioMirror als Quelle.\n3. Wählen Sie zwei echte Lautsprecher oder Monitore als Ziel 1 und Ziel 2.\n4. Klicken Sie auf Start.\n\nBedienung\n\nPegel ist ein Lautstärkemultiplikator. 1,0 bedeutet unverändert.\nVerzögerung ms verzögert ein Ziel, um Echo zu verringern.\nSplit L/R sendet den linken Kanal an Ziel 1 und den rechten Kanal an Ziel 2.\nSync setzt sichere App-Werte, ändert aber keine Windows-Treibereinstellungen.\nAuto-Neustart erstellt Audiostreams nach Standby, Display-Abschaltung oder Geräte-Reset neu.\nTest öffnet den eingebauten Lautsprechertest.\n\nWenn captured frames bei 0 bleibt, empfängt die Quelle kein Audio. Setzen Sie sie als Windows-Standardausgabe und starten Sie den Player neu."),
            ("AboutTitle", "Über AudioMirror"),
            ("AboutBody", "AudioMirror\n\nEin kleines Windows-WASAPI-Werkzeug, das einen Wiedergabestream auf zwei Ausgabegeräte spiegelt, mit getrenntem Pegel, Verzögerung, Kanalaufteilung und eingebautem Lautsprechertest.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepository\nhttps://github.com/rausNT/audio-mirror-windows\n\nLizenz\nMIT License\n\nDie Software wird ohne Gewährleistung jeglicher Art bereitgestellt."),
            ("TestTitle", "AudioMirror Test"),
            ("TestHint", "Links spielt Ziel 1, Rechts spielt Ziel 2, Beide spielt beide Ziele."));

        Add("fr",
            ("HelpTitle", "Aide AudioMirror"),
            ("HelpHeading", "Aide"),
            ("HelpBody", "Configuration rapide\n\n1. Définissez un périphérique de lecture inutilisé comme sortie Windows par défaut.\n2. Choisissez ce périphérique comme Source dans AudioMirror.\n3. Choisissez deux enceintes ou moniteurs réels comme Sortie 1 et Sortie 2.\n4. Cliquez sur Démarrer.\n\nCommandes\n\nGain est un multiplicateur de volume. 1,0 signifie aucun changement.\nDélai ms ajoute un retard à une sortie pour réduire l'écho.\nSplit L/R envoie le canal gauche vers Sortie 1 et le canal droit vers Sortie 2.\nSync applique des valeurs sûres côté application, sans modifier les pilotes Windows.\nRedémarrage auto recrée les flux audio après veille, extinction de l'écran ou réinitialisation d'un périphérique.\nTest ouvre le test d'enceintes intégré.\n\nSi captured frames reste à 0, la Source ne reçoit pas d'audio. Définissez-la comme sortie Windows par défaut et redémarrez le lecteur."),
            ("AboutTitle", "À propos d'AudioMirror"),
            ("AboutBody", "AudioMirror\n\nPetit utilitaire Windows WASAPI qui duplique un flux de lecture vers deux périphériques de sortie, avec gain, délai, séparation des canaux et test d'enceintes intégré.\n\nCopyright (c) 2026 AudioMirror contributors\n\nDépôt\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicence\nMIT License\n\nLe logiciel est fourni tel quel, sans garantie d'aucune sorte."),
            ("TestTitle", "Test AudioMirror"),
            ("TestHint", "Gauche joue Sortie 1, Droite joue Sortie 2, Les deux joue les deux sorties."));

        Add("es",
            ("HelpTitle", "Ayuda de AudioMirror"),
            ("HelpHeading", "Ayuda"),
            ("HelpBody", "Configuración rápida\n\n1. Ponga un dispositivo de reproducción sin uso como salida predeterminada de Windows.\n2. Elija ese dispositivo como Fuente en AudioMirror.\n3. Elija dos altavoces o monitores reales como Destino 1 y Destino 2.\n4. Pulse Iniciar.\n\nControles\n\nGanancia es un multiplicador de volumen. 1,0 no cambia el nivel.\nRetardo ms añade retardo a un destino para reducir el eco.\nSplit L/R envía el canal izquierdo al Destino 1 y el derecho al Destino 2.\nSincronizar aplica valores seguros en la app, pero no cambia el controlador de Windows.\nReinicio auto recrea los flujos tras suspensión, apagado de pantalla o reinicio del dispositivo.\nPrueba abre la prueba integrada de altavoces.\n\nSi captured frames queda en 0, la Fuente no recibe audio. Póngala como salida predeterminada de Windows y reinicie el reproductor."),
            ("AboutTitle", "Acerca de AudioMirror"),
            ("AboutBody", "AudioMirror\n\nPequeña utilidad WASAPI para Windows que duplica un flujo de reproducción en dos dispositivos de salida, con ganancia, retardo, separación de canales y prueba integrada de altavoces.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepositorio\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicencia\nMIT License\n\nEl software se proporciona tal cual, sin garantía de ningún tipo."),
            ("TestTitle", "Prueba de AudioMirror"),
            ("TestHint", "Izq. reproduce Destino 1, Der. reproduce Destino 2, Ambos reproduce ambos destinos."));

        Add("it",
            ("HelpTitle", "Guida AudioMirror"),
            ("HelpHeading", "Guida"),
            ("HelpBody", "Configurazione rapida\n\n1. Imposta un dispositivo di riproduzione inutilizzato come uscita predefinita di Windows.\n2. Scegli quel dispositivo come Sorgente in AudioMirror.\n3. Scegli due altoparlanti o monitor reali come Uscita 1 e Uscita 2.\n4. Premi Avvia.\n\nControlli\n\nGuadagno è un moltiplicatore di volume. 1,0 significa invariato.\nRitardo ms aggiunge ritardo a un'uscita per ridurre l'eco.\nSplit L/R invia il canale sinistro a Uscita 1 e il destro a Uscita 2.\nSync applica valori sicuri nell'app, ma non cambia i driver Windows.\nRiavvio auto ricrea i flussi audio dopo sospensione, spegnimento display o reset del dispositivo.\nTest apre il test altoparlanti integrato.\n\nSe captured frames resta a 0, la Sorgente non riceve audio. Impostala come uscita predefinita di Windows e riavvia il player."),
            ("AboutTitle", "Informazioni su AudioMirror"),
            ("AboutBody", "AudioMirror\n\nPiccola utilità WASAPI per Windows che duplica un flusso di riproduzione su due dispositivi di uscita, con guadagno, ritardo, separazione canali e test altoparlanti integrato.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepository\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicenza\nMIT License\n\nIl software è fornito così com'è, senza alcuna garanzia."),
            ("TestTitle", "Test AudioMirror"),
            ("TestHint", "Sinistra riproduce Uscita 1, Destra riproduce Uscita 2, Entrambi riproduce entrambe."));

        Add("pt",
            ("HelpTitle", "Ajuda do AudioMirror"),
            ("HelpHeading", "Ajuda"),
            ("HelpBody", "Configuração rápida\n\n1. Defina um dispositivo de reprodução livre como saída predefinida do Windows.\n2. Escolha esse dispositivo como Fonte no AudioMirror.\n3. Escolha duas colunas ou monitores reais como Saída 1 e Saída 2.\n4. Prima Iniciar.\n\nControlos\n\nGanho é um multiplicador de volume. 1,0 significa sem alteração.\nAtraso ms adiciona atraso a uma saída para reduzir eco.\nSplit L/R envia o canal esquerdo para Saída 1 e o direito para Saída 2.\nSincronizar aplica valores seguros na aplicação, mas não altera o controlador do Windows.\nReinício auto recria os fluxos após suspensão, ecrã desligado ou reposição do dispositivo.\nTeste abre o teste integrado das colunas.\n\nSe captured frames ficar em 0, a Fonte não recebe áudio. Defina-a como saída predefinida do Windows e reinicie o leitor."),
            ("AboutTitle", "Sobre o AudioMirror"),
            ("AboutBody", "AudioMirror\n\nPequena utilidade WASAPI para Windows que duplica um fluxo de reprodução para dois dispositivos de saída, com ganho, atraso, separação de canais e teste integrado das colunas.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepositório\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicença\nMIT License\n\nO software é fornecido tal como está, sem garantia de qualquer tipo."),
            ("TestTitle", "Teste do AudioMirror"),
            ("TestHint", "Esq. toca Saída 1, Dir. toca Saída 2, Ambos toca as duas saídas."));

        Add("pl",
            ("HelpTitle", "Pomoc AudioMirror"),
            ("HelpHeading", "Pomoc"),
            ("HelpBody", "Szybka konfiguracja\n\n1. Ustaw nieużywane urządzenie odtwarzania jako domyślne wyjście Windows.\n2. Wybierz je jako Źródło w AudioMirror.\n3. Wybierz dwa prawdziwe głośniki lub monitory jako Cel 1 i Cel 2.\n4. Naciśnij Start.\n\nSterowanie\n\nWzmocn. to mnożnik głośności. 1,0 oznacza bez zmian.\nOpóźn. ms dodaje opóźnienie do jednego celu, aby zmniejszyć echo.\nSplit L/R wysyła lewy kanał do Celu 1, a prawy do Celu 2.\nSync stosuje bezpieczne wartości w aplikacji, ale nie zmienia ustawień sterownika Windows.\nAuto restart odtwarza strumienie po uśpieniu, wyłączeniu ekranu lub resecie urządzenia.\nTest otwiera wbudowany test głośników.\n\nJeśli captured frames zostaje na 0, Źródło nie otrzymuje dźwięku. Ustaw je jako domyślne wyjście Windows i uruchom odtwarzacz ponownie."),
            ("AboutTitle", "O AudioMirror"),
            ("AboutBody", "AudioMirror\n\nMałe narzędzie WASAPI dla Windows, które kopiuje jeden strumień odtwarzania na dwa urządzenia wyjściowe, z osobnym wzmocnieniem, opóźnieniem, podziałem kanałów i wbudowanym testem głośników.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepozytorium\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicencja\nMIT License\n\nOprogramowanie jest dostarczane takie, jakie jest, bez jakiejkolwiek gwarancji."),
            ("TestTitle", "Test AudioMirror"),
            ("TestHint", "Lewy odtwarza Cel 1, Prawy odtwarza Cel 2, Oba odtwarza oba cele."));

        Add("nl",
            ("HelpTitle", "AudioMirror Help"),
            ("HelpHeading", "Help"),
            ("HelpBody", "Snelle instelling\n\n1. Stel een ongebruikt afspeelapparaat in als standaarduitvoer van Windows.\n2. Kies dat apparaat als Bron in AudioMirror.\n3. Kies twee echte speakers of monitoren als Doel 1 en Doel 2.\n4. Klik op Start.\n\nBediening\n\nGain is een volumevermenigvuldiger. 1,0 betekent ongewijzigd.\nVertraging ms voegt vertraging toe aan een doel om echo te verminderen.\nSplit L/R stuurt het linkerkanaal naar Doel 1 en het rechterkanaal naar Doel 2.\nSync past veilige app-waarden toe, maar wijzigt geen Windows-driverinstellingen.\nAuto herstart maakt audiostreams opnieuw na slaapstand, scherm uit of een reset van het apparaat.\nTest opent de ingebouwde speakertest.\n\nAls captured frames op 0 blijft, ontvangt de Bron geen audio. Stel hem in als standaarduitvoer van Windows en herstart de speler."),
            ("AboutTitle", "Over AudioMirror"),
            ("AboutBody", "AudioMirror\n\nEen klein Windows WASAPI-hulpmiddel dat één afspeelstream spiegelt naar twee uitvoerapparaten, met aparte gain, vertraging, kanaalsplitsing en ingebouwde speakertest.\n\nCopyright (c) 2026 AudioMirror contributors\n\nRepository\nhttps://github.com/rausNT/audio-mirror-windows\n\nLicentie\nMIT License\n\nDe software wordt geleverd zoals deze is, zonder enige garantie."),
            ("TestTitle", "AudioMirror Test"),
            ("TestHint", "Links speelt Doel 1, Rechts speelt Doel 2, Beide speelt beide doelen."));

        Add("zh-Hans",
            ("HelpTitle", "AudioMirror 帮助"),
            ("HelpHeading", "帮助"),
            ("HelpBody", "快速设置\n\n1. 将一个未使用的播放设备设为 Windows 默认输出。\n2. 在 AudioMirror 中把该设备选为源。\n3. 将两个真实扬声器或显示器选为目标 1 和目标 2。\n4. 点击开始。\n\n控件\n\n增益是音量倍数。1.0 表示不改变。\n延迟 ms 会给一个目标增加延迟，用于减少回声。\nSplit L/R 将左声道发送到目标 1，将右声道发送到目标 2。\n同步会应用安全的应用内设置，但不会修改 Windows 驱动设置。\n自动重启会在睡眠、屏幕关闭或设备重置后重新创建音频流。\n测试会打开内置扬声器测试。\n\n如果 captured frames 一直为 0，说明源没有收到音频。请将其设为 Windows 默认输出并重启播放器。"),
            ("AboutTitle", "关于 AudioMirror"),
            ("AboutBody", "AudioMirror\n\n一个小型 Windows WASAPI 工具，可将一个播放流镜像到两个输出设备，并支持独立增益、延迟、声道分离和内置扬声器测试。\n\nCopyright (c) 2026 AudioMirror contributors\n\n仓库\nhttps://github.com/rausNT/audio-mirror-windows\n\n许可证\nMIT License\n\n本软件按原样提供，不附带任何形式的担保。"),
            ("TestTitle", "AudioMirror 测试"),
            ("TestHint", "左播放目标 1，右播放目标 2，两者同时播放两个目标。"));

        Add("ja",
            ("HelpTitle", "AudioMirror ヘルプ"),
            ("HelpHeading", "ヘルプ"),
            ("HelpBody", "クイック設定\n\n1. 未使用の再生デバイスを Windows の既定出力にします。\n2. AudioMirror でそのデバイスをソースに選びます。\n3. 実際のスピーカーまたはモニターを出力 1 と出力 2 に選びます。\n4. 開始を押します。\n\n操作\n\nゲインは音量倍率です。1.0 は変更なしです。\n遅延 ms は片方の出力を遅らせ、エコーを減らします。\nSplit L/R は左チャンネルを出力 1、右チャンネルを出力 2 に送ります。\n同期はアプリ側の安全な値を適用しますが、Windows ドライバー設定は変更しません。\n自動再起動はスリープ、画面オフ、デバイスリセット後に音声ストリームを作り直します。\nテストは内蔵スピーカーテストを開きます。\n\ncaptured frames が 0 のままなら、ソースが音声を受け取っていません。Windows の既定出力にして、プレーヤーを再起動してください。"),
            ("AboutTitle", "AudioMirror について"),
            ("AboutBody", "AudioMirror\n\n1 つの再生ストリームを 2 つの出力デバイスへミラーする小さな Windows WASAPI ユーティリティです。出力ごとのゲイン、遅延、チャンネル分離、内蔵スピーカーテストに対応します。\n\nCopyright (c) 2026 AudioMirror contributors\n\nリポジトリ\nhttps://github.com/rausNT/audio-mirror-windows\n\nライセンス\nMIT License\n\n本ソフトウェアは現状のまま提供され、いかなる保証もありません。"),
            ("TestTitle", "AudioMirror テスト"),
            ("TestHint", "左は出力 1、右は出力 2、両方は両方の出力を再生します。"));
    }

    public static void SetLanguage(string? code)
    {
        CurrentCode = Languages.ContainsKey(code ?? "") ? code! : "en";
    }

    public static string T(string key)
    {
        return Languages.TryGetValue(CurrentCode, out var language) && language.TryGetValue(key, out var value)
            ? value
            : English.TryGetValue(key, out var fallback)
                ? fallback
                : key;
    }

    public static string F(string key, params object?[] args)
    {
        return string.Format(T(key), args);
    }

    private static Dictionary<string, string> Merge(params (string Key, string Value)[] translations)
    {
        var result = new Dictionary<string, string>(English, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in translations)
        {
            result[key] = value;
        }

        return result;
    }

    private static void Add(string code, params (string Key, string Value)[] translations)
    {
        if (!Languages.TryGetValue(code, out var language))
        {
            return;
        }

        foreach (var (key, value) in translations)
        {
            language[key] = value;
        }
    }
}
