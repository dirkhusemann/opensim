/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.IO;
using System.Reflection;
using System.Timers;
using log4net;
using OpenSim.Framework.Console;
using OpenSim.Framework.Statistics;

namespace OpenSim.Framework.Servers
{
    /// <summary>
    /// Common base for the main OpenSimServers (user, grid, inventory, region, etc)
    /// </summary>
    public abstract class BaseOpenSimServer
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// This will control a periodic log printout of the current 'show stats' (if they are active) for this
        /// server.
        /// </summary>
        private Timer m_periodicLogStatsTimer = new Timer(20 * 60 * 1000);
        
        protected ConsoleBase m_console;

        /// <summary>
        /// Time at which this server was started
        /// </summary>
        protected DateTime m_startuptime;

        /// <summary>
        /// Server version information.  Usually VersionInfo + information about svn revision, operating system, etc.
        /// </summary>
        protected string m_version;

        protected BaseHttpServer m_httpServer;
        public BaseHttpServer HttpServer
        {
            get { return m_httpServer; }
        }

        /// <summary>
        /// Holds the non-viewer statistics collection object for this service/server
        /// </summary>
        protected IStatsCollector m_stats;

        public BaseOpenSimServer()
        {
            m_startuptime = DateTime.Now;
            m_version = VersionInfo.Version;
            
            m_periodicLogStatsTimer.Elapsed += new ElapsedEventHandler(LogStats);            
            m_periodicLogStatsTimer.Enabled = true;
        }
                
        /// <summary>
        /// Print statistics to the logfile, if they are active
        /// </summary>
        protected void LogStats(object source, ElapsedEventArgs e)
        {
            if (m_stats != null)
            {
                m_log.Info(m_stats.Report());
            }            
        }

        /// <summary>
        /// Performs initialisation of the scene, such as loading configuration from disk.
        /// </summary>
        public virtual void Startup()
        {
            m_log.Info("[STARTUP]: Beginning startup processing");

            EnhanceVersionInformation();

            m_log.Info("[STARTUP]: Version " + m_version + "\n");
        }

        /// <summary>
        /// Should be overriden and referenced by descendents if they need to perform extra shutdown processing
        /// </summary>
        public virtual void Shutdown()
        {
            m_log.Info("[SHUTDOWN]: Shutdown processing on main thread complete.  Exiting...");

            Environment.Exit(0);
        }

        /// <summary>
        /// Runs commands issued by the server console from the operator
        /// </summary>
        /// <param name="command">The first argument of the parameter (the command)</param>
        /// <param name="cmdparams">Additional arguments passed to the command</param>
        public virtual void RunCmd(string command, string[] cmdparams)
        {
            switch (command)
            {
                case "help":
                    Notice("quit - equivalent to shutdown.");

                    if (m_stats != null)
                        Notice("show stats - statistical information for this server");
                    
                    Notice("show uptime - show server startup time and uptime.");
                    Notice("show version - show server version.");
                    Notice("shutdown - shutdown the server.\n");

                    break;

                case "show":
                    if (cmdparams.Length > 0)
                    {
                        Show(cmdparams[0]);
                    }
                    break;

                case "quit":
                case "shutdown":
                    Shutdown();
                    break;
            }
        }

        /// <summary>
        /// Outputs to the console information about the region
        /// </summary>
        /// <param name="ShowWhat">What information to display (valid arguments are "uptime", "users")</param>
        public virtual void Show(string ShowWhat)
        {
            switch (ShowWhat)
            {
                case "stats":
                    if (m_stats != null)
                    {
                        Notice(m_stats.Report());
                    }
                    break;

                case "uptime":
                    Notice("Server has been running since " + m_startuptime.DayOfWeek + ", " + m_startuptime.ToString());
                    Notice("That is an elapsed time of " + (DateTime.Now - m_startuptime).ToString());
                    break;

                case "version":
                    m_console.Notice("This is " + m_version);
                    break;
            }
        }

        /// <summary>
        /// Console output is only possible if a console has been established.
        /// That is something that cannot be determined within this class. So
        /// all attempts to use the console MUST be verified.
        /// </summary>
        private void Notice(string msg)
        {
            if (m_console != null)
            {
                m_console.Notice(msg);
            }
        }

        /// <summary>
        /// Enhance the version string with extra information if it's available.
        /// </summary>
        protected void EnhanceVersionInformation()
        {
            string buildVersion = string.Empty;

            // Add subversion revision information if available
            // FIXME: Making an assumption about the directory we're currently in - we do this all over the place
            // elsewhere as well
            string svnFileName = "../.svn/entries";
            string inputLine;
            int strcmp;

            if (File.Exists(svnFileName))
            {
                StreamReader EntriesFile = File.OpenText(svnFileName);
                inputLine = EntriesFile.ReadLine();
                while (inputLine != null)
                {
                    // using the dir svn revision at the top of entries file
                    strcmp = String.Compare(inputLine, "dir");
                    if (strcmp == 0)
                    {
                        buildVersion = EntriesFile.ReadLine();
                        break;
                    }
                    else
                    {
                        inputLine = EntriesFile.ReadLine();
                    }
                }
                EntriesFile.Close();
            }

            if (!string.IsNullOrEmpty(buildVersion))
            {
                m_version += ", SVN build r" + buildVersion;
            }
            else
            {
                m_version += ", SVN build revision not available";
            }

            // Add operating system information if available
            string OSString = "";

            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
            {
                OSString = System.Environment.OSVersion.ToString();
            }
            else
            {
                OSString = Util.ReadEtcIssue();
            }
            if (OSString.Length > 45)
            {
                OSString = OSString.Substring(0, 45);
            }

            m_version += ", OS " + OSString;
        }
    }
}
