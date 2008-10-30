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
using System.Net;
using System.Net.Sockets;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;

namespace OpenSim.Region.ClientStack.LindenUDP.Tests
{
    /// <summary>
    /// This class enables synchronous testing of the LLUDPServer by allowing us to load our own data into the end 
    /// receive event
    /// </summary>
    public class TestLLUDPServer : LLUDPServer
    {
        /// <summary>
        /// The chunks of data to pass to the LLUDPServer when it calls EndReceive
        /// </summary>
        protected Queue<ChunkSenderTuple> m_chunksToLoad = new Queue<ChunkSenderTuple>();
        
        protected override void BeginReceive()
        {
            // Do nothing
        }
        
        protected override bool EndReceive(out int numBytes, IAsyncResult result, ref EndPoint epSender)
        {
            numBytes = 0;
            
            if (m_chunksToLoad.Count <= 0)
                return false;
            
            ChunkSenderTuple tuple = m_chunksToLoad.Dequeue();
            RecvBuffer = tuple.Data;
            numBytes   = tuple.Data.Length;
            epSender   = tuple.Sender;
            
            return true;
        }
        
        public override void SendPacketTo(byte[] buffer, int size, SocketFlags flags, uint circuitcode) 
        {
            // Don't do anything just yet
        }
        
        /// <summary>
        /// Load some data to be received by the LLUDPServer on the next receive call
        /// </summary>
        /// <param name="data"></param>
        /// <param name="epSender"></param>
        public void LoadReceive(byte[] data, EndPoint epSender)
        {
            m_chunksToLoad.Enqueue(new ChunkSenderTuple(data, epSender));
        }
        
        /// <summary>
        /// Load a packet to be received by the LLUDPServer on the next receive call
        /// </summary>
        /// <param name="packet"></param>
        public void LoadReceive(Packet packet, EndPoint epSender)
        {
            LoadReceive(packet.ToBytes(), epSender);
        }
        
        /// <summary>
        /// Calls the protected asynchronous result method
        /// </summary>
        /// <param name="result"></param>
        public void ReceiveData(IAsyncResult result)
        {
            OnReceivedData(result);
        }                
        
        /// <summary>
        /// Has a circuit with the given code been established?
        /// </summary>
        /// <param name="circuitCode"></param>
        /// <returns></returns>
        public bool HasCircuit(uint circuitCode)
        {
            lock (clientCircuits_reverse)
            {
                return clientCircuits_reverse.ContainsKey(circuitCode);
            }
        }
    }
    
    /// <summary>
    /// Record the data and sender tuple
    /// </summary>
    public class ChunkSenderTuple
    {
        public byte[] Data;
        public EndPoint Sender;
        
        public ChunkSenderTuple(byte[] data, EndPoint sender)
        {
            Data = data;
            Sender = sender;
        }
    }
}