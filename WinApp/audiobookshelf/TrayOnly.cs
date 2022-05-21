using System;
using System.Diagnostics;
using System.Windows.Forms;
using audiobookshelf.Properties;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;
using System.Collections.Generic;

namespace audiobookshelf
{
    class ReleaseCliAsset
    {
        public string tag { get; set; }
        public string downloadUrl { get; set; }
        public string url { get; set; }
    }

    public class TrayOnly : ApplicationContext
    {
        public Process ab = null;
        public ServerLogs serverLogsForm = null;

        private readonly NotifyIcon trayIcon;
        private readonly string PORT = "13378";
        private readonly string absDataPath;
        private readonly string appFolderName = "audiobookshelf";
        private readonly string absServerFilename = "server.exe";
        private readonly ToolStripMenuItem StopServerToolStripMenuItem;
        private readonly ToolStripMenuItem StartServerToolStripMenuItem;
        private readonly ToolStripMenuItem OpenWebToolStripMenuItem;
        private readonly ToolStripMenuItem ServerLogsToolStripMenuItem;
        private readonly ToolStripLabel VersionToolStripLabel;
        private readonly string absServerPath;

        private List<string> serverLogList = new List<string>();

        public TrayOnly()
        {
            VersionToolStripLabel = new ToolStripLabel("Server Not Installed", null, false, OpenCurrentRelease);
            VersionToolStripLabel.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);

            StopServerToolStripMenuItem = new ToolStripMenuItem("Stop Server", null, StopServer) { Enabled = false };
            StartServerToolStripMenuItem = new ToolStripMenuItem("Start Server", null, StartServer) { Enabled = false };
            ServerLogsToolStripMenuItem = new ToolStripMenuItem("Server Logs", null, ShowServerLogs) { Enabled = true };
            OpenWebToolStripMenuItem = new ToolStripMenuItem("Open Web App", null, Open) { Enabled = false };


            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        VersionToolStripLabel,
                        new ToolStripMenuItem("Check for Server Update", null, CheckForServerUpdate),
                        new ToolStripSeparator(),
                        StopServerToolStripMenuItem,
                        StartServerToolStripMenuItem,
                        ServerLogsToolStripMenuItem,
                        new ToolStripSeparator(),
                        OpenWebToolStripMenuItem,
                        new ToolStripMenuItem("Quit", null, Exit)
                    }
                },
                Visible = true
            };
            
            trayIcon.DoubleClick += Open;
            trayIcon.BalloonTipClicked += BalloonTipClicked;

            absDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName);
            absServerPath = Path.Combine(absDataPath, absServerFilename);
            Debug.WriteLine("Abs Data Path = " + absDataPath);

            init();
        }

        private async void init()
        {
            string currentServerVersion = Settings.Default.ServerVersion;

            Debug.WriteLine(format: "Current server version downloaded is {0}", currentServerVersion);

            // Fetch latest release on start
            ReleaseCliAsset cliAsset = await fetchReleaseFeedAsync();
            if (cliAsset == null)
            {
                Debug.WriteLine("Failed to get latest release cli");

                if (currentServerVersion == null || currentServerVersion == "")
                {
                    Debug.WriteLine("Error no cli asset found and no server installed");
                    MessageBox.Show("Could not find server to download", "Audiobookshelf", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            } else
            {
                Debug.WriteLine(format: "Got latest release server for {0}", cliAsset.tag);

                if (currentServerVersion == null || currentServerVersion == "")
                {
                    Debug.WriteLine("No server installed - starting download");
                    downloadServerCli(cliAsset);
                    return;
                } else if (currentServerVersion != cliAsset.tag)
                {
                    Debug.WriteLine(format: "Update is available for server to {0} from current {1}", cliAsset.tag, currentServerVersion);
                    if (MessageBox.Show(string.Format("Server update is available to {0}. Do you want to update?", cliAsset.tag), "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        downloadServerCli(cliAsset);
                        return;
                    };
                } else
                {
                    Debug.WriteLine("Server is up-to-date!");
                }
            }

            setVersionTooltip();
            VersionToolStripLabel.IsLink = true;
            OpenWebToolStripMenuItem.Enabled = true;
            StartServerToolStripMenuItem.Enabled = true;

            // Start server
            startService();
        }

        public void Exit(object sender, EventArgs e)
        {
            stopService();
            trayIcon.Visible = false;
            System.Windows.Forms.Application.Exit();
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
                openBrowser();
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

        public async void CheckForServerUpdate(object sender, EventArgs e)
        {
            // TODO: this is duplicative from code above just doesnt download
            ReleaseCliAsset cliAsset = await fetchReleaseFeedAsync();
            if (cliAsset == null)
            {
                Debug.WriteLine("Failed to get latest release cli");
            }
            else
            {
                Debug.WriteLine(format: "Got latest release server for {0}", cliAsset.tag);

                if (Settings.Default.ServerVersion != cliAsset.tag)
                {
                    if (MessageBox.Show(string.Format("Server update is available to {0}. Do you want to update?", cliAsset.tag), "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        downloadServerCli(cliAsset);
                        return;
                    };
                }
                else
                {
                    MessageBox.Show("Server is up-to-date!");
                }
            }
        }

        public void ShowServerLogs(object sender, EventArgs e)
        {
            serverLogsForm = new ServerLogs();
            serverLogsForm.Show();
            serverLogsForm.setLogs(serverLogList);
        }

        public void OpenCurrentRelease(object sender, EventArgs e)
        {
            string currentVersion = Settings.Default.ServerVersion;
            if (currentVersion == null || currentVersion == "") return;

            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = "https://github.com/advplyr/audiobookshelf/releases/tag/" + currentVersion,
                UseShellExecute = true
            };
            Process.Start(psInfo);
        }

        public void BalloonTipClicked(object sender, EventArgs e)
        {
            openBrowser();
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
                        FileName = absServerPath,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                ab.OutputDataReceived += new DataReceivedEventHandler(OuputHandler);

                ab.ErrorDataReceived += Ab_ErrorDataReceived;

                // Start the ABS Server process.
                ab.Start();

                ab.BeginOutputReadLine();

                // Show an alert that we started the server.
                trayIcon.ShowBalloonTip(500, "Audiobookshelf", "Server started", ToolTipIcon.Info);

                // Fix up the context menu stuff
                StartServerToolStripMenuItem.Enabled = false;
                StopServerToolStripMenuItem.Enabled = true;
            }
        }

        private void Ab_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine("Error Data Received " + e.Data);
        }

        private void openBrowser()
        {
            if (ab == null) return;

            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = "http://localhost:" + PORT,
                UseShellExecute = true
            };
            Process.Start(psInfo);
        }

        // Why??
        private void OuputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (outLine.Data == null || outLine.Data == "")
            {
                return;
            }

            Debug.WriteLine(outLine.Data);
            serverLogList.Add(outLine.Data);

            if (serverLogsForm != null && !serverLogsForm.IsDisposed)
            {
                // Server logs form is open add line
                serverLogsForm.addLogLine(outLine.Data);
            }
        }


        // Get latest release with server cli download url
        private async Task<ReleaseCliAsset> fetchReleaseFeedAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("audiobookshelf"));
            var releases = await client.Repository.Release.GetAll("advplyr", "audiobookshelf");
            var latest = releases[0];
            Debug.WriteLine(format: "The latest release is tagged at {0}", latest.TagName);

            string absCliDownloadUrl = null;

            // TODO: easier way to loop through Assets?
            for (int i = 0; i < latest.Assets.Count; i++)
            {
                var asset = latest.Assets[i];
                if (asset.Name == "audiobookshelf-cli.exe")
                {
                    absCliDownloadUrl = asset.BrowserDownloadUrl;
                    break;
                }
            }

            if (absCliDownloadUrl == null) return null;

            Debug.WriteLine(format: "Found server cli asset download url {0}", absCliDownloadUrl);
            ReleaseCliAsset releaseCliAsset = new ReleaseCliAsset();
            releaseCliAsset.downloadUrl = absCliDownloadUrl;
            releaseCliAsset.tag = latest.TagName;
            releaseCliAsset.url = latest.HtmlUrl;
            return releaseCliAsset;
        }

        private async void downloadServerCli(ReleaseCliAsset releaseCliAsset)
        {
            if (ab != null) // Stop server if started
            {
                stopService();
            }

            string downloadUrl = releaseCliAsset.downloadUrl;
            Debug.WriteLine("Starting server dl url " + downloadUrl);

            HttpClient client = new HttpClient();
            var response = await client.GetAsync(downloadUrl);
 
            await using (var fs = new FileStream(absServerPath, System.IO.FileMode.Create)) // Overwrite existing
            {
                Debug.WriteLine("Opened file stream to " + absServerPath);
                await response.Content.CopyToAsync(fs);
            }

            if (response.IsSuccessStatusCode)
            {
                trayIcon.ShowBalloonTip(500, "Audiobookshelf", String.Format("Server {0} downloaded successfully", releaseCliAsset.tag), ToolTipIcon.Info);

                // Save latest server version in user settings
                Settings.Default.ServerVersion = releaseCliAsset.tag;
                Settings.Default.Save();

                setVersionTooltip();
                StartServerToolStripMenuItem.Enabled = true;
                OpenWebToolStripMenuItem.Enabled = true;

                // Start server
                startService();
            } else
            {
                MessageBox.Show(
                   "Failed to download server",
                   "Download failed",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error);
            }
        }

        private void setVersionTooltip()
        {
            VersionToolStripLabel.Text = "Server " + Settings.Default.ServerVersion;
            VersionToolStripLabel.IsLink = true;
 
        }
    }

}