using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using OpenSim.Framework;
using OpenSim.Framework.Types;
using OpenGrid.Framework.Communications;

using Nwc.XmlRpc;

namespace OpenGrid.Framework.Communications.OGS1
{
    public class OGS1GridServices : IGridServices
    {
        public RegionCommsListener listener;
        public GridInfo grid;

        public RegionCommsListener RegisterRegion(RegionInfo regionInfo, GridInfo gridInfo)
        {
            Hashtable GridParams = new Hashtable();

            grid = gridInfo;

            // Login / Authentication
            GridParams["authkey"] = gridInfo.GridServerSendKey;
            GridParams["UUID"] = regionInfo.SimUUID.ToStringHyphenated();
            GridParams["sim_ip"] = regionInfo.CommsExternalAddress;
            GridParams["sim_port"] = regionInfo.CommsIPListenPort.ToString();

            // Package into an XMLRPC Request
            ArrayList SendParams = new ArrayList(); 
            SendParams.Add(GridParams);

            // Send Request
            XmlRpcRequest GridReq = new XmlRpcRequest("simulator_login", SendParams);
            XmlRpcResponse GridResp = GridReq.Send(gridInfo.GridServerURI, 3000);
            Hashtable GridRespData = (Hashtable)GridResp.Value;
            Hashtable griddatahash = GridRespData;

            // Process Response
            if (GridRespData.ContainsKey("error"))
            {
                string errorstring = (string)GridRespData["error"];
                OpenSim.Framework.Console.MainLog.Instance.Error("Unable to connect to grid: " + errorstring);
                return null;
            }
            //this.neighbours = (ArrayList)GridRespData["neighbours"];

            listener = new RegionCommsListener();

            return listener;
        }

        public List<RegionInfo> RequestNeighbours(RegionInfo regionInfo)
        {
            Hashtable param = new Hashtable();
            param["xmin"] = regionInfo.RegionLocX - 1;
            param["ymin"] = regionInfo.RegionLocY - 1;
            param["xmax"] = regionInfo.RegionLocX + 1;
            param["ymax"] = regionInfo.RegionLocY + 1;
            IList parameters = new ArrayList();
            parameters.Add(param);
            XmlRpcRequest req = new XmlRpcRequest("map_block", parameters);
            XmlRpcResponse resp = req.Send(grid.GridServerURI, 3000);
            Hashtable respData = (Hashtable)resp.Value;

            List<RegionInfo> neighbours = new List<RegionInfo>();

            foreach (Hashtable n in (Hashtable)respData.Values)
            {
                RegionInfo neighbour = new RegionInfo();

                //OGS1
                neighbour.RegionHandle = (ulong)n["regionhandle"];
                neighbour.RegionLocX = (uint)n["x"];
                neighbour.RegionLocY = (uint)n["y"];
                neighbour.RegionName = (string)n["name"];

                //OGS1+
                neighbour.CommsIPListenAddr = (string)n["sim_ip"];
                neighbour.CommsIPListenPort = (int)n["sim_port"];
                neighbour.CommsExternalAddress = (string)n["sim_uri"];
                neighbour.SimUUID = (string)n["uuid"];

                neighbours.Add(neighbour);
            }

            return neighbours;
        }
        public RegionInfo RequestNeighbourInfo(ulong regionHandle)
        {
            OpenSim.Framework.Console.MainLog.Instance.Warn("Unimplemented - RequestNeighbourInfo()");
            return null;
        }
        public List<MapBlockData> RequestNeighbourMapBlocks(int minX, int minY, int maxX, int maxY)
        {
            Hashtable param = new Hashtable();
            param["xmin"] = minX;
            param["ymin"] = minY;
            param["xmax"] = maxX;
            param["ymax"] = maxY;
            IList parameters = new ArrayList();
            parameters.Add(param);
            XmlRpcRequest req = new XmlRpcRequest("map_block", parameters);
            XmlRpcResponse resp = req.Send(grid.GridServerURI, 3000);
            Hashtable respData = (Hashtable)resp.Value;

            List<MapBlockData> neighbours = new List<MapBlockData>();

            foreach (Hashtable n in (Hashtable)respData.Values)
            {
                MapBlockData neighbour = new MapBlockData();

                neighbour.X = (ushort)n["x"];
                neighbour.Y = (ushort)n["y"];

                neighbour.Name = (string)n["name"];
                neighbour.Access = (byte)n["access"];
                neighbour.RegionFlags = (uint)n["region-flags"];
                neighbour.WaterHeight = (byte)n["water-height"];
                neighbour.MapImageId = (string)n["map-image-id"];

                neighbours.Add(neighbour);
            }

            return neighbours;
        }
    }
}
