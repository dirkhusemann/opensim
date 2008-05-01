﻿using System;

namespace OpenSim.Framework
{
    [Serializable]
    public class Location : ICloneable
    {
        private readonly int m_x;
        private readonly int m_y;

        public Location(int x, int y)
        {
            m_x = x;
            m_y = y;
        }

        public int X
        {
            get { return m_x; }
        }

        public int Y
        {
            get { return m_y; }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;

            if (obj is Location)
            {
                return Equals((Location) obj);
            }

            return base.Equals(obj);
        }

        public bool Equals(Location loc)
        {
            return loc.X == X && loc.Y == Y;
        }

        public bool Equals(int x, int y)
        {
            return X == x && y == Y;
        }

        public UInt64 RegionHandle
        {
            get { return UInt64.MinValue; }
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() * 29 + Y.GetHashCode();
        }

        public object Clone()
        {
            return new Location(X, Y);
        }
    }
}