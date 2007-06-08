using Nwc.XmlRpc;
using System;
using System.Net;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using libsecondlife;

namespace OpenGridServices.Manager
{
	public class GridServerConnectionManager
	{
		private string ServerURL;
		public LLUUID SessionID;
		public bool connected=false;
		
		public RegionBlock[][] WorldMap;
		
		public bool Connect(string GridServerURL, string username, string password)
		{
			try {
				this.ServerURL=GridServerURL;
				Hashtable LoginParamsHT = new Hashtable();
				LoginParamsHT["username"]=username;
				LoginParamsHT["password"]=password;
				ArrayList LoginParams = new ArrayList();
				LoginParams.Add(LoginParamsHT);
				XmlRpcRequest GridLoginReq = new XmlRpcRequest("manager_login",LoginParams);
				XmlRpcResponse GridResp = GridLoginReq.Send(ServerURL,3000);
				if(GridResp.IsFault) {
					connected=false;
					return false;
				} else {
					Hashtable gridrespData = (Hashtable)GridResp.Value;
					this.SessionID = new LLUUID((string)gridrespData["session_id"]);
					connected=true;
					return true;
				}
			} catch(Exception e) {
				Console.WriteLine(e.ToString());
				connected=false;
				return false;
			}
		}
		
		public void DownloadMap()
		{
			System.Net.WebClient mapdownloader = new WebClient();
			Stream regionliststream = mapdownloader.OpenRead(ServerURL + "/regionlist");
			
			RegionBlock TempRegionData;
			
			XmlDocument doc = new XmlDocument();
            doc.Load(regionliststream);
            regionliststream.Close();
            XmlNode rootnode = doc.FirstChild;
            if (rootnode.Name != "regions")
            {
				// TODO - ERROR!
            }

            for(int i=0; i<=rootnode.ChildNodes.Count; i++)
            {
                if(rootnode.ChildNodes.Item(i).Name != "region") {
                	// TODO - ERROR!
                } else {
					TempRegionData = new RegionBlock();

					
                }
			}
		}
		
		public bool RestartServer()
		{
			return true;
		}
		
		public bool ShutdownServer()
		{
			try {
				Hashtable ShutdownParamsHT = new Hashtable();
				ArrayList ShutdownParams = new ArrayList();
				ShutdownParamsHT["session_id"]=this.SessionID.ToString();
				ShutdownParams.Add(ShutdownParamsHT);
				XmlRpcRequest GridShutdownReq = new XmlRpcRequest("shutdown",ShutdownParams);
				XmlRpcResponse GridResp = GridShutdownReq.Send(this.ServerURL,3000);
				if(GridResp.IsFault) {
					return false;
				} else {
					connected=false;
					return true;
				}
			} catch(Exception e) {
				Console.WriteLine(e.ToString());
				return false;
			}
		}
	
		public void DisconnectServer()
		{
			this.connected=false;
		}
	}
}
