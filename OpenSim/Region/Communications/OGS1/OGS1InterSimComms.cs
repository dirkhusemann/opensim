using System;
using libsecondlife;
using OpenSim.Framework.Types;

namespace OpenSim.Region.Communications.OGS1
{
    public delegate bool InformRegionChild(ulong regionHandle, AgentCircuitData agentData);
    public delegate bool ExpectArrival(ulong regionHandle, LLUUID agentID, LLVector3 position);

    public sealed class InterRegionSingleton
    {
        static readonly InterRegionSingleton instance = new InterRegionSingleton();

        public event InformRegionChild OnChildAgent;
        public event ExpectArrival OnArrival;

        static InterRegionSingleton()
        {
        }

        InterRegionSingleton()
        {
        }

        public static InterRegionSingleton Instance
        {
            get
            {
                return instance;
            }
        }

        public bool InformRegionOfChildAgent(ulong regionHandle, AgentCircuitData agentData)
        {
            if (OnChildAgent != null)
            {
                return OnChildAgent(regionHandle, agentData);
            }
            return false;
        }

        public bool ExpectAvatarCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position)
        {
            if (OnArrival != null)
            {
                return OnArrival(regionHandle, agentID, position);
            }
            return false;
        }
    }

    public class OGS1InterRegionRemoting : MarshalByRefObject
    {

        public OGS1InterRegionRemoting()
        {
        }

        public bool InformRegionOfChildAgent(ulong regionHandle, AgentCircuitData agentData)
        {
            return InterRegionSingleton.Instance.InformRegionOfChildAgent(regionHandle, agentData);
        }

        public bool ExpectAvatarCrossing(ulong regionHandle, LLUUID agentID, LLVector3 position)
        {
            return InterRegionSingleton.Instance.ExpectAvatarCrossing(regionHandle, agentID, position);
        }
    }
}
