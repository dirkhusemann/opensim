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

using System.Net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Servers;
using OpenSim.Region.Physics.Manager;
using OpenSim.Region.Environment;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Tests.Common.Mock;

namespace OpenSim.Region.Environment.Scenes.Tests
{        
    /// <summary>
    /// Utilities for constructing and performing operations upon scenes.
    /// </summary>
    public class SceneTestUtils
    {        
        /// <summary>
        /// Set up a test scene
        /// </summary>
        /// <returns></returns>
        public static TestScene SetupScene()
        {
            return SetupScene("Unit test region", UUID.Random(), 1000, 1000, new TestCommunicationsManager());
        }
        
        /// <summary>
        /// Set up a test scene
        /// </summary>
        /// <param name="name">Name of the region</param>
        /// <param name="id">ID of the region</param>
        /// <param name="x">X co-ordinate of the region</param>
        /// <param name="y">Y co-ordinate of the region</param>
        /// <param name="cm">This should be the same if simulating two scenes within a standalone</param>
        /// <returns></returns>
        public static TestScene SetupScene(string name, UUID id, uint x, uint y, CommunicationsManager cm)
        {
            RegionInfo regInfo = new RegionInfo(x, y, new IPEndPoint(IPAddress.Loopback, 9000), "127.0.0.1");
            regInfo.RegionName = name;
            regInfo.RegionID = id;
            
            AgentCircuitManager acm = new AgentCircuitManager();
            SceneCommunicationService scs = new SceneCommunicationService(cm);
            
            SQLAssetServer assetService = new SQLAssetServer(new TestAssetDataPlugin());
            AssetCache ac = new AssetCache(assetService);
            
            StorageManager sm = new StorageManager("OpenSim.Data.Null.dll", "", "");            
            IConfigSource configSource = new IniConfigSource();
            
            TestScene testScene = new TestScene(
                regInfo, acm, cm, scs, ac, sm, null, false, false, false, configSource, null);
            
            testScene.LandChannel = new TestLandChannel();
            testScene.LoadWorldMap();
            
            PhysicsPluginManager physicsPluginManager = new PhysicsPluginManager();
            physicsPluginManager.LoadPluginsFromAssembly("Physics/OpenSim.Region.Physics.BasicPhysicsPlugin.dll");
            testScene.PhysicsScene 
                = physicsPluginManager.GetPhysicsScene("basicphysics", "ZeroMesher", configSource, "test");            
                        
            return testScene;
        }
        
        /// <summary>
        /// Add a root agent
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="agentId"></param>
        /// <returns></returns>        
        public static TestClient AddRootAgent(Scene scene, UUID agentId)
        {
            string firstName = "testfirstname";
            
            AgentCircuitData agent = new AgentCircuitData();
            agent.AgentID = agentId;
            agent.firstname = firstName;
            agent.lastname = "testlastname";
            agent.SessionID = UUID.Zero;
            agent.SecureSessionID = UUID.Zero;
            agent.circuitcode = 123;
            agent.BaseFolder = UUID.Zero;
            agent.InventoryFolder = UUID.Zero;
            agent.startpos = Vector3.Zero;
            agent.CapsPath = "http://wibble.com";
            
            // We emulate the proper login sequence here by doing things in three stages            
            // Stage 1: simulate login by telling the scene to expect a new user connection
            scene.NewUserConnection(agent);
            
            // Stage 2: add the new client as a child agent to the scene
            TestClient client = new TestClient(agent, scene);
            scene.AddNewClient(client);
            
            // Stage 3: Invoke agent crossing, which converts the child agent into a root agent (with appearance,
            // inventory, etc.)
            scene.AgentCrossing(agent.AgentID, new Vector3(90, 90, 90), false);
            
            return client;
        }

        /// <summary>
        /// Add a test object
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>        
        public static SceneObjectPart AddSceneObject(Scene scene)
        {
            return AddSceneObject(scene, null);
        }
        
        /// <summary>
        /// Add a test object
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static SceneObjectPart AddSceneObject(Scene scene, string name)
        {
            SceneObjectGroup sceneObject = new SceneObjectGroup();
            SceneObjectPart part 
                = new SceneObjectPart(UUID.Zero, PrimitiveBaseShape.Default, Vector3.Zero, Quaternion.Identity, Vector3.Zero);
            
            if (name != null)
                part.Name = name;
            
            //part.UpdatePrimFlags(false, false, true);           
            part.ObjectFlags |= (uint)PrimFlags.Phantom;            
            sceneObject.SetRootPart(part);
            
            scene.AddNewSceneObject(sceneObject, false);
            
            return part;
        }
        
        /// <summary>
        /// Delete a scene object asynchronously
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="part"></param>
        /// <param name="action"></param>
        /// <param name="destinationId"></param>
        /// <param name="client"></param>
        public static void DeleteSceneObjectAsync(
            TestScene scene, SceneObjectPart part, DeRezAction action, UUID destinationId, IClientAPI client)
        {
            // Turn off the timer on the async sog deleter - we'll crank it by hand within a unit test
            AsyncSceneObjectGroupDeleter sogd = scene.SceneObjectGroupDeleter;
            sogd.Enabled = false;

            scene.DeRezObject(client, part.LocalId, UUID.Zero, action, destinationId);
            sogd.InventoryDeQueueAndDelete();             
        }
    }
}
