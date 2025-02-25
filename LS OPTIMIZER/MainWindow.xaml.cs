using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LS_OPTIMIZER
{
    public partial class LSOptimizer : Window
    {
        private static Mutex mutex = new Mutex(true, "{B1A2F3C4-D5E6-7890-1234-56789ABCDEF0}");
        private bool isOptimizing = false;
        public string UserName { get; set; } = string.Empty;

        private DispatcherTimer memoryUpdateTimer = new DispatcherTimer();
        private DispatcherTimer _timer = new DispatcherTimer();
        private DispatcherTimer autoCleanTimer = new DispatcherTimer();
        private int _dotCount = 0;
        private string logFilePath = string.Empty;
        private string? currentLogFileName;
        private NotifyIcon notifyIcon = new NotifyIcon();
        private string configFilePath = string.Empty;
        private Config config = new Config();

        public LSOptimizer()
        {
            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                System.Windows.MessageBox.Show("O LS OPTIMIZER já está em execução.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Windows.Application.Current.Shutdown();
                return;
            }

            InitializeComponent();

            this.Title = "LS OPTIMIZER";

            UserName = Environment.UserName; // Initialize UserName
            this.DataContext = this;

            string baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ls Optimizer");
            string userDirectory = Path.Combine(baseDirectory, "User", UserName);

            // Verifica se o diretório "Ls Optimizer\User\{UserName}" existe; caso contrário, cria
            if (!Directory.Exists(userDirectory))
            {
                Directory.CreateDirectory(userDirectory);
            }

            logFilePath = Path.Combine(baseDirectory, "logs");

            // Verifica se o diretório "Ls Optimizer\logs" existe; caso contrário, cria
            if (!Directory.Exists(logFilePath))
            {
                Directory.CreateDirectory(logFilePath);
            }

            configFilePath = Path.Combine(userDirectory, "config.json");
            config = new Config(); // Initialize config to avoid nullability issues
            LoadConfig();

            memoryUpdateTimer = new DispatcherTimer();
            memoryUpdateTimer.Interval = TimeSpan.FromSeconds(1);
            memoryUpdateTimer.Tick += MemoryUpdateTimer_Tick;
            memoryUpdateTimer.Start();

            notifyIcon = new NotifyIcon();

            // Verifica os argumentos de linha de comando
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == "startup")
            {
                // Iniciado com o Windows, minimiza para a bandeja do sistema
                this.Hide();
                SetupTrayIcon();
            }
            else
            {
                // Iniciado normalmente, mostra a janela
                this.Show();
            }

            SetupAutoCleanTimer();
        }






        private void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                config = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            }
            else
            {
                config = new Config();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            string json = JsonSerializer.Serialize(config);
            File.WriteAllText(configFilePath, json);
        }

        private void Restore(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;

            // Oculta e remove o ícone da bandeja
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }


        private void SetupTrayIcon()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = Path.Combine(baseDirectory, "Assets", "roda-dentada.ico");

                if (!File.Exists(iconPath))
                {
                    LogMessage($"Erro: O arquivo de ícone não foi encontrado no caminho: {iconPath}");
                    return;
                }

                notifyIcon = new NotifyIcon();
                notifyIcon.Icon = new System.Drawing.Icon(iconPath); // Caminho correto do ícone
                notifyIcon.Visible = true;

                var contextMenu = new ContextMenuStrip();
                var restoreMenuItem = new ToolStripMenuItem("Desminimizar", null, Restore);
                var startWithWindowsMenuItem = new ToolStripMenuItem("Iniciar com o Windows", null, ToggleStartWithWindows);
                var limpararqvMenuItem = new ToolStripMenuItem("Limpar temporarios", null, LimparTemporarios);
                startWithWindowsMenuItem.Checked = config.StartWithWindows;
                var closeMenuItem = new ToolStripMenuItem("Fechar", null, CloseApp);

                var autoCleanMenuItem = new ToolStripMenuItem("Limpar arquivos a cada");
                var intervals = new[] { "Nenhum", "1 min", "5 min", "10 min", "15 min", "30 min", "60 min" };
                foreach (var interval in intervals)
                {
                    var item = new ToolStripMenuItem(interval, null, SetAutoCleanInterval);
                    item.Tag = interval;
                    if (interval == config.AutoCleanInterval)
                    {
                        item.Checked = true;
                    }
                    autoCleanMenuItem.DropDownItems.Add(item);
                }

                contextMenu.Items.Add(restoreMenuItem);
                contextMenu.Items.Add(startWithWindowsMenuItem);
                contextMenu.Items.Add(limpararqvMenuItem);
                contextMenu.Items.Add(autoCleanMenuItem);
                contextMenu.Items.Add(closeMenuItem);

                notifyIcon.ContextMenuStrip = contextMenu;
                notifyIcon.DoubleClick += Restore;

                LogMessage("Ícone da bandeja do sistema configurado com sucesso.");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao configurar o ícone da bandeja do sistema: {ex.Message}");
            }
        }

        private void SetAutoCleanInterval(object? sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                string selectedInterval = menuItem.Tag as string ?? "Nenhum";
                LogMessage($"Intervalo de limpeza automática selecionado: {selectedInterval}");
                config.AutoCleanInterval = selectedInterval;
                SaveConfig();
                SetupAutoCleanTimer();
                UpdateAutoCleanMenuItems(menuItem);
            }
        }

        private void UpdateAutoCleanMenuItems(ToolStripMenuItem selectedItem)
        {
            if (notifyIcon.ContextMenuStrip != null)
            {
                foreach (ToolStripMenuItem item in notifyIcon.ContextMenuStrip.Items)
                {
                    if (item.DropDownItems.Count > 0)
                    {
                        foreach (ToolStripMenuItem subItem in item.DropDownItems)
                        {
                            subItem.Checked = subItem == selectedItem;
                        }
                    }
                }
            }
        }

        private void SetupAutoCleanTimer()
        {
            autoCleanTimer.Stop();
            if (config.AutoCleanInterval != "Nenhum")
            {
                int intervalMinutes = config.AutoCleanInterval switch
                {
                    "1 min" => 1,
                    "5 min" => 5,
                    "10 min" => 10,
                    "15 min" => 15,
                    "30 min" => 30,
                    "60 min" => 60,
                    _ => 0
                };
                autoCleanTimer.Interval = TimeSpan.FromMinutes(intervalMinutes);
                autoCleanTimer.Tick += (s, e) => LimparTemporarios(s, e);
                autoCleanTimer.Start();
                LogMessage($"Timer de limpeza automática configurado para {config.AutoCleanInterval}.");
            }
            else
            {
                LogMessage("Limpeza automática desativada.");
            }
        }










        private void LimparTemporarios(object? sender, EventArgs e)
        {
            LogMessage("Iniciando limpeza de arquivos temporários.");
            LimparTemp();
            LimparPreFetch();
            LimparRecent();
            LimparCache();
            LimparCachesAdicionais();
            LogMessage("Limpeza de arquivos temporários concluída.");
        }

        private void ToggleStartWithWindows(object? sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null)
            {
                menuItem.Checked = !menuItem.Checked;
                config.StartWithWindows = menuItem.Checked;
                SaveConfig();

                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                string taskName = "LSOptimizer";

                if (config.StartWithWindows)
                {
                    // Adiciona a entrada no agendador de tarefas para iniciar com o Windows
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/create /f /tn \"{taskName}\" /tr \"\\\"{exePath}\\\" startup\" /sc onlogon /rl highest /it",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    try
                    {
                        using (Process? process = Process.Start(processInfo))
                        {
                            if (process != null)
                            {
                                process.WaitForExit();
                                if (process.ExitCode != 0)
                                {
                                    string error = process.StandardError.ReadToEnd();
                                    LogMessage($"Erro ao criar tarefa agendada: {error}");
                                }
                            }
                            else
                            {
                                LogMessage("Erro: Não foi possível iniciar o processo.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao criar tarefa agendada: {ex.Message}");
                    }

                    // Adiciona a entrada no Registro do Windows para iniciar com o Windows
                    try
                    {
                        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null)
                            {
                                key.SetValue(taskName, $"\"{exePath}\" startup");
                            }
                            else
                            {
                                LogMessage("Erro: Não foi possível abrir a chave do Registro.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao adicionar entrada no Registro: {ex.Message}");
                    }
                }
                else
                {
                    // Remove a entrada do agendador de tarefas
                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/delete /f /tn \"{taskName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    try
                    {
                        using (Process? process = Process.Start(processInfo))
                        {
                            if (process != null)
                            {
                                process.WaitForExit();
                                if (process.ExitCode != 0)
                                {
                                    string error = process.StandardError.ReadToEnd();
                                    LogMessage($"Erro ao remover tarefa agendada: {error}");
                                }
                            }
                            else
                            {
                                LogMessage("Erro: Não foi possível iniciar o processo.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao remover tarefa agendada: {ex.Message}");
                    }

                    // Remove a entrada do Registro do Windows
                    try
                    {
                        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue(taskName, false);
                            }
                            else
                            {
                                LogMessage("Erro: Não foi possível abrir a chave do Registro.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao remover entrada do Registro: {ex.Message}");
                    }
                }
            }
        }

        private void CloseApp(object? sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            System.Windows.Application.Current.Shutdown();
        }

        private void MemoryUpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateMemoryInfo();
        }

        private void UpdateMemoryInfo()
        {
            double totalMemoryGB = MemoryCleaner.GetTotalMemory() / 1024.0;
            double freeMemoryGB = MemoryCleaner.GetAvailableMemory() / 1024.0;
            double cacheSizeGB = MemoryCleaner.GetAvailableMemory() / 1024.0;

            totalSystemMemoryText.Text = string.Concat("Memória total do sistema: ", totalMemoryGB.ToString("F2"), " GB");
            freeMemoryText.Text = string.Concat("Memória livre: ", freeMemoryGB.ToString("F2"), " GB");
            cacheSizeText.Text = string.Concat("Cache: ", cacheSizeGB.ToString("F2"), " GB");
        }

        private void Github(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/Luqtta",
                UseShellExecute = true
            });
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.Hide();
            SetupTrayIcon();
        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OtimizarButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isOptimizing)
            {
                isOptimizing = true;
                ButtonOtimizar.IsEnabled = false;
                ButtonOtimizar.Background = new SolidColorBrush(Color.FromRgb(103, 118, 129));

                if (!config.RememberRestorePointChoice)
                {
                    var confirmationWindow = new RestorePointConfirmationWindow();
                    if (confirmationWindow.ShowDialog() == true)
                    {
                        if (confirmationWindow.UserChoice)
                        {
                            await CreateRestorePointAsync();
                        }

                        config.RememberRestorePointChoice = confirmationWindow.RememberChoice;
                        config.UserChoice = confirmationWindow.UserChoice; // Armazena a escolha do usuário
                        SaveConfig();
                    }
                }
                else
                {
                    if (config.UserChoice)
                    {
                        isOptimizing = true;
                        ButtonOtimizar.IsEnabled = false;
                        ButtonOtimizar.Background = new SolidColorBrush(Color.FromRgb(103, 118, 129));
                    }
                }

                IniciaOtimizacao();
            }
        }


        private DispatcherTimer dotsAnimationTimer = new DispatcherTimer();
        private int dotsCount = 0;


        private async Task CreateRestorePointAsync()
        {
            try
            {
                StatusText.Text = "Status: Criando ponto de restauração";
                StartDotsAnimation();

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Checkpoint-Computer -Description 'LSOptimizer Antes de Otimizar' -RestorePointType 'MODIFY_SETTINGS'\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                await Task.Run(() =>
                {
                    using (Process? process = Process.Start(processInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit();
                            if (process.ExitCode != 0)
                            {
                                string error = process.StandardError.ReadToEnd();
                                Dispatcher.Invoke(() => LogMessage($"Erro ao criar tarefa agendada: {error}"));
                            }
                        }
                        else
                        {
                            Dispatcher.Invoke(() => LogMessage("Erro: Não foi possível iniciar o processo."));
                        }
                    }
                });

                StatusText.Text = "Status: Ponto de restauração criado com sucesso!";
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao criar ponto de restauração: {ex.Message}");
            }
            finally
            {
                StopDotsAnimation();
            }
        }

        private void StartDotsAnimation()
        {
            dotsAnimationTimer.Interval = TimeSpan.FromMilliseconds(500);
            dotsAnimationTimer.Tick += DotsAnimationTimer_Tick;
            dotsAnimationTimer.Start();
        }

        private void StopDotsAnimation()
        {
            dotsAnimationTimer.Stop();
            dotsAnimationTimer.Tick -= DotsAnimationTimer_Tick;
            StatusText.Text = "Status: Ponto de restauração criado!";
            dotsCount = 0;
        }

        private void DotsAnimationTimer_Tick(object? sender, EventArgs e)
        {
            dotsCount = (dotsCount + 1) % 4;
            string dots = new string('.', dotsCount);
            StatusText.Text = $"Status: Criando ponto de restauração{dots}";
        }



        private void IniciaOtimizacao()
        {
            isOptimizing = true;
            UpdateStatus();

            // Cria um novo nome de arquivo de log para cada otimização
            currentLogFileName = Path.Combine(logFilePath, $"optimizer_log_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt");

            LogMessage("A otimização começou.");

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Task.Run(() =>
            {
                LimparArquivos();
                OtimizarDisco();
                LimparCachesAdicionais();
                OtimizarInternet();

                Task.Delay(5000).ContinueWith(t =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        isOptimizing = false;
                        StatusText.Text = "Status: Otimização concluída!";
                        LogMessage("A otimização terminou.");

                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                StatusText.Text = "Status: Pronto para otimizar!";
                                ButtonOtimizar.IsEnabled = true;
                                ButtonOtimizar.Background = new SolidColorBrush(Color.FromRgb(100, 149, 237));
                            });
                        });

                        _timer.Stop();
                    });
                });
            });
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _dotCount = (_dotCount + 1) % 4;
            string dots = new string('.', _dotCount);

            StatusText.Text = $"Status: Otimização em andamento{dots}";
        }

        private void UpdateStatus()
        {
            Dispatcher.Invoke(() =>
            {
                if (isOptimizing)
                {
                    StatusText.Text = "Status: Otimização em andamento.";
                }
                else
                {
                    StatusText.Text = "Status: Pronto para otimizar!";
                }
            });
        }

        private void LogMessage(string message)
        {
            try
            {
                if (currentLogFileName != null)
                {
                    File.AppendAllText(currentLogFileName, $"{DateTime.Now}: {message}{Environment.NewLine}");
                }
                else
                {
                    Console.WriteLine("Erro: currentLogFileName é nulo.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever no log: {ex.Message}");
            }
        }

        private void OtimizarInternet()
        {
            LogMessage("Iniciando otimização da internet.");

            try
            {
                // Limpar cache do navegador (exemplo para Google Chrome)
                string chromeCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\Cache");
                if (Directory.Exists(chromeCachePath))
                {
                    var files = Directory.GetFiles(chromeCachePath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            LogMessage($"Arquivo excluído do cache do Chrome: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Erro ao excluir arquivo {file} do cache do Chrome: {ex.Message}");
                        }
                    }
                }

                // Limpar cache do navegador (exemplo para Microsoft Edge)
                string edgeCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data\Default\Cache");
                if (Directory.Exists(edgeCachePath))
                {
                    var files = Directory.GetFiles(edgeCachePath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            LogMessage($"Arquivo excluído do cache do Edge: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Erro ao excluir arquivo {file} do cache do Edge: {ex.Message}");
                        }
                    }
                }

                LogMessage("Limpeza dos caches de internet concluída.");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao limpar caches de internet: {ex.Message}");
            }

            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                        {
                            LogMessage("Flush DNS executado com sucesso.");
                        }
                        else
                        {
                            string error = process.StandardError.ReadToEnd();
                            LogMessage($"Erro ao executar flush DNS: {error}");
                        }
                    }
                    else
                    {
                        LogMessage("Erro: Não foi possível iniciar o processo de flush DNS.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao executar flush DNS: {ex.Message}");
            }

            LogMessage("Otimização da internet concluída.");
        }


        private void LimparCachesAdicionais()
        {
            string iconCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
            var iconCacheFiles = Directory.GetFiles(iconCachePath, "iconcache*.db");
            foreach (var file in iconCacheFiles)
            {
                try
                {
                    File.Delete(file);
                    LogMessage($"Cache de ícones excluído: {file}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Erro ao excluir cache de ícones: {ex.Message}");
                }
            }

            string thumbCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Windows\Explorer");
            var thumbCacheFiles = Directory.GetFiles(thumbCachePath, "thumbcache*.db");
            foreach (var file in thumbCacheFiles)
            {
                try
                {
                    File.Delete(file);
                    LogMessage($"Cache de miniaturas excluído: {file}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Erro ao excluir cache de miniaturas: {ex.Message}");
                }
            }

            string fontCachePath = Path.Combine(@"C:\Windows\ServiceProfiles\LocalService\AppData\Local\Microsoft\Windows\FontCache");
            try
            {
                // Exclui todos os arquivos dentro da pasta FontCache
                var fontCacheFiles = Directory.GetFiles(fontCachePath);
                foreach (var file in fontCacheFiles)
                {
                    try
                    {
                        File.Delete(file);
                        LogMessage($"Arquivo excluído do cache de fontes: {file}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao excluir arquivo {file} do cache de fontes: {ex.Message}");
                    }
                }

                LogMessage("Limpeza do cache de fontes concluída.");
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao tentar limpar o cache de fontes: {ex.Message}");
            }
        }

        private void OtimizarDisco()
        {
            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);

            string? disco = Path.GetPathRoot(windowsDirectory);
            if (disco == null)
            {
                LogMessage("Erro: Não foi possível determinar a raiz do diretório do sistema.");
                return;
            }

            ProcessStartInfo processInfo = new ProcessStartInfo("defrag", $"/C {disco} /O");

            try
            {
                LogMessage("Iniciando Desfragmentação do disco...");
                Process? process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                    LogMessage("Desfragmentação concluída!");
                }
                else
                {
                    LogMessage("Erro: Não foi possível iniciar o processo de desfragmentação.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao iniciar a desfragmentação: {ex.Message}");
            }
        }

        // Limpa a pasta TEMP
        private void LimparTemp()
        {
            string tempPath = Path.GetTempPath();

            try
            {
                var files = Directory.GetFiles(tempPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        LogMessage($"Arquivo excluído da TEMP: {file}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao excluir arquivo {file} da TEMP: {ex.Message}");
                    }
                }

                var directories = Directory.GetDirectories(tempPath);
                foreach (var dir in directories)
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        LogMessage($"Pasta excluída da TEMP: {dir}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao excluir pasta {dir} da TEMP: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao limpar TEMP: {ex.Message}");
            }
        }

        // Limpa a pasta PreFetch
        private void LimparPreFetch()
        {
            string prefetchPath = @"C:\\Windows\\Prefetch";

            try
            {
                if (Directory.Exists(prefetchPath))
                {
                    var files = Directory.GetFiles(prefetchPath);
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            LogMessage($"Arquivo excluído da PreFetch: {file}");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Erro ao excluir arquivo {file} da PreFetch: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao limpar PreFetch: {ex.Message}");
            }
        }

        // Limpa a pasta Recent
        private void LimparRecent()
        {
            string recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);

            try
            {
                var files = Directory.GetFiles(recentPath);
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                        LogMessage($"Arquivo excluído da Recent: {file}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erro ao excluir arquivo {file} da Recent: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao limpar Recent: {ex.Message}");
            }
        }

        // Limpa o cache do sistema
        private void LimparCache()
        {
            string[] systemCacheDirs = new string[]
            {
                @"C:\\Windows\\SoftwareDistribution\\Download",
                @"C:\\Windows\\Temp",
                @"C:\\Users\\" + Environment.UserName + @"\\AppData\\Local\\Microsoft\\Windows\\Explorer"
            };

            try
            {
                foreach (var dir in systemCacheDirs)
                {
                    if (Directory.Exists(dir))
                    {
                        var files = Directory.GetFiles(dir);
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                                LogMessage($"Arquivo excluído da Cache: {file}");
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Erro ao excluir arquivo {file} da Cache: {ex.Message}");
                            }
                        }

                        var directories = Directory.GetDirectories(dir);
                        foreach (var directory in directories)
                        {
                            try
                            {
                                Directory.Delete(directory, true);
                                LogMessage($"Pasta excluída da Cache: {directory}");
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Erro ao excluir pasta {directory} da Cache: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Erro ao limpar Cache: {ex.Message}");
            }
        }

        private void LimparArquivos()
        {
            LimparTemp();
            LimparPreFetch();
            LimparRecent();
            LimparCache();
            LimparCachesAdicionais();
        }
    }

    public class Config
    {
        public bool StartWithWindows { get; set; } = false;
        public string AutoCleanInterval { get; set; } = "None";
        public bool RememberRestorePointChoice { get; set; } = false;
        public bool UserChoice { get; set; } = true;
    }
}