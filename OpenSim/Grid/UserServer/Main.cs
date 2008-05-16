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
using System.Net;
using System.Reflection;
using libsecondlife;
using log4net;
using log4net.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Statistics;

namespace OpenSim.Grid.UserServer
{
    /// <summary>
    /// </summary>
    public class OpenUser_Main : BaseOpenSimServer, conscmd_callback
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private UserConfig Cfg;

        public UserManager m_userManager;
        public UserLoginService m_loginService;
        public MessageServersConnector m_messagesService;

        private LLUUID m_lastCreatedUser = LLUUID.Random();

        [STAThread]
        public static void Main(string[] args)
        {
            XmlConfigurator.Configure();

            m_log.Info("Launching UserServer...");

            OpenUser_Main userserver = new OpenUser_Main();

            userserver.Startup();
            userserver.Work();
        }

        private OpenUser_Main()
        {
            m_console = new ConsoleBase("User", this);
            MainConsole.Instance = m_console;
        }

        private void Work()
        {
            m_console.Notice("Enter help for a list of commands\n");

            while (true)
            {
                m_console.Prompt();
            }
        }

        public void Startup()
        {
            Cfg = new UserConfig("USER SERVER", (Path.Combine(Util.configDir(), "UserServer_Config.xml")));

            m_stats = StatsManager.StartCollectingUserStats();

            m_log.Info("[REGION]: Establishing data connection");
            m_userManager = new UserManager();
            m_userManager._config = Cfg;
            m_userManager.AddPlugin(Cfg.DatabaseProvider, Cfg.DatabaseConnect);

            m_loginService = new UserLoginService(
                 m_userManager, new LibraryRootFolder(), Cfg, Cfg.DefaultStartupMsg);

            m_messagesService = new MessageServersConnector();

            m_loginService.OnUserLoggedInAtLocation += NotifyMessageServersUserLoggedInToLocation;
            m_userManager.OnLogOffUser += NotifyMessageServersUserLoggOff;

            m_log.Info("[REGION]: Starting HTTP process");

            m_httpServer = new BaseHttpServer(Cfg.HttpPort);
            AddHttpHandlers();
            m_httpServer.Start();

            m_log.Info("[SERVER]: Userserver 0.5 - Startup complete");
        }

        protected void AddHttpHandlers()
        {
            m_httpServer.AddXmlRPCHandler("login_to_simulator", m_loginService.XmlRpcLoginMethod);

            m_httpServer.AddHTTPHandler("login", m_loginService.ProcessHTMLLogin);

            m_httpServer.SetLLSDHandler(m_loginService.LLSDLoginMethod);

            m_httpServer.AddXmlRPCHandler("get_user_by_name", m_userManager.XmlRPCGetUserMethodName);
            m_httpServer.AddXmlRPCHandler("get_user_by_uuid", m_userManager.XmlRPCGetUserMethodUUID);
            m_httpServer.AddXmlRPCHandler("get_avatar_picker_avatar", m_userManager.XmlRPCGetAvatarPickerAvatar);
            m_httpServer.AddXmlRPCHandler("add_new_user_friend", m_userManager.XmlRpcResponseXmlRPCAddUserFriend);
            m_httpServer.AddXmlRPCHandler("remove_user_friend", m_userManager.XmlRpcResponseXmlRPCRemoveUserFriend);
            m_httpServer.AddXmlRPCHandler("update_user_friend_perms", m_userManager.XmlRpcResponseXmlRPCUpdateUserFriendPerms);
            m_httpServer.AddXmlRPCHandler("get_user_friend_list", m_userManager.XmlRpcResponseXmlRPCGetUserFriendList);
            m_httpServer.AddXmlRPCHandler("get_avatar_appearance", m_userManager.XmlRPCGetAvatarAppearance);
            m_httpServer.AddXmlRPCHandler("update_avatar_appearance", m_userManager.XmlRPCUpdateAvatarAppearance);
            m_httpServer.AddXmlRPCHandler("logout_of_simulator", m_userManager.XmlRPCLogOffUserMethodUUID);

            // Message Server ---> User Server
            m_httpServer.AddXmlRPCHandler("register_messageserver", m_messagesService.XmlRPCRegisterMessageServer);
            m_httpServer.AddXmlRPCHandler("agent_change_region", m_messagesService.XmlRPCUserMovedtoRegion);
            m_httpServer.AddXmlRPCHandler("deregister_messageserver", m_messagesService.XmlRPCDeRegisterMessageServer);


            m_httpServer.AddStreamHandler(
                new RestStreamHandler("DELETE", "/usersessions/", m_userManager.RestDeleteUserSessionMethod));

            m_httpServer.AddXmlRPCHandler("update_user_profile", m_userManager.XmlRpcResponseXmlRPCUpdateUserProfile);
        }

        public void do_create(string what)
        {
            switch (what)
            {
                case "user":
                    string tempfirstname;
                    string templastname;
                    string tempMD5Passwd;
                    uint regX = 1000;
                    uint regY = 1000;

                    tempfirstname = m_console.CmdPrompt("First name");
                    templastname = m_console.CmdPrompt("Last name");
                    //tempMD5Passwd = m_console.PasswdPrompt("Password");
                    tempMD5Passwd = m_console.CmdPrompt("Password");
                    regX = Convert.ToUInt32(m_console.CmdPrompt("Start Region X"));
                    regY = Convert.ToUInt32(m_console.CmdPrompt("Start Region Y"));

                    if (null != m_userManager.GetUserProfile(tempfirstname, templastname))
                    {
                        m_log.ErrorFormat(
                            "[USERS]: A user with the name {0} {1} already exists!", tempfirstname, templastname);

                        break;
                    }

                    tempMD5Passwd = Util.Md5Hash(Util.Md5Hash(tempMD5Passwd) + ":" + String.Empty);

                    LLUUID userID = new LLUUID();
                    try
                    {
                        userID = m_userManager.AddUserProfile(tempfirstname, templastname, tempMD5Passwd, regX, regY);
                    }
                    catch (Exception ex)
                    {
                        m_log.ErrorFormat("[USERS]: Error creating user: {0}", ex.ToString());
                    }

                    try
                    {
                        bool created
                            = SynchronousRestObjectPoster.BeginPostObject<Guid, bool>(
                                "POST", m_userManager._config.InventoryUrl + "CreateInventory/", userID.UUID);

                        if (!created)
                        {
                            throw new Exception(
                                String.Format(
                                    "The inventory creation request for user {0} did not succeed."
                                        + "  Please contact your inventory service provider for more information.",
                                    userID));
                        }

                    }
                    catch (WebException)
                    {
                        m_log.ErrorFormat(
                            "[USERS]: Could not contact the inventory service at {0} to create an inventory for {1}",
                            m_userManager._config.InventoryUrl + "CreateInventory/", userID.UUID);
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[USERS]: Error creating inventory for user: {0}", e);
                    }

                    m_lastCreatedUser = userID;
                    break;
            }
        }

        public override void RunCmd(string cmd, string[] cmdparams)
        {
            base.RunCmd(cmd, cmdparams);

            switch (cmd)
            {
                case "help":
                    m_console.Notice("create user - create a new user");
                    break;

                case "create":
                    do_create(cmdparams[0]);
                    break;

                case "test-inventory":
                    //  RestObjectPosterResponse<List<InventoryFolderBase>> requester = new RestObjectPosterResponse<List<InventoryFolderBase>>();
                    // requester.ReturnResponseVal = TestResponse;
                    // requester.BeginPostObject<LLUUID>(m_userManager._config.InventoryUrl + "RootFolders/", m_lastCreatedUser);
                    SynchronousRestObjectPoster.BeginPostObject<LLUUID, List<InventoryFolderBase>>("POST",
                                                                                                   m_userManager.
                                                                                                       _config.
                                                                                                       InventoryUrl +
                                                                                                   "RootFolders/",
                                                                                                   m_lastCreatedUser);
                    break;
            }
        }

        public override void Shutdown()
        {
            m_loginService.OnUserLoggedInAtLocation -= NotifyMessageServersUserLoggedInToLocation;

            base.Shutdown();
        }

        public void TestResponse(List<InventoryFolderBase> resp)
        {
            m_console.Notice("response got");
        }

        public void NotifyMessageServersUserLoggOff(LLUUID agentID)
        {
            m_messagesService.TellMessageServersAboutUserLogoff(agentID);
        }

        public void NotifyMessageServersUserLoggedInToLocation(LLUUID agentID, LLUUID sessionID, LLUUID RegionID,
                                                                ulong regionhandle, float positionX, float positionY,
                                                                float positionZ, string firstname, string lastname)
        {

            m_messagesService.TellMessageServersAboutUser(agentID, sessionID, RegionID, regionhandle, positionX,
                                                          positionY,  positionZ, firstname, lastname);
        }
    }
}
