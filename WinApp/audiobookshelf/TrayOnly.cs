using System;
using System.Diagnostics;
using System.Windows.Forms;
using audiobookshelf.Properties;
using System.IO;

namespace audiobookshelf
{
    public class TrayOnly : ApplicationContext
    {
        public Process ab = null;

        private readonly NotifyIcon trayIcon;
        private readonly string PORT = "13378";
        private readonly string absDataPath;
        private readonly string appFolderName = "audiobookshelf";
        private readonly string absServerFilename = "server.exe";
        private readonly ToolStripMenuItem StopServerToolStripMenuItem;
        private readonly ToolStripMenuItem StartServerToolStripMenuItem;

        public TrayOnly()
        {
            StopServerToolStripMenuItem = new ToolStripMenuItem("Stop Server", null, StopServer) { Enabled = false };
            StartServerToolStripMenuItem = new ToolStripMenuItem("Start Server", null, StartServer);

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        StopServerToolStripMenuItem,
                        StartServerToolStripMenuItem,
                        new ToolStripSeparator(),
                        new ToolStripMenuItem("Open Web App", null, Open),
                        new ToolStripMenuItem("Quit", null, Exit)
                    }
                },
                Visible = true
            };
            
            trayIcon.DoubleClick += Open;

            //trayIcon.DoubleClick += OpenOrAlert;
            absDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName);
            
            Debug.WriteLine("Abs Data Path = " + absDataPath);
        }

        public void Exit(object sender, EventArgs e)
        {
            stopService();
            trayIcon.Visible = false;
            Application.Exit();
        }

        public void StopServer(object sender, EventArgs e)
        {
            stopService();
        }

        public void StartServer(object sender, EventArgs e)
        {
            startService();
        }

        public void Open(object sender, EventArgs e)
        {
            // Server already started, 
            if (ab != null)
            {
                // just open the browser.
                openBrowser(PORT);
            }
            
            // Server not started, 
            else
            {
                // ask master if we should start it.
                if (MessageBox.Show("Server not started, start server?", "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    // Lets do what our master told us to do.
                    startService();
                };
            }
        }

        private void stopService()
        {
            if (ab != null)
            {
                StopServerToolStripMenuItem.Enabled = false;
                StartServerToolStripMenuItem.Enabled = true;

                ab.Kill();
                ab = null;
                trayIcon.ShowBalloonTip(500, "Audiobookshelf", "Server stopped", ToolTipIcon.Info);
                Debug.WriteLine("Killed process");
            }
        }

        private void startService()
        {
            if (ab == null)
            {
                Debug.WriteLine("Starting service");

                string configPath = Path.Combine(absDataPath, "config");
                string metadataPath = Path.Combine(absDataPath, "metadata");

                ab = new Process
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        Arguments = "-p " + PORT + " --config " + configPath + " --metadata " + metadataPath + " --source windows",
                        FileName = Path.Combine(absDataPath, absServerFilename),
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                // Why?
                //ab.OutputDataReceived += new DataReceivedEventHandler(OuputHandler);

                // Start the ABS Server process.
                ab.Start();

                // Is this even needed???? Make sure ab server process gets stopped even if parent is killed
                //ChildProcessTracker.AddProcess(ab);

                // Why?
                //ab.BeginOutputReadLine();

                // Show an alert that we started the server.
                trayIcon.ShowBalloonTip(500, "Audiobookshelf", "Server started", ToolTipIcon.Info);

                // Fix up the context menu stuff
                StartServerToolStripMenuItem.Enabled = false;
                StopServerToolStripMenuItem.Enabled = true;
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

        // Why??
        //private void OuputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        //{
        //    // Console.WriteLine(outLine.Data);
        //    Debug.WriteLine(outLine.Data);
        //}
    }

}