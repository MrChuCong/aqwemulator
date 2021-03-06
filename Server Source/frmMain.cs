﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

using AQWE.Core;
using AQWE.Data;
using AQWE.Net;

namespace AQWE
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            
            this.infoLogger = new textLogger(logInfo);
            this.warningLogger = new textLogger(logWarning);
            this.errorLogger = new textLogger(logError);
            this.clientLogger = new clientMessageLogger(logClientMessage);
            this.serverLogger = new serverMessageLogger(logServerMessage);
        }

        #region Startup
        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Show();
            Application.DoEvents();

            string logDir = Filesystem.startupPath + "/logs";
            if (!Filesystem.folderExists(logDir))
                Filesystem.createFolder(logDir);

            this.Boot();
        }

        private void writeInfo()
        {
            Logging.logHolyInfo("HardCore Emulator");
            Logging.logHolyInfo("Build: 1.0.7");
            Logging.logHolyInfo("- AdventureQuest Worlds Emulator.");
            this.writeBlank();
            Logging.logHolyInfo("Developed by Syntax & Divien.");
            Logging.logHolyInfo("Copyright © 2009-2010 Divi Developments. All rights reserved.");
            Logging.logHolyInfo("HardCore Emulator and/or Divi Developments are NOT affiliated with Artix Entertainment, LLC. in any way.");
            Application.DoEvents();
        }

        private void Boot()
        {
            this.writeInfo();
            this.writeBlank();
            Logging.logHolyInfo("Starting up...");
            Logging.logHolyInfo("Checking settings.ini...");
            Application.DoEvents();

            string settingFilePath = Filesystem.startupPath + @"\settings.ini";
            if (!Filesystem.fileExists(settingFilePath))
            {
                Logging.logHolyInfo("File 'settings.ini' wasn't present at " + settingFilePath + ".");
                Logging.logHolyInfo("System is going to use default settings.");
                this.writeBlank();
                Application.DoEvents();
            }
            else
            {
                Logging.logHolyInfo("settings.ini found. (" + settingFilePath + ")");
                this.writeBlank();
                Application.DoEvents();

                Settings.mysql_host = Filesystem.readINI("mysql", "host", settingFilePath);
                Settings.mysql_username = Filesystem.readINI("mysql", "username", settingFilePath);
                Settings.mysql_password = Filesystem.readINI("mysql", "password", settingFilePath);
                Settings.mysql_db = Filesystem.readINI("mysql", "database", settingFilePath);
                Settings.mysql_port = int.Parse(Filesystem.readINI("mysql", "port", settingFilePath));
            }

            if (!Database.openConnection(Settings.mysql_host, Settings.mysql_username, Settings.mysql_password, Settings.mysql_db, Settings.mysql_port))
            {
                this.Shutdown();
                return;
            }
            this.writeBlank();
            Application.DoEvents();

            Settings.Init();
            this.writeBlank();
            Database.runQuery("UPDATE servers SET max = '" + Settings.server_max_connections + "' WHERE name = '" + Settings.server_name + "'");
            Application.DoEvents();

            Preloader.Init();
            this.writeBlank();
            Application.DoEvents();

            if (!Sockets.Listen(Settings.server_port, Settings.server_max_connections, Settings.server_back_log))
            {
                this.Shutdown();
                return;
            }
            this.writeBlank();
            Application.DoEvents();

            Database.runQuery("UPDATE servers SET count = '0' WHERE name = '" + Settings.server_name + "'");
            Database.runQuery("UPDATE servers SET online = '1' WHERE name = '" + Settings.server_name + "'");

            Logging.logHolyInfo("AQW Emulator is ready for connections.");
        }

        public void updateOnlineUsers(int value)
        {
            this.statusOnlineUsers.Text = value.ToString();
            Database.runQuery("UPDATE servers SET count = " + value + " WHERE name = '" + Settings.server_name + "'");
        }

        private void Shutdown()
        {
            Logging.logHolyInfo("Shutting down...");
            Database.runQuery("UPDATE servers SET count = 0 WHERE name = '" + Settings.server_name + "'");
            Database.runQuery("UPDATE servers SET online = 0 WHERE name = '" + Settings.server_name + "'");
            Database.closeConnection();
            Logging.logHolyInfo("Shutdown complete.");
            Logging.logHolyInfo("Application will terminate in 3 seconds...");
            Application.DoEvents();
            Thread.Sleep(3000);
            Environment.Exit(2);
        }

        private void Shutdown(object noText)
        {
            Database.runQuery("UPDATE servers SET count = 0 WHERE name = '" + Settings.server_name + "'");
            Database.runQuery("UPDATE servers SET online = 0 WHERE name = '" + Settings.server_name + "'");
            Database.closeConnection();
            Application.DoEvents();
            Thread.Sleep(3000);
            Environment.Exit(2);
        }

        private void Shutdown(bool Server, bool mySQL)
        {
            if (Server)
            {
                Logging.logHolyInfo("Shutting down server...");
                Database.runQuery("UPDATE servers SET count = 0 WHERE name = '" + Settings.server_name + "'");
                Database.runQuery("UPDATE servers SET online = 0 WHERE name = '" + Settings.server_name + "'");
                Sockets.stopConnection();
                Logging.logHolyInfo("Server shutdown complete.");
                Application.DoEvents();
            }
            if (mySQL)
            {
                Logging.logHolyInfo("Shutting mySQL down...");
                Database.closeConnection();
                Logging.logHolyInfo("mySQL shutdown complete.");
                Application.DoEvents();
            }
        }
        #endregion

        #region Logging
        #region Declares
        private int countErrors = 0;
        private int countWarnings = 0;
        #region Delegates
        private delegate void asyncPointer();
        private delegate void textLogger(string Text);
        private delegate void clientMessageLogger(int connectionID, string Message);
        private delegate void serverMessageLogger(int connectionID, string Message);
        private textLogger infoLogger;
        private textLogger warningLogger;
        private textLogger errorLogger;
        private clientMessageLogger clientLogger;
        private serverMessageLogger serverLogger;
        #endregion
        #endregion

        #region Methods
        public void writeBlank()
        {
            this.txtLog.AppendText(Environment.NewLine);
            this.txtLog.SelectionStart = txtLog.Text.Length;
            this.txtLog.ScrollToCaret();
        }
        public void logInfo(string Text)
        {
            if (this.InvokeRequired)
                this.Invoke(infoLogger, Text);
            else
            {
                this.txtLog.AppendText(Text + Environment.NewLine);
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();
            }
        }
        public void logWarning(string Text)
        {
            if (this.InvokeRequired)
                this.Invoke(warningLogger, Text);
            else
            {
                StringBuilder Data = new StringBuilder();
                Data.Append("Warning!" + Environment.NewLine);
                Data.Append("Time: " + DateTime.Now.ToShortTimeString() + Environment.NewLine);
                Data.Append("Description: " + Text + Environment.NewLine + Environment.NewLine);

                this.writeBlank();
                this.txtLog.SelectionFont = new Font("Consolas", 8, FontStyle.Bold);
                this.txtLog.SelectionColor = Color.Orange;
                this.txtLog.SelectedText = Data.ToString();
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();

                countWarnings += 1;
                this.statusWarnings.Text = countWarnings.ToString();

                if (Logging.saveLogsToDisk)
                    Logging.writeToLogFile(Data.ToString());
            }
        }
        public void logError(string Text)
        {
            if (this.InvokeRequired)
                this.Invoke(errorLogger, Text);
            else
            {
                StackFrame ST = new StackTrace().GetFrame(2);
                StringBuilder Data = new StringBuilder();

                Data.Append("ERROR!" + Environment.NewLine);
                Data.Append("Time: " + DateTime.Now.ToShortTimeString() + Environment.NewLine);
                Data.Append("Assembly: " + ST.GetMethod().ReflectedType.Name + Environment.NewLine);
                Data.Append("Method: " + ST.GetMethod().Name + Environment.NewLine);
                Data.Append("Description: " + Text + Environment.NewLine + Environment.NewLine);

                this.txtLog.AppendText(Environment.NewLine);
                this.txtLog.SelectionFont = new Font("Consolas", 8, FontStyle.Bold);
                this.txtLog.SelectionColor = Color.Red;
                this.txtLog.SelectedText = Data.ToString();
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();

                countErrors += 1;
                this.statusErrors.Text = countErrors.ToString();

                if (Logging.saveLogsToDisk)
                    Logging.writeToLogFile(Data.ToString());
            }
        }
        public void logClientMessage(int connectionID, string Message)
        {
            if (this.InvokeRequired)
                this.Invoke(clientLogger, connectionID, Message);
            else
            {
                this.txtLog.AppendText("[" + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond + "]" + Environment.NewLine);
                this.txtLog.AppendText("Message from connection [" + connectionID + "]" + Environment.NewLine);
                this.txtLog.AppendText("    Message: " + Message);
                this.txtLog.Text += Environment.NewLine + Environment.NewLine;
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();
            }
        }
        public void logServerMessage(int connectionID, string Message)
        {
            if (this.InvokeRequired)
                this.Invoke(serverLogger, connectionID, Message);
            else
            {
                this.txtLog.AppendText("[" + DateTime.Now.ToLongTimeString() + ":" + DateTime.Now.Millisecond + "]" + Environment.NewLine);
                this.txtLog.AppendText("Sent message to connection [" + connectionID + "]" + Environment.NewLine);
                this.txtLog.AppendText("    Message: " + Message);
                this.txtLog.Text += Environment.NewLine + Environment.NewLine;
                this.txtLog.SelectionStart = txtLog.Text.Length;
                this.txtLog.ScrollToCaret();
            }
        }
        #endregion

        private void mnuItemExit_Click(object sender, EventArgs e)
        {
            this.Shutdown();
        }
        #endregion

        private void mnuItemLogInfoMessages_Click(object sender, EventArgs e)
        {
            Logging.logInfoMessages = !Logging.logInfoMessages;
            mnuItemLogInfoMessages.Checked = !mnuItemLogInfoMessages.Checked;
        }

        private void mnuItemLogWarnings_Click(object sender, EventArgs e)
        {
            Logging.logWarnings = !Logging.logWarnings;
            mnuItemLogWarnings.Checked = !mnuItemLogWarnings.Checked;
        }

        private void mnuItemLogErrors_Click(object sender, EventArgs e)
        {
            Logging.logErrors = !Logging.logErrors;
            mnuItemLogErrors.Checked = !mnuItemLogErrors.Checked;
        }

        private void mnuItemWriteLogsToFile_Click(object sender, EventArgs e)
        {
            Logging.saveLogsToDisk = !Logging.saveLogsToDisk;
            mnuItemWriteLogsToFile.Checked = !mnuItemWriteLogsToFile.Checked;
        }

        private void mnuItemLogClientMessages_Click(object sender, EventArgs e)
        {
            Logging.logClientMessages = !Logging.logClientMessages;
            mnuItemLogClientMessages.Checked = !mnuItemLogClientMessages.Checked;
        }

        private void mnuItemLogServerMessages_Click(object sender, EventArgs e)
        {
            Logging.logServerMessages = !Logging.logServerMessages;
            mnuItemLogServerMessages.Checked = !mnuItemLogServerMessages.Checked;
        }

        private void mnuItemClearLog_Click(object sender, EventArgs e)
        {
            this.txtLog.Clear();
            this.writeInfo();
            this.txtLog.SelectionStart = txtLog.Text.Length;
            this.txtLog.ScrollToCaret();
        }

        private void mnuItemServerStop_Click(object sender, EventArgs e)
        {
            this.Shutdown(true, true);
        }
    }
}
