using System.Collections.Generic;
using Axiom.Math;
using libsecondlife;

namespace OpenSim.Region.Environment.Scenes
{
    public abstract class EntityBase
    {
        public LLUUID m_uuid;

        protected List<EntityBase> m_children;


        protected Scene m_scene;
        protected string m_name;

        /// <summary>
        /// 
        /// </summary>
        public virtual string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        protected LLVector3 m_pos;

        /// <summary>
        /// 
        /// </summary>
        public virtual LLVector3 Pos
        {
            get { return m_pos; }
            set { m_pos = value; }
        }

        public LLVector3 m_velocity;

        /// <summary>
        /// 
        /// </summary>
        public virtual LLVector3 Velocity
        {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        protected Quaternion m_rotation = new Quaternion(0, 0, 1, 0);

        public virtual Quaternion Rotation
        {
            get { return m_rotation; }
            set { m_rotation = value; }
        }

        protected uint m_localId;

        public uint LocalId
        {
            get { return m_localId; }
            set { m_localId = value; }
        }

        /// <summary>
        /// Creates a new Entity (should not occur on it's own)
        /// </summary>
        public EntityBase()
        {
            m_uuid = new LLUUID();

            m_pos = new LLVector3();
            m_velocity = new LLVector3();
            Rotation = new Quaternion();
            m_name = "(basic entity)";

            m_children = new List<EntityBase>();
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void UpdateMovement()
        {
            foreach (EntityBase child in m_children)

            {
                child.UpdateMovement();
            }
        }

        /// <summary>
        /// Performs any updates that need to be done at each frame. This function is overridable from it's children.
        /// </summary>
        public virtual void Update()
        {
            // Do any per-frame updates needed that are applicable to every type of entity

            foreach (EntityBase child in m_children)
            {
                child.Update();
            }
        }

        /// <summary>
        /// Called at a set interval to inform entities that they should back themsleves up to the DB 
        /// </summary>
        public virtual void BackUp()
        {
        }

        /// <summary>
        /// Copies the entity
        /// </summary>
        /// <returns></returns>
        public virtual EntityBase Copy()
        {
            return (EntityBase) MemberwiseClone();
        }

        /// <summary>
        /// Infoms the entity that the land (heightmap) has changed
        /// </summary>
        public virtual void LandRenegerated()
        {
        }
    }
}