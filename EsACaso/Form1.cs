using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;
using System.Windows.Forms.DataVisualization.Charting;
using Newtonsoft.Json.Linq;
namespace EsACaso
{
    public partial class Form1 : Form
    {
        private string _serial = "";
        private string _model = "";
        private string _manufacturer = "";
        private PerformanceCounter _cpuCounter;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ProgressBar _progressBar;

        private PluginLoader _pluginLoader;
        private Button _btnThemeToggle;
        private Button _btnBattery;
        private Button _btnGpu;
        private ToolTip _btnThemeTooltip;
        private Button _btnGraphs;
        // GPU name cached dopo CaricaInfoHardware per usarla nel pulsante
        private string _gpuName = "";
        private PictureBox _pcImageBox;
        private static readonly string ImagesCacheDir = Path.Combine(Application.StartupPath, "images_cache");

        public Form1()
        {
            InitializeComponent();

            SettingsManager.Load();
            Logger.Info("EsACaso avviato.");

            ThemeManager.ApplyThemeToForm(this);

            _pluginLoader = new PluginLoader();
            _pluginLoader.LoadPlugins(this);

            // ── Pulsanti extra nel panelTop ──────────────────────────────────
            _btnThemeToggle = MakePanelTopBtn("🌓");
            _btnThemeTooltip = new ToolTip();
            _btnThemeTooltip.SetToolTip(_btnThemeToggle, "Toggle light/dark theme");
            _btnThemeToggle.Click += (s, e) =>
            {
                ThemeManager.ToggleTheme();
                Logger.Info("Tema cambiato.");
            };
            panelTop.Controls.Add(_btnThemeToggle);

            _btnBattery = MakePanelTopBtn("🔋");
            _btnBattery.Click += (s, e) => ShowInfoBattery();
            panelTop.Controls.Add(_btnBattery);

            _btnGpu = MakePanelTopBtn("💎");
            _btnGpu.Click += (s, e) => ShowGpuMenu();
            panelTop.Controls.Add(_btnGpu);

            _btnGraphs = MakePanelTopBtn("📊");
            _btnGraphs.Click += (s, e) => ShowLiveGraphs();
            panelTop.Controls.Add(_btnGraphs);
            this.Load += Form1_Load;
            this.Move += Form1_Move;
            this.Resize += Form1_Resize;
            this.FormClosed += Form1_FormClosed;

            (_serial, _model, _manufacturer) = GetComputerInfo();
            CaricaInfoHardware();
            InitPcImageBox();
            _ = LoadPcImageAsync();
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch { _cpuCounter = null; }

            CaricaInfoSistema();
            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) => CaricaInfoSistema();
            timer.Start();

            InitTrayIcon();
            InitProgressBar();
        }

        private Button MakePanelTopBtn(string text)
        {
            return new Button
            {
                Text = text,
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(42, 42, 42) },
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int startX = btnInfo != null ? btnInfo.Right + 10 : 10;
            _btnThemeToggle.Location = new Point(startX, (panelTop.Height - _btnThemeToggle.Height) / 2);
            _btnBattery.Location = new Point(_btnThemeToggle.Right + 6, _btnThemeToggle.Top);
            _btnGpu.Location = new Point(_btnBattery.Right + 6, _btnThemeToggle.Top);
            _btnGraphs.Location = new Point(_btnGpu.Right + 6, _btnThemeToggle.Top);

            var bounds = SettingsManager.WindowBounds;
            if (!bounds.IsEmpty)
                this.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                SettingsManager.WindowBounds = this.Bounds;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
                SettingsManager.WindowBounds = this.Bounds;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Logger.Info("EsACaso chiuso.");
            SettingsManager.Save();
            _pluginLoader.UnloadPlugins();
        }

        // ── BATTERY ──────────────────────────────────────────────────────────
        private void ShowInfoBattery()
        {
            Logger.Info("Apertura info batteria.");
            ShowInfoDialog("Battery Information", GetBatteryInfoString());
        }

        private string GetBatteryInfoString()
        {
            var ps = SystemInformation.PowerStatus;
            var sb = new StringBuilder();
            if (ps.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery)
            {
                sb.AppendLine("No battery detected (desktop system).");
                return sb.ToString();
            }
            sb.AppendLine($"  AC Line Status:        {ps.PowerLineStatus}");
            sb.AppendLine($"  Battery Charge Status: {ps.BatteryChargeStatus}");
            sb.AppendLine($"  Battery Life Remaining:{(ps.BatteryLifeRemaining >= 0 ? ps.BatteryLifeRemaining / 60 + " min" : "N/A")}");
            sb.AppendLine($"  Battery Life Percent:  {ps.BatteryLifePercent * 100:0}%");
            return sb.ToString();
        }

        // ── GPU ───────────────────────────────────────────────────────────────
        private void ShowInfoGpu()
        {
            Logger.Info("Apertura info GPU.");
            string info = GetGpuInfoString();
            ShowInfoDialogWithSupportBtn("GPU Information", info, _gpuName);
        }

        private string GetGpuInfoString()
        {
            var sb = new StringBuilder();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    bool any = false;
                    foreach (var obj in searcher.Get())
                    {
                        any = true;
                        ulong vramMB = 0;
                        try { vramMB = Convert.ToUInt64(obj["AdapterRAM"] ?? 0UL) / 1024 / 1024; } catch { }
                        sb.AppendLine($"  Name:                      {obj["Name"] ?? "N/A"}");
                        sb.AppendLine($"  Driver Version:            {obj["DriverVersion"] ?? "N/A"}");
                        sb.AppendLine($"  Video Processor:           {obj["VideoProcessor"] ?? "N/A"}");
                        sb.AppendLine($"  Adapter RAM:               {vramMB} MB");
                        sb.AppendLine($"  Current Resolution:        {obj["CurrentHorizontalResolution"]}x{obj["CurrentVerticalResolution"]}");
                        sb.AppendLine($"  Refresh Rate:              {obj["RefreshRate"] ?? "N/A"} Hz");
                        sb.AppendLine();
                    }
                    if (!any) sb.AppendLine("No GPU controllers found.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                sb.AppendLine($"Error querying GPU info: {ex.Message}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ritorna l'URL della pagina driver/supporto per la GPU rilevata.
        /// Se il nome contiene "nvidia" → NVIDIA, "amd"/"radeon" → AMD, altrimenti Intel.
        /// </summary>
        private string GetGpuSupportUrl(string gpuName)
        {
            string lower = gpuName.ToLower();

            if (lower.Contains("nvidia"))
            {
                // Cerca la serie (es. "RTX 4090", "GTX 1660") nell'URL di ricerca driver NVIDIA
                return $"https://www.nvidia.com/it-it/geforce/drivers/?search={Uri.EscapeDataString(gpuName)}";
            }
            if (lower.Contains("amd") || lower.Contains("radeon"))
            {
                return $"https://www.amd.com/it/support/search#{Uri.EscapeDataString(gpuName)}";
            }
            if (lower.Contains("intel"))
            {
                return "https://www.intel.com/content/www/us/en/support/detect.html";
            }
            // Fallback generico
            return $"https://www.bing.com/search?q={Uri.EscapeDataString(gpuName + " driver support")}";
        }

        // Dialog con testo + pulsante "Apri pagina driver GPU"
        private void ShowInfoDialogWithSupportBtn(string title, string info, string gpuName)
        {
            Form f = new Form
            {
                Text = title,
                Size = new Size(560, 430),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = info
            };

            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.RightToLeft
            };

            var btnClose = MakeButton("Chiudi");
            btnClose.Click += (s, e) => f.Close();

            // Pulsante driver solo se abbiamo un nome GPU
            if (!string.IsNullOrWhiteSpace(gpuName))
            {
                var btnDriver = MakeButton("🔗 Pagina driver GPU");
                btnDriver.Width = 160;
                btnDriver.Click += (s, e) =>
                {
                    string url = GetGpuSupportUrl(gpuName);
                    Logger.Info($"Apertura pagina driver GPU: {url}");
                    OpenWebPage(gpuName, url, f);
                };
                pnlBtn.Controls.Add(btnClose);
                pnlBtn.Controls.Add(btnDriver);
            }
            else
            {
                pnlBtn.Controls.Add(btnClose);
            }

            f.Controls.Add(rtb);
            f.Controls.Add(pnlBtn);
            f.ShowDialog();
        }

        // Dialog generica solo testo
        private void ShowInfoDialog(string title, string info)
        {
            Form f = new Form
            {
                Text = title,
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(15, 15, 15)
            };
            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = info
            };
            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnClose = MakeButton("Chiudi");
            btnClose.Click += (s, e) => f.Close();
            pnlBtn.Controls.Add(btnClose);
            f.Controls.Add(rtb);
            f.Controls.Add(pnlBtn);
            f.ShowDialog();
        }

        // Apre WebView2 con l'URL indicato (riusa la stessa logica di btnImmagine)
        private void OpenWebPage(string windowTitle, string url, Form parent = null)
        {
            Form fWeb = new Form
            {
                Text = windowTitle,
                Size = new Size(1100, 700),
                StartPosition = FormStartPosition.CenterScreen
            };
            var webView = new WebView2 { Dock = DockStyle.Fill };
            fWeb.Controls.Add(webView);
            webView.CoreWebView2InitializationCompleted += (s, e2) => webView.CoreWebView2.Navigate(url);
            fWeb.Load += async (s, e2) => await webView.EnsureCoreWebView2Async();
            fWeb.Show(parent);
        }

        // ── TRAY ICON ─────────────────────────────────────────────────────────
        private void InitTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Items.Add("Apri", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; BringToFront(); });
            _trayMenu.Items.Add("Esci", null, (s, e) => { _trayIcon.Visible = false; Application.Exit(); });

            _trayIcon = new NotifyIcon
            {
                Text = "EsACaso",
                Icon = SystemIcons.Application,
                ContextMenuStrip = _trayMenu
            };
            _trayIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; BringToFront(); };
            _trayIcon.Visible = false;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
                _trayIcon.Visible = true;
                _trayIcon.ShowBalloonTip(1500, "EsACaso", "Minimizzato in tray", ToolTipIcon.Info);
                Logger.Info("App minimizzata in tray.");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _trayIcon.Visible = false;
            base.OnFormClosing(e);
        }

        // ── PROGRESS BAR ──────────────────────────────────────────────────────
        private void InitProgressBar()
        {
            _progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Dock = DockStyle.Bottom,
                Height = 6,
                Visible = false
            };
            Controls.Add(_progressBar);
            _progressBar.BringToFront();
        }
        private void InitPcImageBox()
        {
            _pcImageBox = new PictureBox
            {
                Size = new Size(110, 110),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, 142)
            };
            Controls.Add(_pcImageBox);
            _pcImageBox.BringToFront();

            _pcImageBox.Image = SystemIcons.Application.ToBitmap();
        }
        private void ShowProgress(bool visible)
        {
            if (InvokeRequired) { Invoke(new Action(() => ShowProgress(visible))); return; }
            _progressBar.Visible = visible;
        }

        // ── DEBLOAT ───────────────────────────────────────────────────────────
        private async void btnDebloat_Click(object sender, EventArgs e)
        {
            btnDebloat.Enabled = false;
            ShowProgress(true);
            Logger.Info("Debloat avviato.");
            try
            {
                await RunScript("https://christitus.com/win", "christitus_win.ps1");
                MessageBox.Show(
                    "Tutto completato!\nRiavvia il sistema per fare in modo che i cambiamenti abbiano effetto.",
                    "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Logger.Info("Debloat completato.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                MessageBox.Show("Controlla la connessione a internet", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ShowProgress(false);
            btnDebloat.Enabled = true;
        }

        private async Task RunScript(string url, string fileName)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), fileName);
            using (var client = new HttpClient())
            {
                string scriptContent = await client.GetStringAsync(url);
                File.WriteAllText(tempFile, scriptContent, Encoding.UTF8);
            }
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                    UseShellExecute = true,
                    Verb = "runas"
                }
            };
            process.Start();
            await Task.Run(() => process.WaitForExit());
            File.Delete(tempFile);
        }

        // ── WINGET UPDATE ─────────────────────────────────────────────────────
        // FIX: BeginOutputReadLine ora ha il relativo handler OutputDataReceived
        private async void btnWinget_Click(object sender, EventArgs e)
        {
            btnWinget.Enabled = false;
            ShowProgress(true);
            Logger.Info("Winget update avviato.");

            Form fWinget = new Form
            {
                Text = "Winget — Aggiornamenti",
                Size = new Size(820, 560),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };

            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.RightToLeft
            };
            var btnChiudi = MakeButton("Chiudi");
            btnChiudi.Click += (s2, e2) => fWinget.Close();
            pnlBtn.Controls.Add(btnChiudi);

            fWinget.Controls.Add(rtbOutput);
            fWinget.Controls.Add(pnlBtn);
            fWinget.FormClosed += (s2, e2) => { btnWinget.Enabled = true; ShowProgress(false); };
            fWinget.Show();

            void Append(string text)
            {
                if (rtbOutput.IsDisposed) return;
                if (rtbOutput.InvokeRequired)
                    rtbOutput.Invoke(new Action(() => { rtbOutput.AppendText(text); rtbOutput.ScrollToCaret(); }));
                else { rtbOutput.AppendText(text); rtbOutput.ScrollToCaret(); }
            }

            Append("► Ricerca aggiornamenti disponibili...\n\n");

            try
            {
                await Task.Run(() =>
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "winget",
                            Arguments = "upgrade --all --include-unknown",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8
                        },
                        EnableRaisingEvents = true
                    };

                    // FIX: handler corretto — l'output ora appare davvero nel RTB
                    p.OutputDataReceived += (s2, e2) =>
                    {
                        if (e2.Data != null) Append(e2.Data + "\n");
                    };
                    p.ErrorDataReceived += (s2, e2) =>
                    {
                        if (e2.Data != null) Append("[ERR] " + e2.Data + "\n");
                    };

                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    Logger.Info($"winget uscito con codice {p.ExitCode}.");
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Append($"\n[ERRORE] {ex.Message}\n");
                Append("Assicurati che winget sia installato (App Installer da Microsoft Store).\n");
            }

            Append("\n══════════════════════════════════════════════════════════\n");
            Append("Aggiornamento completato.\n");
            ShowProgress(false);
            btnWinget.Enabled = true;
        }

        // ── INFO SISTEMA ──────────────────────────────────────────────────────
        private void CaricaInfoSistema()
        {
            string os = Environment.OSVersion.VersionString;
            MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(mem);
            DriveInfo drive = new DriveInfo("C");
            float cpu = _cpuCounter != null ? _cpuCounter.NextValue() : -1f;

            richTextBox1.Clear();
            richTextBox1.AppendText($"OS:       {os}\n");
            richTextBox1.AppendText($"CPU uso:  {(cpu >= 0 ? cpu.ToString("F1") + "%" : "N/A")}\n");
            richTextBox1.AppendText($"CPU core: {Environment.ProcessorCount}\n");
            richTextBox1.AppendText($"RAM:      {mem.ullAvailPhys / 1024 / 1024} MB liberi / {mem.ullTotalPhys / 1024 / 1024} MB totali\n");
            richTextBox1.AppendText($"Disco C:  {drive.AvailableFreeSpace / 1024 / 1024 / 1024} GB liberi / {drive.TotalSize / 1024 / 1024 / 1024} GB totali\n");
            richTextBox1.AppendText($"Dominio:  {Environment.UserDomainName}\n");
            richTextBox1.AppendText($"PC name:  {Environment.MachineName}\n");
            richTextBox1.AppendText($"Utente:   {Environment.UserName}\n");
            richTextBox1.ReadOnly = true;
        }

        private void CaricaInfoHardware()
        {
            rtbDispositivo.Clear();
            rtbDispositivo.AppendText($"Produttore: {_manufacturer}\n");
            rtbDispositivo.AppendText($"Modello:    {_model}\n");
            rtbDispositivo.AppendText($"Seriale:    {_serial}\n");

            // Leggi anche la GPU principale e salvala per il pulsante 💎
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (var obj in s.Get())
                    {
                        _gpuName = obj["Name"]?.ToString() ?? "";
                        rtbDispositivo.AppendText($"GPU:        {_gpuName}\n");
                        break; // prima GPU trovata
                    }
                }
            }
            catch (Exception ex) { Logger.Error(ex); }

            rtbDispositivo.ReadOnly = true;
        }

        private (string serial, string model, string manufacturer) GetComputerInfo()
        {
            string serial = "N/A", model = "N/A", manufacturer = "N/A";
            try
            {
                using (var s1 = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                    foreach (var obj in s1.Get())
                        serial = obj["SerialNumber"]?.ToString() ?? "N/A";
                using (var s2 = new ManagementObjectSearcher("SELECT Model, Manufacturer FROM Win32_ComputerSystem"))
                    foreach (var obj in s2.Get())
                    {
                        model = obj["Model"]?.ToString() ?? "N/A";
                        manufacturer = obj["Manufacturer"]?.ToString() ?? "N/A";
                    }
            }
            catch (Exception ex) { Logger.Error(ex); }
            return (serial, model, manufacturer);
        }

        // ── MANUTENZIONE ──────────────────────────────────────────────────────
        private void btnManutenzione_Click(object sender, EventArgs e)
        {
            Logger.Info("Apertura strumenti manutenzione.");

            Form fManu = new Form
            {
                Text = "Strumenti di manutenzione",
                Size = new Size(820, 600),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };

            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.LeftToRight
            };

            void Append(string text)
            {
                if (rtb.IsDisposed) return;
                if (rtb.InvokeRequired)
                    rtb.Invoke(new Action(() => { rtb.AppendText(text); rtb.ScrollToCaret(); }));
                else { rtb.AppendText(text); rtb.ScrollToCaret(); }
            }

            void AddBtn(string label, Func<Task> action)
            {
                var b = MakeButton(label);
                b.Width = 160;
                b.Dock = DockStyle.None;
                b.Click += async (s2, e2) =>
                {
                    b.Enabled = false;
                    ShowProgress(true);
                    Logger.Info($"Manutenzione: {label}");
                    await action();
                    ShowProgress(false);
                    b.Enabled = true;
                };
                pnlBtn.Controls.Add(b);
            }

            AddBtn("SFC /scannow", () => RunCmdAdmin("sfc", "/scannow", "Controllo integrità file di sistema (SFC)", rtb));
            AddBtn("DISM Restore", () => RunCmdAdmin("DISM", "/Online /Cleanup-Image /RestoreHealth", "DISM RestoreHealth", rtb));

            AddBtn("Svuota %TEMP%", async () =>
            {
                Append("\n► Pulizia cartella TEMP...\n");
                await Task.Run(() =>
                {
                    string temp = Path.GetTempPath();
                    int ok = 0, skip = 0;
                    foreach (string f2 in Directory.GetFiles(temp))
                    {
                        try { File.Delete(f2); ok++; }
                        catch { skip++; }
                    }
                    foreach (string d in Directory.GetDirectories(temp))
                    {
                        try { Directory.Delete(d, true); ok++; }
                        catch { skip++; }
                    }
                    Append($"  Eliminati: {ok}  |  In uso (saltati): {skip}\n");
                });
                Append("── Completato.\n");
            });

            AddBtn("Flush DNS", async () =>
            {
                Append("\n► Pulizia cache DNS...\n");
                await Task.Run(() =>
                {
                    var p = Process.Start(new ProcessStartInfo("ipconfig", "/flushdns")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });
                    Append(p.StandardOutput.ReadToEnd());
                    p.WaitForExit();
                });
                Append("── Completato.\n");
            });

            AddBtn("CHKDSK C:", () =>
            {
                var res = MessageBox.Show(
                    "CHKDSK richiede un riavvio per verificare il disco C:.\nContinuare?",
                    "CHKDSK", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("cmd", "/c echo Y | chkdsk C: /F /R /X")
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    });
                    Append("\nCHKDSK programmato al prossimo avvio.\n");
                }
                return Task.CompletedTask;
            });

            fManu.Controls.Add(rtb);
            fManu.Controls.Add(pnlBtn);
            fManu.Show();
        }

        private async Task RunCmdAdmin(string exe, string args, string titolo, RichTextBox rtb)
        {
            void Append(string text)
            {
                if (rtb.IsDisposed) return;
                if (rtb.InvokeRequired)
                    rtb.Invoke(new Action(() => { rtb.AppendText(text); rtb.ScrollToCaret(); }));
                else { rtb.AppendText(text); rtb.ScrollToCaret(); }
            }

            Append($"\n► {titolo}...\n");
            await Task.Run(() =>
            {
                string tempOut = Path.Combine(Path.GetTempPath(), "esacaso_cmd_out.txt");
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Process cmd -ArgumentList '/c {exe} {args} > \\\"{tempOut}\\\" 2>&1' -Verb RunAs -Wait\"",
                    UseShellExecute = true,
                    Verb = "runas"
                });
                p?.WaitForExit();
                if (File.Exists(tempOut))
                {
                    Append(File.ReadAllText(tempOut, Encoding.Default));
                    File.Delete(tempOut);
                }
            });
            Append("── Completato.\n");
            Logger.Info($"{titolo} completato.");
        }
        // ── IMMAGINE PC (ricerca online + cache + fallback) ────────────────────
        private async Task LoadPcImageAsync()
        {
            try
            {
                Directory.CreateDirectory(ImagesCacheDir);
                string cacheKey = SanitizeFileName($"{_manufacturer}_{_model}");
                string cachePath = Path.Combine(ImagesCacheDir, cacheKey + ".png");

                // 1) Cache locale
                if (File.Exists(cachePath))
                {
                    SetPcImage(Image.FromFile(cachePath));
                    Logger.Info("Immagine PC caricata da cache.");
                    return;
                }

                // 2) Ricerca online (solo se abbiamo dati utili)
                if (!string.IsNullOrWhiteSpace(_manufacturer) && _manufacturer != "N/A"
                    && !string.IsNullOrWhiteSpace(_model) && _model != "N/A")
                {
                    string query = $"{_manufacturer} {_model}";
                    string imageUrl = await SearchFirstImageUrlAsync(query);

                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        bool downloaded = await DownloadImageAsync(imageUrl, cachePath);
                        if (downloaded && File.Exists(cachePath))
                        {
                            SetPcImage(Image.FromFile(cachePath));
                            Logger.Info($"Immagine PC scaricata e messa in cache: {imageUrl}");
                            return;
                        }
                    }
                }

                // 3) Fallback: icona generica per tipo di chassis
                Logger.Info("Ricerca immagine fallita, uso fallback generico.");
                SetPcImage(GetFallbackImageForChassis());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SetPcImage(GetFallbackImageForChassis());
            }
        }

        private void SetPcImage(Image img)
        {
            if (_pcImageBox.InvokeRequired)
            {
                _pcImageBox.Invoke(new Action(() => { _pcImageBox.Image = img; }));
            }
            else
            {
                _pcImageBox.Image = img;
            }
        }

        private string SanitizeFileName(string input)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                input = input.Replace(c, '_');
            return input.Replace(' ', '_');
        }

        // Apre un WebView2 invisibile, naviga su DuckDuckGo Images, estrae il primo URL immagine
        private async Task<string> SearchFirstImageUrlAsync(string query)
        {
            var tcs = new TaskCompletionSource<string>();

            Form hiddenForm = new Form
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized,
                Size = new Size(1, 1),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-2000, -2000) // fuori schermo
            };
            var webView = new WebView2 { Dock = DockStyle.Fill };
            hiddenForm.Controls.Add(webView);

            // Timeout di sicurezza: se dopo 12s non abbiamo risultato, abortiamo
            var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(12));
            cts.Token.Register(() =>
            {
                if (!tcs.Task.IsCompleted) tcs.TrySetResult(null);
            });

            webView.CoreWebView2InitializationCompleted += async (s, e) =>
            {
                try
                {
                    string url = $"https://duckduckgo.com/?q={Uri.EscapeDataString(query)}&iax=images&ia=images";
                    webView.CoreWebView2.NavigationCompleted += async (s2, e2) =>
                    {
                        try
                        {
                            // Diamo tempo al JS della pagina di popolare i risultati
                            await Task.Delay(2500);

                            // Estrae il primo URL immagine valido trovato nel DOM dei risultati
                            string script = @"
                        (function() {
                            var imgs = document.querySelectorAll('img.tile--img__img, img[data-src]');
                            for (var i = 0; i < imgs.length; i++) {
                                var src = imgs[i].src || imgs[i].getAttribute('data-src');
                                if (src && src.startsWith('http')) return src;
                            }
                            return '';
                        })();
                    ";
                            string result = await webView.CoreWebView2.ExecuteScriptAsync(script);
                            string cleaned = result?.Trim('"');
                            if (!tcs.Task.IsCompleted)
                                tcs.TrySetResult(string.IsNullOrWhiteSpace(cleaned) ? null : cleaned);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                            if (!tcs.Task.IsCompleted) tcs.TrySetResult(null);
                        }
                    };
                    webView.CoreWebView2.Navigate(url);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    if (!tcs.Task.IsCompleted) tcs.TrySetResult(null);
                }
            };

            try
            {
                await webView.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                if (!tcs.Task.IsCompleted) tcs.TrySetResult(null);
            }

            string finalUrl = await tcs.Task;

            // Pulizia: chiudiamo la finestra nascosta
            if (this.InvokeRequired)
                this.Invoke(new Action(() => hiddenForm.Close()));
            else
                hiddenForm.Close();

            return finalUrl;
        }

        private async Task<bool> DownloadImageAsync(string url, string destPath)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    byte[] data = await client.GetByteArrayAsync(url);

                    using (var ms = new MemoryStream(data))
                    using (var img = Image.FromStream(ms))
                    {
                        img.Save(destPath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        // Fallback: icona generica in base al tipo di chassis rilevato via WMI
        private Image GetFallbackImageForChassis()
        {
            string tipo = GetChassisType();
            Logger.Info($"Fallback immagine per tipo chassis: {tipo}");

            switch (tipo)
            {
                case "Laptop":
                    return DrawFallbackIcon("💻");
                case "Desktop":
                    return DrawFallbackIcon("🖥️");
                case "AllInOne":
                    return DrawFallbackIcon("🖥️");
                default:
                    return DrawFallbackIcon("🖳");
            }
        }

        // Disegna un'icona testuale (emoji) su sfondo dark come placeholder, zero dipendenze esterne
        private Image DrawFallbackIcon(string emoji)
        {
            var bmp = new Bitmap(160, 160);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(20, 20, 20));
                using (var font = new Font("Segoe UI Emoji", 48f))
                using (var brush = new SolidBrush(Color.FromArgb(204, 204, 204)))
                {
                    var size = g.MeasureString(emoji, font);
                    g.DrawString(emoji, font, brush, (160 - size.Width) / 2, (160 - size.Height) / 2);
                }
            }
            return bmp;
        }

        private string GetChassisType()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        var types = obj["ChassisTypes"] as ushort[];
                        if (types == null || types.Length == 0) continue;
                        ushort t = types[0];

                        // Codici SMBIOS più comuni
                        if (t == 9 || t == 10 || t == 14) return "Laptop";       // Laptop, Notebook, Sub Notebook
                        if (t == 13) return "AllInOne";                          // All in One
                        if (t == 3 || t == 4 || t == 6 || t == 7) return "Desktop"; // Desktop, Low Profile, Mini Tower, Tower
                    }
                }
            }
            catch (Exception ex) { Logger.Error(ex); }
            return "Unknown";
        }
        // ── RETE ──────────────────────────────────────────────────────────────
        private async void btnRete_Click(object sender, EventArgs e)
        {
            Logger.Info("Apertura strumenti rete.");

            Form fRete = new Form
            {
                Text = "Strumenti di rete",
                Size = new Size(820, 620),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true
            };

            var pnlTop = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 96,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };

            void Append(string text)
            {
                if (rtb.IsDisposed) return;
                if (rtb.InvokeRequired)
                    rtb.Invoke(new Action(() => { rtb.AppendText(text); rtb.ScrollToCaret(); }));
                else { rtb.AppendText(text); rtb.ScrollToCaret(); }
            }

            var txtHost = new TextBox
            {
                Width = 200,
                Text = "8.8.8.8",
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var btnPing = MakeButton("Ping");
            btnPing.Width = 80;
            btnPing.Dock = DockStyle.None;
            btnPing.Click += async (s2, e2) =>
            {
                btnPing.Enabled = false;
                ShowProgress(true);
                string host = txtHost.Text.Trim();
                Append($"\n► Ping a {host}...\n");
                await Task.Run(() =>
                {
                    try
                    {
                        var ping = new System.Net.NetworkInformation.Ping();
                        for (int i = 0; i < 4; i++)
                        {
                            var reply = ping.Send(host, 2000);
                            if (reply.Status == IPStatus.Success)
                                Append($"  Risposta da {reply.Address}: {reply.RoundtripTime} ms  TTL={reply.Options?.Ttl}\n");
                            else
                                Append($"  Timeout — {reply.Status}\n");
                        }
                    }
                    catch (Exception ex) { Logger.Error(ex); Append($"  Errore: {ex.Message}\n"); }
                });
                ShowProgress(false);
                btnPing.Enabled = true;
            };

            var btnTrace = MakeButton("Traceroute");
            btnTrace.Width = 110;
            btnTrace.Dock = DockStyle.None;
            btnTrace.Click += async (s2, e2) =>
            {
                btnTrace.Enabled = false;
                ShowProgress(true);
                string host = txtHost.Text.Trim();
                Append($"\n► Traceroute a {host}...\n");
                await Task.Run(() =>
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "tracert",
                            Arguments = $"-d {host}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.GetEncoding(850)
                        }
                    };
                    p.OutputDataReceived += (s3, e3) => { if (e3.Data != null) Append(e3.Data + "\n"); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                });
                ShowProgress(false);
                btnTrace.Enabled = true;
            };

            var btnIPLocale = MakeButton("IP Locale");
            btnIPLocale.Width = 100;
            btnIPLocale.Dock = DockStyle.None;
            btnIPLocale.Click += async (s2, e2) =>
            {
                btnIPLocale.Enabled = false;
                ShowProgress(true);
                Append("\n► Indirizzi IP locali...\n");
                await Task.Run(() =>
                {
                    var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in hostEntry.AddressList)
                        Append($"  {ip.AddressFamily,-12} {ip}\n");
                });
                ShowProgress(false);
                btnIPLocale.Enabled = true;
            };

            var btnIPPubblico = MakeButton("IP Pubblico");
            btnIPPubblico.Width = 110;
            btnIPPubblico.Dock = DockStyle.None;
            btnIPPubblico.Click += async (s2, e2) =>
            {
                btnIPPubblico.Enabled = false;
                ShowProgress(true);
                Append("\n► Recupero IP pubblico...\n");
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        string ip = await client.GetStringAsync("https://api.ipify.org");
                        Append($"  IP pubblico: {ip.Trim()}\n");
                    }
                }
                catch (Exception ex) { Logger.Error(ex); Append($"  Errore: {ex.Message}\n"); }
                ShowProgress(false);
                btnIPPubblico.Enabled = true;
            };

            var btnIpconfig = MakeButton("ipconfig /all");
            btnIpconfig.Width = 120;
            btnIpconfig.Dock = DockStyle.None;
            btnIpconfig.Click += async (s2, e2) =>
            {
                btnIpconfig.Enabled = false;
                ShowProgress(true);
                Append("\n► ipconfig /all...\n");
                await Task.Run(() =>
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ipconfig",
                            Arguments = "/all",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.GetEncoding(850)
                        }
                    };
                    p.OutputDataReceived += (s3, e3) => { if (e3.Data != null) Append(e3.Data + "\n"); };
                    p.Start();
                    p.BeginOutputReadLine();
                    p.WaitForExit();
                });
                ShowProgress(false);
                btnIpconfig.Enabled = true;
            };

            var pnlHost = new Panel { Height = 40, Dock = DockStyle.None, Width = 300 };
            var lblHost = new Label
            {
                Text = "Host:",
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(0, 10),
                AutoSize = true
            };
            pnlHost.Controls.Add(lblHost);
            pnlHost.Controls.Add(txtHost);
            txtHost.Location = new Point(40, 8);

            pnlTop.Controls.Add(pnlHost);
            pnlTop.Controls.Add(btnPing);
            pnlTop.Controls.Add(btnTrace);
            pnlTop.Controls.Add(btnIPLocale);
            pnlTop.Controls.Add(btnIPPubblico);
            pnlTop.Controls.Add(btnIpconfig);

            fRete.Controls.Add(rtb);
            fRete.Controls.Add(pnlTop);
            fRete.Show();
        }

        // ── STARTUP MANAGER ───────────────────────────────────────────────────
        private void btnStartup_Click(object sender, EventArgs e)
        {
            Logger.Info("Apertura Startup Manager.");

            Form fStartup = new Form
            {
                Text = "Startup Manager",
                Size = new Size(720, 500),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var listView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.FromArgb(204, 204, 204),
                Font = new Font("Consolas", 9f)
            };
            listView.Columns.Add("Nome", 200);
            listView.Columns.Add("Comando", 420);
            listView.Columns.Add("Scope", 70);

            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6)
            };

            var keys = new (string path, string scope)[]
            {
                (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKCU"),
                (@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "HKLM"),
                (@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU")
            };

            void Ricarica()
            {
                listView.Items.Clear();
                foreach (var (path, scope) in keys)
                {
                    var root = scope == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                    using (var key = root.OpenSubKey(path, false))
                    {
                        if (key == null) continue;
                        foreach (string name in key.GetValueNames())
                        {
                            string val = key.GetValue(name)?.ToString() ?? "";
                            var item = new ListViewItem(name);
                            item.SubItems.Add(val);
                            item.SubItems.Add(scope);
                            item.Tag = (path, scope, name);
                            listView.Items.Add(item);
                        }
                    }
                }
            }

            Ricarica();

            var btnDisabilita = MakeButton("Disabilita selezionato");
            btnDisabilita.Width = 180;
            btnDisabilita.Dock = DockStyle.None;
            btnDisabilita.Click += (s2, e2) =>
            {
                if (listView.SelectedItems.Count == 0) return;
                var item = listView.SelectedItems[0];
                var (path, scope, name) = ((string, string, string))item.Tag;
                var root = scope == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                try
                {
                    using (var key = root.OpenSubKey(path, true))
                    {
                        string val = key?.GetValue(name)?.ToString() ?? "";
                        key?.DeleteValue(name, false);
                        using (var dis = root.CreateSubKey(path.Replace("\\Run", "\\DisabledStartup")))
                            dis?.SetValue(name, val);
                    }
                    Logger.Info($"Startup disabilitato: {name}");
                    Ricarica();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show($"Errore: {ex.Message}\nProva ad eseguire come amministratore.", "Errore",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnAbilita = MakeButton("Riabilita selezionato");
            btnAbilita.Width = 180;
            btnAbilita.Dock = DockStyle.None;
            btnAbilita.Click += (s2, e2) =>
            {
                if (listView.SelectedItems.Count == 0) return;
                var item = listView.SelectedItems[0];
                var (path, scope, name) = ((string, string, string))item.Tag;
                var root = scope == "HKCU" ? Registry.CurrentUser : Registry.LocalMachine;
                try
                {
                    string disPath = path.Replace("\\Run", "\\DisabledStartup");
                    using (var dis = root.OpenSubKey(disPath, true))
                    {
                        if (dis == null) return;
                        string val = dis.GetValue(name)?.ToString() ?? "";
                        using (var run = root.OpenSubKey(path, true))
                            run?.SetValue(name, val);
                        dis.DeleteValue(name, false);
                    }
                    Logger.Info($"Startup riabilitato: {name}");
                    Ricarica();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnRefresh = MakeButton("Aggiorna");
            btnRefresh.Width = 100;
            btnRefresh.Dock = DockStyle.None;
            btnRefresh.Click += (s2, e2) => Ricarica();

            pnlBtn.Controls.Add(btnDisabilita);
            pnlBtn.Controls.Add(btnAbilita);
            pnlBtn.Controls.Add(btnRefresh);

            fStartup.Controls.Add(listView);
            fStartup.Controls.Add(pnlBtn);
            fStartup.Show();
        }

        // ── REPORT HARDWARE ───────────────────────────────────────────────────
        private void btnReport_Click(object sender, EventArgs e)
        {
            Logger.Info("Apertura Report Hardware.");
            string reportText = BuildReport();

            Form fReport = new Form
            {
                Text = "Report Hardware",
                Size = new Size(680, 700),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var pnlBtn = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(20, 20, 20),
                Padding = new Padding(6)
            };

            var btnSalva = MakeButton("Salva TXT");
            btnSalva.Width = 120;
            btnSalva.Dock = DockStyle.None;
            btnSalva.Click += (s2, e2) =>
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "File di testo|*.txt",
                    FileName = $"report_{Environment.MachineName}.txt"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(dlg.FileName, reportText, Encoding.UTF8);
                    Logger.Info($"Report salvato: {dlg.FileName}");
                }
            };

            var btnCopia = MakeButton("Copia");
            btnCopia.Width = 90;
            btnCopia.Dock = DockStyle.None;
            btnCopia.Click += (s2, e2) =>
            {
                Clipboard.SetText(reportText);
                MessageBox.Show("Report copiato negli appunti!", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            var btnStampa = MakeButton("Stampa / PDF");
            btnStampa.Width = 120;
            btnStampa.Dock = DockStyle.None;
            btnStampa.Click += (s2, e2) => StampaReport(reportText);

            pnlBtn.Controls.Add(btnSalva);
            pnlBtn.Controls.Add(btnCopia);
            pnlBtn.Controls.Add(btnStampa);

            var rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(15, 15, 15),
                ForeColor = Color.FromArgb(204, 204, 204),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = reportText
            };

            fReport.Controls.Add(rtb);
            fReport.Controls.Add(pnlBtn);
            fReport.Show();
        }

        private string BuildReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════════════════");
            sb.AppendLine("         REPORT COMPONENTI HARDWARE");
            sb.AppendLine("══════════════════════════════════════════\n");

            sb.AppendLine("── CPU ──────────────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
                foreach (var obj in s.Get())
                {
                    sb.AppendLine($"  Nome:       {obj["Name"]}");
                    sb.AppendLine($"  Socket:     {obj["SocketDesignation"]}");
                    sb.AppendLine($"  Core:       {obj["NumberOfCores"]}");
                    sb.AppendLine($"  Thread:     {obj["NumberOfLogicalProcessors"]}");
                    sb.AppendLine($"  Clock base: {obj["MaxClockSpeed"]} MHz");
                    sb.AppendLine($"  Cache L2:   {obj["L2CacheSize"]} KB");
                    sb.AppendLine($"  Cache L3:   {obj["L3CacheSize"]} KB");
                }

            sb.AppendLine("\n── GPU ──────────────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                foreach (var obj in s.Get())
                {
                    ulong vramMB = 0;
                    try { vramMB = Convert.ToUInt64(obj["AdapterRAM"] ?? 0UL) / 1024 / 1024; } catch { }
                    sb.AppendLine($"  Nome:        {obj["Name"]}");
                    sb.AppendLine($"  VRAM:        {vramMB} MB");
                    sb.AppendLine($"  Risoluzione: {obj["CurrentHorizontalResolution"]}x{obj["CurrentVerticalResolution"]}");
                    sb.AppendLine($"  Driver:      {obj["DriverVersion"]}");
                    sb.AppendLine();
                }

            sb.AppendLine("── RAM (slot fisici) ────────────────────");
            int slotRam = 0;
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                foreach (var obj in s.Get())
                {
                    slotRam++;
                    ulong sizeMB = Convert.ToUInt64(obj["Capacity"] ?? 0UL) / 1024 / 1024;
                    sb.AppendLine($"  Slot {slotRam}:");
                    sb.AppendLine($"    Capacità:   {sizeMB} MB");
                    sb.AppendLine($"    Velocità:   {obj["Speed"]} MHz");
                    sb.AppendLine($"    Tipo:       {RamType(Convert.ToUInt16(obj["MemoryType"] ?? (ushort)0))}");
                    sb.AppendLine($"    Produttore: {obj["Manufacturer"]}");
                    sb.AppendLine($"    Part Num:   {obj["PartNumber"]}");
                }
            using (var s = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray"))
                foreach (var obj in s.Get())
                    sb.AppendLine($"  Slot totali sulla mobo: {obj["MemoryDevices"]}");

            sb.AppendLine("\n── Dischi ───────────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive"))
                foreach (var obj in s.Get())
                {
                    ulong sizeGB = Convert.ToUInt64(obj["Size"] ?? 0UL) / 1024 / 1024 / 1024;
                    sb.AppendLine($"  {obj["Caption"]}");
                    sb.AppendLine($"    Interfaccia: {obj["InterfaceType"]}");
                    sb.AppendLine($"    Dimensione:  {sizeGB} GB");
                    sb.AppendLine($"    Seriale:     {obj["SerialNumber"]}");
                    sb.AppendLine($"    Firmware:    {obj["FirmwareRevision"]}");
                    sb.AppendLine();
                }

            sb.AppendLine("── Scheda madre ─────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard"))
                foreach (var obj in s.Get())
                {
                    sb.AppendLine($"  Produttore: {obj["Manufacturer"]}");
                    sb.AppendLine($"  Modello:    {obj["Product"]}");
                    sb.AppendLine($"  Seriale:    {obj["SerialNumber"]}");
                    sb.AppendLine($"  Versione:   {obj["Version"]}");
                }

            sb.AppendLine("\n── BIOS ─────────────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS"))
                foreach (var obj in s.Get())
                {
                    sb.AppendLine($"  Produttore: {obj["Manufacturer"]}");
                    sb.AppendLine($"  Versione:   {obj["SMBIOSBIOSVersion"]}");
                    sb.AppendLine($"  Data:       {obj["ReleaseDate"]}");
                }

            sb.AppendLine("\n── Rete ─────────────────────────────────");
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter=True"))
                foreach (var obj in s.Get())
                {
                    sb.AppendLine($"  {obj["Name"]}");
                    sb.AppendLine($"    MAC:  {obj["MACAddress"]}");
                    sb.AppendLine($"    Tipo: {obj["AdapterType"]}");
                    sb.AppendLine();
                }

            sb.AppendLine("══════════════════════════════════════════");
            return sb.ToString();
        }

        private void StampaReport(string testo)
        {
            var pd = new PrintDocument { DocumentName = $"Report_{Environment.MachineName}" };
            string[] righe = testo.Split('\n');
            int rigaCorrente = 0;

            pd.PrintPage += (s, e) =>
            {
                float y = e.MarginBounds.Top;
                var font = new Font("Consolas", 8f);
                float lineHeight = font.GetHeight(e.Graphics);
                while (rigaCorrente < righe.Length)
                {
                    if (y + lineHeight > e.MarginBounds.Bottom) { e.HasMorePages = true; font.Dispose(); return; }
                    e.Graphics.DrawString(righe[rigaCorrente], font, Brushes.Black, e.MarginBounds.Left, y);
                    y += lineHeight;
                    rigaCorrente++;
                }
                e.HasMorePages = false;
                font.Dispose();
            };

            var dlg = new PrintDialog { Document = pd, UseEXDialog = true };
            if (dlg.ShowDialog() == DialogResult.OK)
                pd.Print();
        }

        private string RamType(ushort code)
        {
            switch (code)
            {
                case 20: return "DDR";
                case 21: return "DDR2";
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 34: return "DDR5";
                default: return $"Unknown ({code})";
            }
        }

        // ── SUPPORTO WEB ──────────────────────────────────────────────────────
        private string GetSupportUrl(string model, string serial, string manufacturer)
        {
            string m = manufacturer.ToLower();
            if (m.Contains("dell")) return $"https://www.dell.com/support/home/it-it/product-support/servicetag/{Uri.EscapeDataString(serial)}";
            if (m.Contains("lenovo")) return $"https://pcsupport.lenovo.com/it/it/products/{Uri.EscapeDataString(model)}";
            if (m.Contains("hp") || m.Contains("hewlett")) return $"https://support.hp.com/it-it/product/details/{Uri.EscapeDataString(serial)}";
            if (m.Contains("asus")) return $"https://www.asus.com/it/support/?ModelName={Uri.EscapeDataString(model)}";
            if (m.Contains("acer")) return $"https://www.acer.com/it/support/?sn={Uri.EscapeDataString(serial)}";
            if (m.Contains("microsoft")) return "https://support.microsoft.com/it-it/surface";
            return $"https://www.bing.com/search?q={Uri.EscapeDataString(manufacturer + " " + model + " support")}";
        }

        private void btnImmagine_Click(object sender, EventArgs e)
        {
            string url = GetSupportUrl(_model, _serial, _manufacturer);
            Logger.Info($"Apertura supporto web: {url}");
            OpenWebPage($"Supporto PC — {_manufacturer} {_model}", url);
        }

        // ── RIAVVIA ───────────────────────────────────────────────────────────
        private void btnRiavvia_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Riavviare il PC adesso?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                Logger.Info("Riavvio richiesto dall'utente.");
                Process.Start("shutdown.exe", "/r /t 0");
            }
        }

        // ── INFO APP ──────────────────────────────────────────────────────────
        private void btnInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "EsACaso v1.0\n\nQuesto programma raccoglie informazioni sul sistema e sull'hardware, " +
                "e può eseguire uno script PowerShell remoto per configurare il sistema.\n\nSviluppato da Miner.",
                "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ── HELPER UI ─────────────────────────────────────────────────────────
        private Button MakeButton(string text)
        {
            return new Button
            {
                Text = text,
                Dock = DockStyle.None,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderColor = Color.FromArgb(42, 42, 42) },
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
        }
        // ── GRAFICI LIVE ──────────────────────────────────────────────────────
        private void ShowLiveGraphs()
        {
            Logger.Info("Apertura grafici live.");

            Form fGraph = new Form
            {
                Text = "Monitoraggio in tempo reale",
                Size = new Size(820, 600),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.FromArgb(15, 15, 15)
            };
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            Chart chartCpu = CreateChart("CPU %", 100);
            Chart chartRam = CreateChart("RAM usata (MB)", 0); // max dinamico, vedi sotto

            tableLayout.Controls.Add(chartCpu, 0, 0);
            tableLayout.Controls.Add(chartRam, 0, 1);

            fGraph.Controls.Add(tableLayout);

            // Imposta il massimo asse Y della RAM in base alla RAM totale del PC
            MEMORYSTATUSEX memInit = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(memInit);
            long totalRamMB = (long)(memInit.ullTotalPhys / 1024 / 1024);
            chartRam.ChartAreas[0].AxisY.Maximum = totalRamMB;

            var timer = new Timer { Interval = 1000 };
            timer.Tick += (s, e) =>
            {
                float cpu = _cpuCounter != null ? _cpuCounter.NextValue() : 0f;

                MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(mem);
                long ramUsataMB = (long)((mem.ullTotalPhys - mem.ullAvailPhys) / 1024 / 1024);

                AddPoint(chartCpu, cpu);
                AddPoint(chartRam, ramUsataMB);
            };
            timer.Start();

            fGraph.FormClosed += (s, e) => timer.Stop();
            fGraph.Show();
        }

        private Chart CreateChart(string titolo, double maxY)
        {
            var chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            var area = new ChartArea("area");
            area.BackColor = Color.FromArgb(20, 20, 20);
            area.AxisX.LabelStyle.Enabled = false;
            area.AxisX.MajorGrid.LineColor = Color.FromArgb(42, 42, 42);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(42, 42, 42);
            area.AxisX.LineColor = Color.FromArgb(80, 80, 80);
            area.AxisY.LineColor = Color.FromArgb(80, 80, 80);
            area.AxisY.LabelStyle.ForeColor = Color.FromArgb(204, 204, 204);
            if (maxY > 0) area.AxisY.Maximum = maxY;
            area.AxisY.Minimum = 0;
            chart.ChartAreas.Add(area);

            var series = new Series("dati")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(0, 153, 255),
                BorderWidth = 2,
                IsVisibleInLegend = false
            };
            chart.Series.Add(series);

            var titleObj = new Title(titolo)
            {
                ForeColor = Color.FromArgb(204, 204, 204),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            chart.Titles.Add(titleObj);

            return chart;
        }

        private void AddPoint(Chart chart, double value)
        {
            var points = chart.Series["dati"].Points;
            points.AddY(value);
            if (points.Count > 60) // tieni solo ultimi 60 secondi
                points.RemoveAt(0);
        }
        // ── P/INVOKE ─────────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailVirtualExtended;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private void ShowGpuMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("📋 Info GPU", null, (s, e) => ShowInfoDialog("GPU Information", GetGpuInfoString()));
            menu.Items.Add("🌐 Pagina prodotto (Nvidia/AMD/Intel)", null, (s, e) => ApriPaginaGpu());
            menu.Show(_btnGpu, new Point(0, _btnGpu.Height));
        }

        private void ApriPaginaGpu()
        {
            string gpuName = GetPrimaryGpuName();
            if (string.IsNullOrWhiteSpace(gpuName))
            {
                MessageBox.Show("Impossibile rilevare il modello della GPU.", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string url = GetGpuVendorUrl(gpuName);
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        // Prende il nome della prima GPU "vera" (esclude adapter virtuali tipo Remote Desktop)
        private string GetPrimaryGpuName()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_VideoController WHERE AdapterRAM > 0"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(name))
                            return name;
                    }
                }
                // fallback: prendi la prima qualsiasi se il filtro sopra non trova nulla
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                        return obj["Name"]?.ToString();
                }
            }
            catch { }
            return null;
        }

        // Costruisce l'URL di ricerca sul sito del produttore in base al nome scheda
        private string GetGpuVendorUrl(string gpuName)
        {
            string n = gpuName.ToLower();

            if (n.Contains("nvidia") || n.Contains("geforce") || n.Contains("rtx") || n.Contains("gtx"))
            {
                string model = ExtractNvidiaModel(gpuName);
                return string.IsNullOrEmpty(model)
                    ? "https://www.nvidia.com/it-it/geforce/graphics-cards/"
                    : $"https://www.nvidia.com/it-it/search/?q={Uri.EscapeDataString(model)}";
            }

            if (n.Contains("amd") || n.Contains("radeon") || n.Contains("rx "))
            {
                string model = ExtractAmdModel(gpuName);
                return string.IsNullOrEmpty(model)
                    ? "https://www.amd.com/it/products/graphics.html"
                    : $"https://www.amd.com/it/search.html?q={Uri.EscapeDataString(model)}";
            }

            if (n.Contains("intel"))
            {
                return $"https://www.intel.it/content/www/it/it/search.html?ws=text#q={Uri.EscapeDataString(gpuName)}";
            }

            // Produttore sconosciuto: ricerca generica
            return $"https://www.bing.com/search?q={Uri.EscapeDataString(gpuName + " scheda video specifiche")}";
        }

        // Estrae es. "RTX 4070" da "NVIDIA GeForce RTX 4070 Laptop GPU"
        private string ExtractNvidiaModel(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                name, @"(RTX|GTX)\s?(\d{3,4})\s?(Ti|SUPER|Super)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Trim() : null;
        }

        // Estrae es. "RX 7800 XT" da "AMD Radeon RX 7800 XT"
        private string ExtractAmdModel(string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                name, @"RX\s?\d{3,4}\s?(XT|XTX|GRE)?",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Trim() : null;
        }

    }

}