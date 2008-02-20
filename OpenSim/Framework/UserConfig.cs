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
using OpenSim.Framework.Console;

namespace OpenSim.Framework
{
    /// <summary>
    /// UserConfig -- For User Server Configuration
    /// </summary>
    public class UserConfig
    {
        public string DefaultStartupMsg = String.Empty;
        public string GridServerURL = String.Empty;
        public string GridSendKey = String.Empty;
        public string GridRecvKey = String.Empty;

        public string InventoryUrl = String.Empty;

        public string DatabaseProvider = String.Empty;

        public static uint DefaultHttpPort = 8002;
        public static bool DefaultHttpSSL = false;
        public uint HttpPort = DefaultHttpPort;
        public bool HttpSSL = DefaultHttpSSL;
        public uint DefaultX = 1000;
        public uint DefaultY = 1000;

        private ConfigurationMember configMember;

        public UserConfig(string description, string filename)
        {
            configMember =
                new ConfigurationMember(filename, description, loadConfigurationOptions, handleIncomingConfiguration,true);
            configMember.performConfigurationRetrieve();
        }

        public void loadConfigurationOptions()
        {
            configMember.addConfigurationOption("default_startup_message",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING_NOT_EMPTY,
                                                "Default Startup Message", "Welcome to OGS", false);

            configMember.addConfigurationOption("default_grid_server",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING_NOT_EMPTY,
                                                "Default Grid Server URI",
                                                "http://127.0.0.1:" + GridConfig.DefaultHttpPort.ToString() + "/", false);
            configMember.addConfigurationOption("grid_send_key", ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Key to send to grid server", "null", false);
            configMember.addConfigurationOption("grid_recv_key", ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "Key to expect from grid server", "null", false);

            configMember.addConfigurationOption("default_inventory_server",
                                                ConfigurationOption.ConfigurationTypes.TYPE_STRING_NOT_EMPTY,
                                                "Default Inventory Server URI",
                                                "http://127.0.0.1:" + InventoryConfig.DefaultHttpPort.ToString() + "/",
                                                false);
            configMember.addConfigurationOption("database_provider", ConfigurationOption.ConfigurationTypes.TYPE_STRING,
                                                "DLL for database provider", "OpenSim.Framework.Data.MySQL.dll", false);

            configMember.addConfigurationOption("http_port", ConfigurationOption.ConfigurationTypes.TYPE_UINT32,
                                                "Http Listener port", DefaultHttpPort.ToString(), false);
            configMember.addConfigurationOption("http_ssl", ConfigurationOption.ConfigurationTypes.TYPE_BOOLEAN,
                                                "Use SSL? true/false", DefaultHttpSSL.ToString(), false);
            configMember.addConfigurationOption("default_X", ConfigurationOption.ConfigurationTypes.TYPE_UINT32,
                                                "Known good region X", "1000", false);
            configMember.addConfigurationOption("default_Y", ConfigurationOption.ConfigurationTypes.TYPE_UINT32,
                                                "Known good region Y", "1000", false);
        }

        public bool handleIncomingConfiguration(string configuration_key, object configuration_result)
        {
            switch (configuration_key)
            {
                case "default_startup_message":
                    DefaultStartupMsg = (string) configuration_result;
                    break;
                case "default_grid_server":
                    GridServerURL = (string) configuration_result;
                    break;
                case "grid_send_key":
                    GridSendKey = (string) configuration_result;
                    break;
                case "grid_recv_key":
                    GridRecvKey = (string) configuration_result;
                    break;
                case "default_inventory_server":
                    InventoryUrl = (string) configuration_result;
                    break;
                case "database_provider":
                    DatabaseProvider = (string) configuration_result;
                    break;
                case "http_port":
                    HttpPort = (uint) configuration_result;
                    break;
                case "http_ssl":
                    HttpSSL = (bool) configuration_result;
                    break;
                case "default_X":
                    DefaultX = (uint) configuration_result;
                    break;
                case "default_Y":
                    DefaultY = (uint) configuration_result;
                    break;
            }

            return true;
        }
    }
}