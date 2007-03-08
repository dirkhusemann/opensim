/*
Copyright (c) OpenSim project, http://osgrid.org/

* Copyright (c) <year>, <copyright holder>
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*     * Redistributions of source code must retain the above copyright
*       notice, this list of conditions and the following disclaimer.
*     * Redistributions in binary form must reproduce the above copyright
*       notice, this list of conditions and the following disclaimer in the
*       documentation and/or other materials provided with the distribution.
*     * Neither the name of the <organization> nor the
*       names of its contributors may be used to endorse or promote products
*       derived from this software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY <copyright holder> ``AS IS'' AND ANY
* EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
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
using libsecondlife;
using OpenSim.world;

namespace OpenSim
{
	/// <summary>
	/// This class handles connection to the underlying database used for configuration of the region.
	/// Region content is also stored by this class. The main entry point is InitConfig() which attempts to locate
	/// opensim.yap in the current working directory. If opensim.yap can not be found, default settings are loaded from
	/// what is hardcoded here and then saved into opensim.yap for future startups.
	/// </summary>
	
	
	public abstract class SimConfig
	{
		public string RegionName;
		
		public uint RegionLocX;
		public uint RegionLocY;
		public ulong RegionHandle;
		
		public int IPListenPort;
		public string IPListenAddr;
		
        public string AssetURL;
	    public string AssetSendKey;
		
	    public string GridURL;
	    public string GridSendKey;
		
	    public abstract void InitConfig();
	    public abstract void LoadFromGrid();
	    public abstract World LoadWorld();
	    public abstract void SaveMap();
		
	}
	
	public interface ISimConfig
	{
		SimConfig GetConfigObject();
	}
}
