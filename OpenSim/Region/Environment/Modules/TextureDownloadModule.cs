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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AS IS AND ANY
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
using System.Threading;
using libsecondlife;
using libsecondlife.Packets;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace OpenSim.Region.Environment.Modules
{
    //this is a first attempt, to start breaking the mess thats called the assetcache up.
    // basically this should be the texture sending (to clients) code moved out of assetcache 
    //and some small clean up
    // but on first tests it didn't seem to work very well so is currently not in use.
    public class TextureDownloadModule : IRegionModule
    {
        private Scene m_scene;
        private List<Scene> m_scenes = new List<Scene>();

        private readonly BlockingQueue<TextureSender> m_queueSenders = new BlockingQueue<TextureSender>();

        private readonly Dictionary<LLUUID, UserTextureDownloadService> m_userTextureServices =
            new Dictionary<LLUUID, UserTextureDownloadService>();

        private Thread m_thread;

        public TextureDownloadModule()
        {
        }

        public void Initialise(Scene scene, IConfigSource config)
        {
            if (m_scene == null)
            {
                //Console.WriteLine("Creating Texture download module");
                m_thread = new Thread(new ThreadStart(ProcessTextureSenders));
                m_thread.IsBackground = true;
                m_thread.Start();
            }

            if (!m_scenes.Contains(scene))
            {
                m_scenes.Add(scene);
                m_scene = scene;
                m_scene.EventManager.OnNewClient += NewClient;
                m_scene.EventManager.OnRemovePresence += EventManager_OnRemovePresence;
            }
        }

        private void EventManager_OnRemovePresence(LLUUID agentId)
        {
            UserTextureDownloadService textureService;

            lock (m_userTextureServices)
            {
                if( m_userTextureServices.TryGetValue( agentId, out textureService ))
                {
                    textureService.Close();

                    m_userTextureServices.Remove(agentId);
                }
            }
        }

        public void PostInitialise()
        {
        }

        public void Close()
        {
        }

        public string Name
        {
            get { return "TextureDownloadModule"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }

        public void NewClient(IClientAPI client)
        {
            client.OnRequestTexture += TextureRequest;
        }

        private bool TryGetUserTextureService(LLUUID userID, out UserTextureDownloadService textureService)
        {
            lock (m_userTextureServices)
            {
                if (m_userTextureServices.TryGetValue(userID, out textureService))
                {
                    return true;
                }

                textureService = new UserTextureDownloadService(m_scene, m_queueSenders);
                m_userTextureServices.Add(userID, textureService);
                return true;
            }
        }

        public void TextureRequest(Object sender, TextureRequestArgs e)
        {
            IClientAPI client = (IClientAPI) sender;
            UserTextureDownloadService textureService;
            if (TryGetUserTextureService(client.AgentId, out textureService))
            {
                textureService.HandleTextureRequest(client, e);
            }
        }

        public void ProcessTextureSenders()
        {
            while (true)
            {
                TextureSender sender = m_queueSenders.Dequeue();
                if (sender.Cancel)
                {
                    TextureSent(sender);

                    sender.Cancel = false;
                }
                else
                {
                    bool finished = sender.SendTexturePacket();
                    if (finished)
                    {
                        TextureSent(sender);
                    }
                    else
                    {
                        m_queueSenders.Enqueue(sender);
                    }
                }
            }
        }

        private void TextureSent(TextureSender sender)
        {
            sender.Sending = false;
        }
    }
}