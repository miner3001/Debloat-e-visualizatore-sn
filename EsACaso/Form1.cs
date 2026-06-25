using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using Microsoft.Web.WebView2.WinForms;

namespace EsACaso
{
    public partial class Form1 : Form
    {
        private string _serial = "";
        private string _model = "";
        private string _manufacturer = "";

        public Form1()
        {
            InitializeComponent();
            (_serial, _model, _manufacturer) = GetComputerInfo();
            CaricaInfoSistema();
            CaricaInfoHardware();
            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) => CaricaInfoSistema();
            timer.Start();
        }

        private async void btnDebloat_Click(object sender, EventArgs e)
        {
            btnDebloat.Enabled = false;
            try
            {
                await RunScript("https://christitus.com/win", "christitus_win.ps1");
                MessageBox.Show("Tutto completato!\nRiavvia il sistema per fare in modo che i cambiamenti abbiano effetto.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Controlla la connessione a internet", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            btnDebloat.Enabled = true;
        }

        private async Task RunScript(string url, string fileName)
        {
            string tempFile = Path.Combine(Path.GetTempPath(), fileName);
            using (var client = new System.Net.Http.HttpClient())
            {
                string scriptContent = await client.GetStringAsync(url);
                File.WriteAllText(tempFile, scriptContent, Encoding.UTF8);
            }
            var process = new Process();
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Verb = "runas";
            process.Start();
            await Task.Run(() => process.WaitForExit());
            File.Delete(tempFile);
        }

        private void CaricaInfoSistema()
        {
            string os = Environment.OSVersion.VersionString;

            MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(mem);
            DriveInfo drive = new DriveInfo("C");

            richTextBox1.Clear();
            richTextBox1.AppendText($"OS:       {os}\n");
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
            rtbDispositivo.ReadOnly = true;
        }

        private (string serial, string model, string manufacturer) GetComputerInfo()
        {
            string serial = "N/A";
            string model = "N/A";
            string manufacturer = "N/A";

            using (var s1 = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
                foreach (var obj in s1.Get())
                    serial = obj["SerialNumber"]?.ToString() ?? "N/A";

            using (var s2 = new ManagementObjectSearcher("SELECT Model, Manufacturer FROM Win32_ComputerSystem"))
                foreach (var obj in s2.Get())
                {
                    model = obj["Model"]?.ToString() ?? "N/A";
                    manufacturer = obj["Manufacturer"]?.ToString() ?? "N/A";
                }

            return (serial, model, manufacturer);
        }

        private string GetSupportUrl(string model, string serial, string manufacturer)
        {
            string m = manufacturer.ToLower();

            if (m.Contains("dell"))
                return $"https://www.dell.com/support/home/it-it/product-support/servicetag/{Uri.EscapeDataString(serial)}";

            if (m.Contains("lenovo"))
                return $"https://pcsupport.lenovo.com/it/it/products/{Uri.EscapeDataString(model)}";

            if (m.Contains("hp") || m.Contains("hewlett"))
                return $"https://support.hp.com/it-it/product/details/{Uri.EscapeDataString(serial)}";

            if (m.Contains("asus"))
                return $"https://www.asus.com/it/support/?ModelName={Uri.EscapeDataString(model)}";

            if (m.Contains("acer"))
                return $"https://www.acer.com/it/support/?sn={Uri.EscapeDataString(serial)}";

            if (m.Contains("microsoft"))
                return $"https://support.microsoft.com/it-it/surface";

            return $"https://www.bing.com/search?q={Uri.EscapeDataString(manufacturer + " " + model + " support")}";
        }

        private void btnImmagine_Click(object sender, EventArgs e)
        {
            string url = GetSupportUrl(_model, _serial, _manufacturer);

            Form form2 = new Form();
            form2.Text = $"Supporto PC - {_manufacturer} {_model}";
            form2.Size = new System.Drawing.Size(1100, 700);
            form2.StartPosition = FormStartPosition.CenterScreen;

            var webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            form2.Controls.Add(webView);

            webView.CoreWebView2InitializationCompleted += (s, e2) =>
            {
                webView.CoreWebView2.Navigate(url);
            };

            form2.Load += async (s, e2) =>
            {
                await webView.EnsureCoreWebView2Async();
            };

            form2.Show();
        }

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
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private void btnRiavvia_Click(object sender, EventArgs e)
        {
            Process.Start("shutdown.exe", "/r /t 0");
        }

        private void btnInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("EsACaso v1.0\n\nQuesto programma raccoglie informazioni sul sistema e sull'hardware, e può eseguire uno script PowerShell remoto per configurare il sistema.\n\nSviluppato da Miner.", "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}