/*
* Copyright (c) Contributors, http://www.openmetaverse.org/
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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS AND ANY
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
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using libsecondlife;
using libsecondlife.Packets;
using OpenSim.Framework.Console;


namespace OpenSim.Servers
{
    public class CheckSumServer : UDPServerBase
    {
        //protected ConsoleBase m_console;

        public CheckSumServer(int port)
            : base(port)
        {
        }

        protected override void OnReceivedData(IAsyncResult result)
        {
            ipeSender = new IPEndPoint(IPAddress.Any, 0);
            epSender = (EndPoint)ipeSender;
            Packet packet = null;
            int numBytes = Server.EndReceiveFrom(result, ref epSender);
            int packetEnd = numBytes - 1;

            packet = Packet.BuildPacket(RecvBuffer, ref packetEnd, ZeroBuffer);

            if (packet.Type == PacketType.SecuredTemplateChecksumRequest)
            {
                SecuredTemplateChecksumRequestPacket checksum = (SecuredTemplateChecksumRequestPacket)packet;
                TemplateChecksumReplyPacket checkreply = new TemplateChecksumReplyPacket();
                checkreply.DataBlock.Checksum = 3220703154;//180572585;
                checkreply.DataBlock.Flags = 0;
                checkreply.DataBlock.MajorVersion = 1;
                checkreply.DataBlock.MinorVersion = 15;
                checkreply.DataBlock.PatchVersion = 0;
                checkreply.DataBlock.ServerVersion = 0;
                checkreply.TokenBlock.Token = checksum.TokenBlock.Token;
                this.SendPacket(checkreply, epSender);

                /*
                //if we wanted to echo the the checksum/ version from the client (so that any client worked)
                SecuredTemplateChecksumRequestPacket checkrequest = new SecuredTemplateChecksumRequestPacket();
                checkrequest.TokenBlock.Token = checksum.TokenBlock.Token;
                this.SendPacket(checkrequest, epSender);
                */
            }
            else if (packet.Type == PacketType.TemplateChecksumReply)
            {
                //echo back the client checksum reply (Hegemon's method)
                TemplateChecksumReplyPacket checksum2 = (TemplateChecksumReplyPacket)packet;
                TemplateChecksumReplyPacket checkreply2 = new TemplateChecksumReplyPacket();
                checkreply2.DataBlock.Checksum = checksum2.DataBlock.Checksum;
                checkreply2.DataBlock.Flags = checksum2.DataBlock.Flags;
                checkreply2.DataBlock.MajorVersion = checksum2.DataBlock.MajorVersion;
                checkreply2.DataBlock.MinorVersion = checksum2.DataBlock.MinorVersion;
                checkreply2.DataBlock.PatchVersion = checksum2.DataBlock.PatchVersion;
                checkreply2.DataBlock.ServerVersion = checksum2.DataBlock.ServerVersion;
                checkreply2.TokenBlock.Token = checksum2.TokenBlock.Token;
                this.SendPacket(checkreply2, epSender);
            }
            else
            {
            }

            Server.BeginReceiveFrom(RecvBuffer, 0, RecvBuffer.Length, SocketFlags.None, ref epSender, ReceivedData, null);
        }

        private void SendPacket(Packet Pack, EndPoint endp)
        {
            if (!Pack.Header.Resent)
            {
                Pack.Header.Sequence = 1;
            }

            byte[] ZeroOutBuffer = new byte[4096];
            byte[] sendbuffer;
            sendbuffer = Pack.ToBytes();

            try
            {
                if (Pack.Header.Zerocoded)
                {
                    int packetsize = Helpers.ZeroEncode(sendbuffer, sendbuffer.Length, ZeroOutBuffer);
                    this.SendPackTo(ZeroOutBuffer, packetsize, SocketFlags.None, endp);
                }
                else
                {
                    this.SendPackTo(sendbuffer, sendbuffer.Length, SocketFlags.None, endp);
                }
            }
            catch (Exception)
            {
                OpenSim.Framework.Console.MainConsole.Instance.WriteLine(OpenSim.Framework.Console.LogPriority.MEDIUM, "OpenSimClient.cs:ProcessOutPacket() - WARNING: Socket exception occurred on connection ");

            }
        }

        private void SendPackTo(byte[] buffer, int size, SocketFlags flags, EndPoint endp)
        {
            this.Server.SendTo(buffer, size, flags, endp);
        }
    }
}