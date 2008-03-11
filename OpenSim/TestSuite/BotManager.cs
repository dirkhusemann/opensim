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
* 
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using libsecondlife;
using libsecondlife.Packets;
using Nini.Config;
using System.Threading;
using OpenSim.Framework;
using OpenSim.Framework.Console;

namespace OpenSim.TestSuite
{
    /// <summary>
    /// Thread/Bot manager for the application
    /// </summary>
    public class BotManager : conscmd_callback
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected ConsoleBase m_console;
        protected List<PhysicsBot> m_lBot;
        protected Thread[] m_td;
        protected bool m_verbose = true;
        protected Random somthing = new Random(System.Environment.TickCount);
        protected int numbots = 0;
        protected IConfig Previous_config;
        
        /// <summary>
        /// Constructor Creates MainConsole.Instance to take commands and provide the place to write data
        /// </summary>
        public BotManager()
        {
            m_log.Info("In bot manager");
            // m_console = CreateConsole();
            // MainConsole.Instance = m_console;
            m_lBot = new List<PhysicsBot>();
        }

        /// <summary>
        /// Startup number of bots specified in the starting arguments
        /// </summary>
        /// <param name="botcount">How many bots to start up</param>
        /// <param name="cs">The configuration for the bots to use</param>
        public void dobotStartup(int botcount, IConfig cs)
        {
            Previous_config = cs;
            m_td = new Thread[botcount];
            for (int i = 0; i < botcount; i++)
            {
                startupBot(i, cs);
            }
        }

        /// <summary>
        /// Add additional bots (and threads) to our bot pool
        /// </summary>
        /// <param name="botcount">How Many of them to add</param>
        public void addbots(int botcount)
        {
            int len = m_td.Length;
            Thread[] m_td2 = new Thread[len + botcount];
            for (int i = 0; i < len; i++)
            {
                m_td2[i] = m_td[i];
            }
            m_td = m_td2;
            int newlen = len + botcount;
            for (int i = len; i < newlen; i++)
            {
                startupBot(i, Previous_config);
            }
        }
       
        /// <summary>
        /// This starts up the bot and stores the thread for the bot in the thread array
        /// </summary>
        /// <param name="pos">The position in the thread array to stick the bot's thread</param>
        /// <param name="cs">Configuration of the bot</param>
        public void startupBot(int pos, IConfig cs)
        {
            PhysicsBot pb = new PhysicsBot(cs);

            pb.OnConnected += handlebotEvent;
            pb.OnDisconnected += handlebotEvent;
            if (cs.GetString("firstname", "random") == "random") pb.firstname = CreateRandomName();
            if (cs.GetString("lastname", "random") == "random") pb.lastname = CreateRandomName();

            m_td[pos] = new Thread(pb.startup);
            m_td[pos].Name = "CampBot_" + pos;
            m_td[pos].IsBackground = true;
            m_td[pos].Start();
            m_lBot.Add(pb);
            OpenSim.Framework.ThreadTracker.Add(m_td[pos]);
        }

        /// <summary>
        /// Creates a random name for the bot
        /// </summary>
        /// <returns></returns>
        private string CreateRandomName()
        {
            string returnstring = "";
            string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            
            for (int i = 0; i < 7; i++)
            {
                returnstring += chars.Substring(somthing.Next(chars.Length),1);
            }
            return returnstring;
        }

        /// <summary>
        /// High level connnected/disconnected events so we can keep track of our threads by proxy
        /// </summary>
        /// <param name="callbot"></param>
        /// <param name="eventt"></param>
        public void handlebotEvent(PhysicsBot callbot, EventType eventt)
        {
            switch (eventt)
            {
                case EventType.CONNECTED:
                    m_log.Info("[ " + callbot.firstname + " " + callbot.lastname + "]: Connected");
                    numbots++;
                    break;
                case EventType.DISCONNECTED:
                    m_log.Info("[ " + callbot.firstname + " " + callbot.lastname + "]: Disconnected");
                    m_td[m_lBot.IndexOf(callbot)].Abort();
                    numbots--;
                    if (numbots >1)
                        Environment.Exit(0);
                    break;
            }
        }

        /// <summary>
        /// Shutting down all bots
        /// </summary>
        public void doBotShutdown()
        {
            foreach (PhysicsBot pb in m_lBot)
            {
                pb.shutdown();
            }
        }

        /// <summary>
        /// Standard CreateConsole routine
        /// </summary>
        /// <returns></returns>
        protected ConsoleBase CreateConsole()
        {
            return new ConsoleBase("Region", this);
        }

        /// <summary>
        /// I don't think the bots use this..     
        /// </summary>
        /// <param name="commandParams"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        private string CombineParams(string[] commandParams, int pos)
        {
            string result = String.Empty;
            for (int i = pos; i < commandParams.Length; i++)
            {
                result += commandParams[i] + " ";
            }
            result = result.TrimEnd(' ');
            return result;
        }
        
        /// <summary>
        /// Command runnint tool..  Currently use it to add bots, shutdown and (dangerous)Forcequit
        /// </summary>
        /// <param name="command"></param>
        /// <param name="cmdparams"></param>
        public void RunCmd(string command, string[] cmdparams)
        {
            switch (command)
            {
                case "shutdown":
                    m_console.Warn("BOTMANAGER", "Shutting down bots");
                    doBotShutdown();
                    break;
                case "quit":
                    m_console.Warn("DANGER", "This should only be used to quit the program if you've already used the shutdown command and the program hasn't quit");
                    Environment.Exit(0);
                    break;
                case "addbots":
                    int newbots = 0;
                    Helpers.TryParse(cmdparams[0], out newbots);
                    
                    if (newbots > 0)
                        addbots(newbots);
                    break;
                case "help":
                    m_console.Notice("HELP", "\nshutdown - graceful shutdown\naddbots <n> - adds n bots to the test\nquit - forcequits, dangerous if you have not already run shutdown");
                    break;
            }
        }

        /// <summary>
        /// Required method to implement the conscmd_callback interface
        /// </summary>
        /// <param name="ShowWhat"></param>
        public void Show(string ShowWhat)
        {
        }
    }
}
