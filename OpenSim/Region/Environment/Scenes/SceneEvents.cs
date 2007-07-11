using libsecondlife;

namespace OpenSim.Region.Environment.Scenes
{
    /// <summary>
    /// A class for triggering remote scene events.
    /// </summary>
    public class EventManager
    {
        public delegate void OnFrameDelegate();
        public event OnFrameDelegate OnFrame;

        public delegate void OnNewPresenceDelegate(ScenePresence presence);
        public event OnNewPresenceDelegate OnNewPresence;

        public delegate void OnNewPrimitiveDelegate(PrimitiveOld prim);
        public event OnNewPrimitiveDelegate OnNewPrimitive;

        public delegate void OnRemovePresenceDelegate(LLUUID uuid);
        public event OnRemovePresenceDelegate OnRemovePresence;

        public void TriggerOnFrame()
        {
            if (OnFrame != null)
            {
                OnFrame();
            }
        }

        public void TriggerOnNewPrimitive(PrimitiveOld prim)
        {
            if (OnNewPrimitive != null)
                OnNewPrimitive(prim);
        }

        public void TriggerOnNewPresence(ScenePresence presence)
        {
            if (OnNewPresence != null)
                OnNewPresence(presence);
        }

        public void TriggerOnRemovePresence(LLUUID uuid)
        {
            if (OnRemovePresence != null)
            {
                OnRemovePresence(uuid);
            }
        }
    }
}
