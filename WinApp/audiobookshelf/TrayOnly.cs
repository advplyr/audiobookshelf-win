using System;
using System.Diagnostics;
using System.Windows.Forms;
using audiobookshelf.Properties;
using System.IO;

namespace audiobookshelf
{
    public class TrayOnly : ApplicationContext
    {
        private readonly string PORT = "13378";
        public Process ab = null;
        private NotifyIcon trayIcon;
        private string absDataPath = "";
        private string absServerPath = "";

        private ToolStripMenuItem StopServerToolStripMenuItem;
        private ToolStripMenuItem StartServerToolStripMenuItem;

        public TrayOnly()
        {
            StopServerToolStripMenuItem = new ToolStripMenuItem("Stop Server", null, StopServer);
            StartServerToolStripMenuItem = new ToolStripMenuItem("Start Server", null, StartServer);

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        new ToolStripMenuItem("Quit", null, Exit),
                        new ToolStripMenuItem("Open Web App", null, Open)
                    }
                },
                Visible = true
            };

            trayIcon.DoubleClick += OpenOrAlert; 

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            absDataPath = Path.Combine(localAppDataPath, "audiobookshelf");
            Debug.WriteLine("Abs Data Path = " + absDataPath);

            absServerPath = Path.Combine(absDataPath, "server.exe");
            File.WriteAllBytes(absServerPath, Properties.Resources.audiobookshelf);
            Debug.WriteLine("Server written to " + absServerPath);

            startService();
        }

        void Exit(object? sender, EventArgs e)
        {
            stopService();
            trayIcon.Visible = false;
            Application.Exit();
        }

        void StopServer(object? sender, EventArgs e)
        {
            stopService();
        }

        void StartServer(object? sender, EventArgs e)
        {
            startService();
        }

        void VersionCheck(object? sender, EventArgs e)
        {
            Debug.WriteLine("Version Check");
        }

        void Open(object? sender, EventArgs e)
        {
            if (ab != null)
            {
                openBrowser(PORT);
            }
        }

        void OpenOrAlert(object? sender, EventArgs e)
        {
            if (ab != null)
            {
                openBrowser(PORT);
            } else
            {
                MessageBox.Show("Server not started", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void stopService()
        {
            if (ab != null)
            {
                trayIcon.ContextMenuStrip.Items.Add(StartServerToolStripMenuItem);
                trayIcon.ContextMenuStrip.Items.Remove(StopServerToolStripMenuItem);

                ab.Kill();
                ab = null;
                trayIcon.ShowBalloonTip(500, "Audiobookshelf", "Server stopped", ToolTipIcon.Info);
                Debug.WriteLine("Killed process");
            }
        }

        public void startService()
        {
            if (ab != null)
            {
                stopService();
            }
            else
            {
                Debug.WriteLine("Starting service");

                string configPath = Path.Combine(absDataPath, "config");
                string metadataPath = Path.Combine(absDataPath, "metadata");

                ProcessStartInfo start = new ProcessStartInfo();
                start.Arguments = "-p " + PORT + " --config " + configPath + " --metadata " + metadataPath;
                start.FileName = absServerPath;

                start.WindowStyle = ProcessWindowStyle.Hidden;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = true;

                ab = new Process();
                ab.StartInfo = start;
                ab.EnableRaisingEvents = true;

                ab.OutputDataReceived += new DataReceivedEventHandler(OuputHandler);

                ab.Start();

                // Make sure ab server process gets stopped even if parent is killed
                ChildProcessTracker.AddProcess(ab);

                ab.BeginOutputReadLine();

                trayIcon.ShowBalloonTip(500, "Audiobookshelf", "Server started | Click to open browser", ToolTipIcon.Info);

                trayIcon.BalloonTipClicked += Open;


                trayIcon.ContextMenuStrip.Items.Remove(StartServerToolStripMenuItem);
                trayIcon.ContextMenuStrip.Items.Add(StopServerToolStripMenuItem);
            }
        }

        private void openBrowser(string port)
        {
            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = "http://localhost:" + port,
                UseShellExecute = true
            };
            Process.Start(psInfo);
        }


        private void OuputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            // Console.WriteLine(outLine.Data);
            Debug.WriteLine(outLine.Data);
        }
    }

}
