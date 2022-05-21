using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace audiobookshelf
{
    partial class ServerLogs
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public void setLogs(List<string> logLines)
        {
            Debug.WriteLine("Setting Logs " + logLines.Count);
            string[] linesArray = logLines.ToArray();

            for (int i = 0; i < linesArray.Length; i++)
            {
                Debug.WriteLine("LOOKING AT LINE = " + linesArray[i]);
            }

            if (linesArray != null && linesArray.Length > 0)
            {
                logsListBox.Invoke((MethodInvoker)delegate
                {
                    logsListBox.Items.AddRange(linesArray);
                    logsListBox.SelectedIndex = logsListBox.Items.Count - 1;
                });
         
            } else
            {
                Debug.WriteLine("Error Invalid logLines");
            }
        }

        public void addLogLine(string line)
        {
            if (line == "" || line == null) return;

            logsListBox.Invoke((MethodInvoker)delegate
            {
                logsListBox.Items.Add(line);
                logsListBox.SelectedIndex = logsListBox.Items.Count - 1;
            });
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.logsListBox = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // logsListBox
            // 
            this.logsListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logsListBox.FormattingEnabled = true;
            this.logsListBox.ItemHeight = 15;
            this.logsListBox.Location = new System.Drawing.Point(12, 12);
            this.logsListBox.Name = "logsListBox";
            this.logsListBox.Size = new System.Drawing.Size(527, 394);
            this.logsListBox.TabIndex = 0;
            // 
            // ServerLogs
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 418);
            this.Controls.Add(this.logsListBox);
            this.Name = "ServerLogs";
            this.Text = "Server Logs";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox logsListBox;
    }
}