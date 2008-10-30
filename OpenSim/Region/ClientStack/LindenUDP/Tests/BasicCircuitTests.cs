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
using log4net;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace OpenSim.Region.ClientStack.LindenUDP.Tests
{
    /// <summary>
    /// This will contain basic tests for the LindenUDP client stack
    /// </summary>
    [TestFixture]
    public class BasicCircuitTests
    {
        [SetUp]
        public void Init()
        {
            try
            {
                log4net.Config.XmlConfigurator.Configure();
            }
            catch
            {
                // I don't care, just leave log4net off
            }            
        }
        
        /// <summary>
        /// Add a client for testing
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="testLLUDPServer"></param>
        /// <param name="testPacketServer"></param>
        /// <param name="acm">Agent circuit manager used in setting up the stack</param>        
        protected void SetupStack(
            IScene scene, out TestLLUDPServer testLLUDPServer, out TestLLPacketServer testPacketServer, 
            out AgentCircuitManager acm)
        {
            ClientStackUserSettings userSettings = new ClientStackUserSettings();
            testLLUDPServer = new TestLLUDPServer();             
            acm = new AgentCircuitManager();
                                    
            uint port = 666;            
            testLLUDPServer.Initialise(null, ref port, 0, false, userSettings, null, acm);
            testPacketServer = new TestLLPacketServer(testLLUDPServer, userSettings);
            testLLUDPServer.LocalScene = scene;            
        }
        
        /// <summary>
        /// Set up a client for tests which aren't concerned with this process itself
        /// </summary>
        /// <param name="circuitCode"></param>
        /// <param name="epSender"></param>
        /// <param name="testLLUDPServer"></param>
        /// <param name="acm"></param>
        protected void AddClient(
            uint circuitCode, EndPoint epSender, TestLLUDPServer testLLUDPServer, AgentCircuitManager acm)
        {
            UUID myAgentUuid   = UUID.Parse("00000000-0000-0000-0000-000000000001");
            UUID mySessionUuid = UUID.Parse("00000000-0000-0000-0000-000000000002");

            AgentCircuitData acd = new AgentCircuitData();
            acd.AgentID = myAgentUuid;
            acd.SessionID = mySessionUuid; 
            
            UseCircuitCodePacket uccp = new UseCircuitCodePacket();
            
            UseCircuitCodePacket.CircuitCodeBlock uccpCcBlock 
                = new OpenMetaverse.Packets.UseCircuitCodePacket.CircuitCodeBlock();
            uccpCcBlock.Code = circuitCode;
            uccpCcBlock.ID = myAgentUuid;
            uccpCcBlock.SessionID = mySessionUuid;
            uccp.CircuitCode = uccpCcBlock;

            acm.AddNewCircuit(circuitCode, acd);
            
            testLLUDPServer.LoadReceive(uccp, epSender);            
            testLLUDPServer.ReceiveData(null);
        }
        
        /// <summary>
        /// Test adding a client to the stack
        /// </summary>
        [Test]        
        public void TestAddClient()
        {            
            uint myCircuitCode = 123456;
            UUID myAgentUuid   = UUID.Parse("00000000-0000-0000-0000-000000000001");
            UUID mySessionUuid = UUID.Parse("00000000-0000-0000-0000-000000000002");
            
            TestLLUDPServer testLLUDPServer;
            TestLLPacketServer testLLPacketServer;
            AgentCircuitManager acm;
            SetupStack(new MockScene(), out testLLUDPServer, out testLLPacketServer, out acm);
            
            AgentCircuitData acd = new AgentCircuitData();
            acd.AgentID = myAgentUuid;
            acd.SessionID = mySessionUuid;
            
            UseCircuitCodePacket uccp = new UseCircuitCodePacket();
            
            UseCircuitCodePacket.CircuitCodeBlock uccpCcBlock 
                = new OpenMetaverse.Packets.UseCircuitCodePacket.CircuitCodeBlock();
            uccpCcBlock.Code = myCircuitCode;
            uccpCcBlock.ID = myAgentUuid;
            uccpCcBlock.SessionID = mySessionUuid;
            uccp.CircuitCode = uccpCcBlock;
            
            EndPoint testEp = new IPEndPoint(IPAddress.Loopback, 999);
            
            testLLUDPServer.LoadReceive(uccp, testEp);            
            testLLUDPServer.ReceiveData(null);
            
            // Circuit shouildn't exist since the circuit manager doesn't know about this circuit for authentication yet
            Assert.IsFalse(testLLUDPServer.HasCircuit(myCircuitCode));
                        
            acm.AddNewCircuit(myCircuitCode, acd);
            
            testLLUDPServer.LoadReceive(uccp, testEp);            
            testLLUDPServer.ReceiveData(null);            
            
            // Should succeed now
            Assert.IsTrue(testLLUDPServer.HasCircuit(myCircuitCode));
            Assert.IsFalse(testLLUDPServer.HasCircuit(101));
        }

        /// <summary>
        /// Test removing a client from the stack
        /// </summary>
        [Test]        
        public void TestRemoveClient()
        {
            uint myCircuitCode = 123457;
            
            TestLLUDPServer testLLUDPServer;
            TestLLPacketServer testLLPacketServer;
            AgentCircuitManager acm;
            SetupStack(new MockScene(), out testLLUDPServer, out testLLPacketServer, out acm);            
            AddClient(myCircuitCode, new IPEndPoint(IPAddress.Loopback, 1000), testLLUDPServer, acm);
            
            testLLUDPServer.RemoveClientCircuit(myCircuitCode);            
            Assert.IsFalse(testLLUDPServer.HasCircuit(myCircuitCode));
            
            // Check that removing a non-existant circuit doesn't have any bad effects
            testLLUDPServer.RemoveClientCircuit(101);            
            Assert.IsFalse(testLLUDPServer.HasCircuit(101));
        }
        
        /// <summary>
        /// Make sure that the client stack reacts okay to malformed packets
        /// </summary>
        [Test]
        public void TestMalformedPacketSend()
        {
            uint myCircuitCode = 123458;
            EndPoint testEp = new IPEndPoint(IPAddress.Loopback, 1001);
            MockScene scene = new MockScene(); 
            
            TestLLUDPServer testLLUDPServer;
            TestLLPacketServer testLLPacketServer;
            AgentCircuitManager acm;
            SetupStack(scene, out testLLUDPServer, out testLLPacketServer, out acm);            
            AddClient(myCircuitCode, testEp, testLLUDPServer, acm);  

            byte[] data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            // Send two garbled 'packets' in succession
            testLLUDPServer.LoadReceive(data, testEp);
            testLLUDPServer.LoadReceive(data, testEp);
            testLLUDPServer.ReceiveData(null); 
            
            // Check that we are still here
            Assert.IsTrue(testLLUDPServer.HasCircuit(myCircuitCode));
            Assert.That(testLLPacketServer.GetTotalPacketsReceived(), Is.EqualTo(0));
            
            // Check that sending a valid packet to same circuit still succeeds
            Assert.That(scene.ObjectNameCallsReceived, Is.EqualTo(0));

            ObjectNamePacket onp = new ObjectNamePacket();
            ObjectNamePacket.ObjectDataBlock odb = new ObjectNamePacket.ObjectDataBlock();
            odb.LocalID = 1;
            odb.Name = Utils.StringToBytes("helloooo");
            onp.ObjectData = new ObjectNamePacket.ObjectDataBlock[] { odb };
            onp.Header.Zerocoded = false;
            
            testLLUDPServer.LoadReceive(onp, testEp);
            testLLUDPServer.ReceiveData(null);
            
            Assert.That(testLLPacketServer.GetTotalPacketsReceived(), Is.EqualTo(1));  
            Assert.That(testLLPacketServer.GetPacketsReceivedFor(PacketType.ObjectName), Is.EqualTo(1));
        }
    }
}
