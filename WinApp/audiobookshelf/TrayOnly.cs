using System;
using System.Diagnostics;
using System.Windows.Forms;
using audiobookshelf.Properties;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

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

        private readonly NotifyIcon trayIcon;
        private readonly string PORT = "13378";
        private readonly string absDataPath;
        private readonly string appFolderName = "audiobookshelf";
        private readonly string absServerFilename = "server.exe";
        private readonly ToolStripMenuItem StopServerToolStripMenuItem;
        private readonly ToolStripMenuItem StartServerToolStripMenuItem;
        private readonly ToolStripMenuItem OpenWebToolStripMenuItem;
        private readonly string absServerPath;

        private string currentServerVersion = null;

        public TrayOnly()
        {
            StopServerToolStripMenuItem = new ToolStripMenuItem("Stop Server", null, StopServer) { Enabled = false };
            StartServerToolStripMenuItem = new ToolStripMenuItem("Start Server", null, StartServer) { Enabled = false };
            OpenWebToolStripMenuItem = new ToolStripMenuItem("Open Web App", null, Open) { Enabled = false };

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.AppIcon,
                ContextMenuStrip = new ContextMenuStrip()
                {
                    Items = {
                        new ToolStripMenuItem("Check for Server Update", null, CheckForServerUpdate),
                        new ToolStripSeparator(),
                        StopServerToolStripMenuItem,
                        StartServerToolStripMenuItem,
                        new ToolStripSeparator(),
                        OpenWebToolStripMenuItem,
                        new ToolStripMenuItem("Quit", null, Exit)
                    }
                },
                Visible = true
            };
            
            trayIcon.DoubleClick += Open;

            absDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appFolderName);
            absServerPath = Path.Combine(absDataPath, absServerFilename);
            Debug.WriteLine("Abs Data Path = " + absDataPath);

            init();
        }

        private async void init()
        {
            currentServerVersion = Settings.Default.ServerVersion;

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
                    if (MessageBox.Show(String.Format("Server update is available to {0}. Install?", cliAsset.tag), "Audiobookshelf", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        downloadServerCli(cliAsset);
                        return;
                    };
                } else
                {
                    Debug.WriteLine("Server is up-to-date!");
                }
            }

            OpenWebToolStripMenuItem.Enabled = true;
            StartServerToolStripMenuItem.Enabled = true;
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

                if (currentServerVersion != cliAsset.tag)
                {
                    MessageBox.Show(string.Format("Server update is available to {0} from current {1}", cliAsset.tag, currentServerVersion));
                }
                else
                {
                    MessageBox.Show("Server is up-to-date!");
                }
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
                        FileName = absServerPath,
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
                MessageBox.Show("Download Complete for " + releaseCliAsset.tag);

                // Save latest server version in user settings
                Settings.Default.ServerVersion = releaseCliAsset.tag;
                Settings.Default.Save();

                StartServerToolStripMenuItem.Enabled = true;
                OpenWebToolStripMenuItem.Enabled = true;
            } else
            {
                MessageBox.Show(
                   "Failed to download server",
                   "Download failed",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error);
            }
        }
    }

}