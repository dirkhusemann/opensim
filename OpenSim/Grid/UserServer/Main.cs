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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using OpenMetaverse;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Statistics;
using OpenSim.Grid.Communications.OGS1;

namespace OpenSim.Grid.UserServer
{
    /// <summary>
    /// Grid user server main class
    /// </summary>
    public class OpenUser_Main : BaseOpenSimServer
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected UserConfig Cfg;

        public UserManager m_userManager;
        public UserLoginService m_loginService;
        public GridInfoService m_gridInfoService;
        public MessageServersConnector m_messagesService;

        private UUID m_lastCreatedUser = UUID.Random();

        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            m_log.Info("Launching UserServer...");

            OpenUser_Main userserver = new OpenUser_Main();

            userserver.Startup();
            userserver.Work();
        }

        public OpenUser_Main()
        {
            m_console = new ConsoleBase("User");
            MainConsole.Instance = m_console;
        }

        public void Work()
        {
            m_console.Notice("Enter help for a list of commands\n");

            while (true)
            {
                m_console.Prompt();
            }
        }

        protected override void StartupSpecific()
        {
            Cfg = new UserConfig("USER SERVER", (Path.Combine(Util.configDir(), "UserServer_Config.xml")));

            m_stats = StatsManager.StartCollectingUserStats();

            m_log.Info("[STARTUP]: Establishing data connection");
            
            IInterServiceInventoryServices inventoryService = new OGS1InterServiceInventoryService(Cfg.InventoryUrl);

            StartupUserManager(inventoryService);
            m_userManager.AddPlugin(Cfg.DatabaseProvider, Cfg.DatabaseConnect);

            m_gridInfoService = new GridInfoService();

            StartupLoginService(inventoryService);

            m_messagesService = new MessageServersConnector();

            m_loginService.OnUserLoggedInAtLocation += NotifyMessageServersUserLoggedInToLocation;
            m_userManager.OnLogOffUser += NotifyMessageServersUserLoggOff;

            m_messagesService.OnAgentLocation += HandleAgentLocation;
            m_messagesService.OnAgentLeaving += HandleAgentLeaving;
            m_messagesService.OnRegionStartup += HandleRegionStartup;
            m_messagesService.OnRegionShutdown += HandleRegionShutdown;

            m_log.Info("[STARTUP]: Starting HTTP process");

            m_httpServer = new BaseHttpServer(Cfg.HttpPort);
            AddHttpHandlers();
            m_httpServer.Start();
            
            base.StartupSpecific();

            m_console.Commands.AddCommand("userserver", false, "create user",
                    "create user [<first> [<last> [<x> <y> [email]]]]",
                    "Create a new user account", RunCommand);

            m_console.Commands.AddCommand("userserver", false, "reset user password",
                    "reset user password [<first> [<last> [<new password>]]]",
                    "Reset a user's password", RunCommand);

            m_console.Commands.AddCommand("userserver", false, "login level",
                    "login level <level>",
                    "Set the minimum user level to log in", HandleLoginCommand);

            m_console.Commands.AddCommand("userserver", false, "login reset",
                    "login reset",
                    "Reset the login level to allow all users",
                    HandleLoginCommand);

            m_console.Commands.AddCommand("userserver", false, "login text",
                    "login text <text>",
                    "Set the text users will see on login", HandleLoginCommand);

            m_console.Commands.AddCommand("userserver", false, "test-inventory",
                    "test-inventory",
                    "Perform a test inventory transaction", RunCommand);

            m_console.Commands.AddCommand("userserver", false, "logoff-user",
                    "logoff-user <first> <last> <message>",
                    "Log off a named user", RunCommand);
        }

        /// <summary>
        /// Start up the user manager
        /// </summary>
        /// <param name="inventoryService"></param>
        protected virtual void StartupUserManager(IInterServiceInventoryServices inventoryService)
        {
            m_userManager = new UserManager(new OGS1InterServiceInventoryService(Cfg.InventoryUrl));
        }

        /// <summary>
        /// Start up the login service
        /// </summary>
        /// <param name="inventoryService"></param>
        protected virtual void StartupLoginService(IInterServiceInventoryServices inventoryService)
        {
            m_loginService = new UserLoginService(
                m_userManager, inventoryService, new LibraryRootFolder(Cfg.LibraryXmlfile), Cfg, Cfg.DefaultStartupMsg, new RegionProfileServiceProxy());
        }

        protected virtual void AddHttpHandlers()
        {
            m_httpServer.AddXmlRPCHandler("login_to_simulator", m_loginService.XmlRpcLoginMethod);

            m_httpServer.AddHTTPHandler("login", m_loginService.ProcessHTMLLogin);
            //
            // Get the minimum defaultLevel to access to the grid
            //
            m_loginService.setloginlevel((int)Cfg.DefaultUserLevel);

            if (Cfg.EnableLLSDLogin)
            {
                m_httpServer.SetDefaultLLSDHandler(m_loginService.LLSDLoginMethod);
            }

            m_httpServer.AddXmlRPCHandler("get_user_by_name", m_userManager.XmlRPCGetUserMethodName);
            m_httpServer.AddXmlRPCHandler("get_user_by_uuid", m_userManager.XmlRPCGetUserMethodUUID);
            m_httpServer.AddXmlRPCHandler("get_avatar_picker_avatar", m_userManager.XmlRPCGetAvatarPickerAvatar);
            m_httpServer.AddXmlRPCHandler("add_new_user_friend", m_userManager.XmlRpcResponseXmlRPCAddUserFriend);
            m_httpServer.AddXmlRPCHandler("remove_user_friend", m_userManager.XmlRpcResponseXmlRPCRemoveUserFriend);
            m_httpServer.AddXmlRPCHandler("update_user_friend_perms",
                                          m_userManager.XmlRpcResponseXmlRPCUpdateUserFriendPerms);
            m_httpServer.AddXmlRPCHandler("get_user_friend_list", m_userManager.XmlRpcResponseXmlRPCGetUserFriendList);
            m_httpServer.AddXmlRPCHandler("get_avatar_appearance", m_userManager.XmlRPCGetAvatarAppearance);
            m_httpServer.AddXmlRPCHandler("update_avatar_appearance", m_userManager.XmlRPCUpdateAvatarAppearance);
            m_httpServer.AddXmlRPCHandler("update_user_current_region", m_userManager.XmlRPCAtRegion);
            m_httpServer.AddXmlRPCHandler("logout_of_simulator", m_userManager.XmlRPCLogOffUserMethodUUID);
            m_httpServer.AddXmlRPCHandler("get_agent_by_uuid", m_userManager.XmlRPCGetAgentMethodUUID);
            m_httpServer.AddXmlRPCHandler("check_auth_session", m_userManager.XmlRPCCheckAuthSession);
            m_httpServer.AddXmlRPCHandler("set_login_params", m_loginService.XmlRPCSetLoginParams);
            m_httpServer.AddXmlRPCHandler("region_startup", m_messagesService.RegionStartup);
            m_httpServer.AddXmlRPCHandler("region_shutdown", m_messagesService.RegionShutdown);
            m_httpServer.AddXmlRPCHandler("agent_location", m_messagesService.AgentLocation);
            m_httpServer.AddXmlRPCHandler("agent_leaving", m_messagesService.AgentLeaving);
            // Message Server ---> User Server
            m_httpServer.AddXmlRPCHandler("register_messageserver", m_messagesService.XmlRPCRegisterMessageServer);
            m_httpServer.AddXmlRPCHandler("agent_change_region", m_messagesService.XmlRPCUserMovedtoRegion);
            m_httpServer.AddXmlRPCHandler("deregister_messageserver", m_messagesService.XmlRPCDeRegisterMessageServer);

            m_httpServer.AddStreamHandler(new RestStreamHandler("GET", "/get_grid_info",
                                                                m_gridInfoService.RestGetGridInfoMethod));
            m_httpServer.AddXmlRPCHandler("get_grid_info", m_gridInfoService.XmlRpcGridInfoMethod);

            m_httpServer.AddStreamHandler(
                new RestStreamHandler("DELETE", "/usersessions/", m_userManager.RestDeleteUserSessionMethod));

            m_httpServer.AddXmlRPCHandler("update_user_profile", m_userManager.XmlRpcResponseXmlRPCUpdateUserProfile);

            // Handler for OpenID avatar identity pages
            m_httpServer.AddStreamHandler(new OpenIdStreamHandler("GET", "/users/", m_loginService));
            // Handlers for the OpenID endpoint server
            m_httpServer.AddStreamHandler(new OpenIdStreamHandler("POST", "/openid/server/", m_loginService));
            m_httpServer.AddStreamHandler(new OpenIdStreamHandler("GET", "/openid/server/", m_loginService));
        }

        public void do_create(string[] args)
        {
            switch (args[0])
            {
                case "user":
                    CreateUser(args);
                    break;
            }
        }
        
        /// <summary>
        /// Execute switch for some of the reset commands
        /// </summary>
        /// <param name="args"></param>
        protected void Reset(string[] args)
        {
            if (args.Length == 0)
                return;

            switch (args[0])
            {
                case "user":
                
                    switch (args[1])
                    {
                        case "password":
                            ResetUserPassword(args);
                            break;
                    }
                
                    break;
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="cmdparams">string array with parameters: firstname, lastname, password, locationX, locationY, email</param>
        protected void CreateUser(string[] cmdparams)
        {
            string firstName;
            string lastName;
            string password;
            string email;
            uint regX = 1000;
            uint regY = 1000;

            if (cmdparams.Length < 2)
                firstName = MainConsole.Instance.CmdPrompt("First name", "Default");
            else firstName = cmdparams[1];

            if (cmdparams.Length < 3)
                lastName = MainConsole.Instance.CmdPrompt("Last name", "User");
            else lastName = cmdparams[2];

            if (cmdparams.Length < 4)
                password = MainConsole.Instance.PasswdPrompt("Password");
            else password = cmdparams[3];

            if (cmdparams.Length < 5)
                regX = Convert.ToUInt32(MainConsole.Instance.CmdPrompt("Start Region X", regX.ToString()));
            else regX = Convert.ToUInt32(cmdparams[4]);

            if (cmdparams.Length < 6)
                regY = Convert.ToUInt32(MainConsole.Instance.CmdPrompt("Start Region Y", regY.ToString()));
            else regY = Convert.ToUInt32(cmdparams[5]);

            if (cmdparams.Length < 7)
                email = MainConsole.Instance.CmdPrompt("Email", "");
            else email = cmdparams[6];

            if (null == m_userManager.GetUserProfile(firstName, lastName))
            {
                m_lastCreatedUser = m_userManager.AddUser(firstName, lastName, password, email, regX, regY);
            }
            else
            {
                m_log.ErrorFormat("[USERS]: A user with the name {0} {1} already exists!", firstName, lastName);
            }
        }

        /// <summary>
        /// Reset a user password.
        /// </summary>
        /// <param name="cmdparams"></param>
        private void ResetUserPassword(string[] cmdparams)
        {
            string firstName;
            string lastName;
            string newPassword;
            
            if (cmdparams.Length < 3)
                firstName = MainConsole.Instance.CmdPrompt("First name");
            else firstName = cmdparams[2];

            if ( cmdparams.Length < 4 )
                lastName = MainConsole.Instance.CmdPrompt("Last name");
            else lastName = cmdparams[3];

            if ( cmdparams.Length < 5 )
                newPassword = MainConsole.Instance.PasswdPrompt("New password");
            else newPassword = cmdparams[4];
            
            m_userManager.ResetUserPassword(firstName, lastName, newPassword);
        }         

        private void HandleLoginCommand(string module, string[] cmd)
        {
            string subcommand = cmd[1];
            
            switch (subcommand)
            {
                case "level":
                    // Set the minimal level to allow login 
                    // Useful to allow grid update without worrying about users.
                    // or fixing critical issues
                    //
                    if (cmd.Length > 2)
                    {
                        int level = Convert.ToInt32(cmd[2]);
                        m_loginService.setloginlevel(level);
                    }
                    break;
                case "reset":
                     m_loginService.setloginlevel(0);
                    break;
                case "text":
                    if (cmd.Length > 2)
                    {
                        m_loginService.setwelcometext(cmd[2]);
                    }
                    break;
            }
        }

        public void RunCommand(string module, string[] cmd)
        {
            List<string> args = new List<string>(cmd);
            string command = cmd[0];

            args.RemoveAt(0);

            string[] cmdparams = args.ToArray();

            switch (command)
            {
                case "create":
                    do_create(cmdparams);
                    break;
                
                case "reset":
                    Reset(cmdparams);
                    break;


                case "test-inventory":
                    //  RestObjectPosterResponse<List<InventoryFolderBase>> requester = new RestObjectPosterResponse<List<InventoryFolderBase>>();
                    // requester.ReturnResponseVal = TestResponse;
                    // requester.BeginPostObject<UUID>(m_userManager._config.InventoryUrl + "RootFolders/", m_lastCreatedUser);
                    SynchronousRestObjectPoster.BeginPostObject<UUID, List<InventoryFolderBase>>(
                        "POST", Cfg.InventoryUrl + "RootFolders/", m_lastCreatedUser);
                    break;

                case "logoff-user":
                    if (cmdparams.Length >= 3)
                    {
                        string firstname = cmdparams[0];
                        string lastname = cmdparams[1];
                        string message = "";

                        for (int i = 2; i < cmdparams.Length; i++)
                            message += " " + cmdparams[i];

                        UserProfileData theUser = null;
                        try
                        {
                            theUser = m_loginService.GetTheUser(firstname, lastname);
                        }
                        catch (Exception)
                        {
                            m_log.Error("[LOGOFF]: Error getting user data from the database.");
                        }

                        if (theUser != null)
                        {
                            if (theUser.CurrentAgent != null)
                             {
                                if (theUser.CurrentAgent.AgentOnline)
                                {
                                    m_log.Info("[LOGOFF]: Logging off requested user!");
                                    m_loginService.LogOffUser(theUser, message);

                                    theUser.CurrentAgent.AgentOnline = false;

                                    m_loginService.CommitAgent(ref theUser);
                                }
                                else
                                {
                                    m_log.Info(
                                        "[LOGOFF]: User Doesn't appear to be online, sending the logoff message anyway.");
                                    m_loginService.LogOffUser(theUser, message);

                                    theUser.CurrentAgent.AgentOnline = false;

                                    m_loginService.CommitAgent(ref theUser);
                                }
                            }
                            else
                            {
                                m_log.Error(
                                    "[LOGOFF]: Unable to logoff-user.  User doesn't have an agent record so I can't find the simulator to notify");
                            }
                        }
                        else
                        {
                            m_log.Info("[LOGOFF]: User doesn't exist in the database");
                        }
                    }
                    else
                    {
                        m_log.Error(
                            "[LOGOFF]: Invalid amount of parameters.  logoff-user takes at least three.  Firstname, Lastname, and message");
                    }

                    break;
            }
        }
        
        protected override void ShowHelp(string[] helpArgs)
        {
            base.ShowHelp(helpArgs);  

            m_console.Notice("create user - create a new user");
            m_console.Notice("logoff-user <firstname> <lastname> <message> - logs off the specified user from the grid");
            m_console.Notice("reset user password - reset a user's password.");
            m_console.Notice("login-level <value> - Set the miminim userlevel allowed To login.");
            m_console.Notice("login-reset - reset the login level to its default value.");
            m_console.Notice("login-text <text to print during the login>");
            
        }

        public override void ShutdownSpecific()
        {
            m_loginService.OnUserLoggedInAtLocation -= NotifyMessageServersUserLoggedInToLocation;
        }

        public void TestResponse(List<InventoryFolderBase> resp)
        {
            m_console.Notice("response got");
        }

        public void NotifyMessageServersUserLoggOff(UUID agentID)
        {
            m_messagesService.TellMessageServersAboutUserLogoff(agentID);
        }

        public void NotifyMessageServersUserLoggedInToLocation(UUID agentID, UUID sessionID, UUID RegionID,
                                                               ulong regionhandle, float positionX, float positionY,
                                                               float positionZ, string firstname, string lastname)
        {
            m_messagesService.TellMessageServersAboutUser(agentID, sessionID, RegionID, regionhandle, positionX,
                                                          positionY, positionZ, firstname, lastname);
        }

        public void HandleAgentLocation(UUID agentID, UUID regionID, ulong regionHandle)
        {
            m_userManager.HandleAgentLocation(agentID, regionID, regionHandle);
        }

        public void HandleAgentLeaving(UUID agentID, UUID regionID, ulong regionHandle)
        {
            m_userManager.HandleAgentLeaving(agentID, regionID, regionHandle);
        }

        public void HandleRegionStartup(UUID regionID)
        {
            // This might seem strange, that we send this back to the
            // server it came from. But there is method to the madness.
            // There can be multiple user servers on the same database,
            // and each can have multiple messaging servers. So, we send
            // it to all known user servers, who send it to all known
            // message servers. That way, we should be able to finally
            // update presence to all regions and thereby all friends
            //
            m_userManager.HandleRegionStartup(regionID);
            m_messagesService.TellMessageServersAboutRegionShutdown(regionID);
        }

        public void HandleRegionShutdown(UUID regionID)
        {
            // This might seem strange, that we send this back to the
            // server it came from. But there is method to the madness.
            // There can be multiple user servers on the same database,
            // and each can have multiple messaging servers. So, we send
            // it to all known user servers, who send it to all known
            // message servers. That way, we should be able to finally
            // update presence to all regions and thereby all friends
            //
            m_userManager.HandleRegionShutdown(regionID);
            m_messagesService.TellMessageServersAboutRegionShutdown(regionID);
        }
    }
}
