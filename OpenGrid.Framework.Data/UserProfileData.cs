using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife;

namespace OpenGrid.Framework.Data
{
    public class UserProfileData
    {
        public LLUUID UUID;
        public string username;    // The configurable part of the users username
        public string surname;     // The users surname (can be used to indicate user class - eg 'Test User' or 'Test Admin')

        public string passwordHash; // Hash of the users password
        public string passwordSalt; // Salt for the users password

        public ulong homeRegion;       // RegionHandle of home
        public LLVector3 homeLocation; // Home Location inside the sim
        public LLVector3 homeLookAt;   // Coordinates where the user is looking


        public int created;    // UNIX Epoch Timestamp (User Creation)
        public int lastLogin;  // UNIX Epoch Timestamp (Last Login Time)

        public string userInventoryURI; // URI to inventory server for this user
        public string userAssetURI;     // URI to asset server for this user

        public uint profileCanDoMask; // Profile window "I can do" mask
        public uint profileWantDoMask; // Profile window "I want to" mask

        public string profileAboutText; // My about window text
        public string profileFirstText; // First Life Text

        public LLUUID profileImage; // My avatars profile image
        public LLUUID profileFirstImage; // First-life image
        public UserAgentData currentAgent; // The users last agent
    }

    public class UserAgentData
    {
        public LLUUID UUID;             // Internal session ID
        public string agentIP;          // The IP of the agent
        public uint agentPort;          // The port of the agent
        public bool agentOnline;        // The online status of the agent
        public LLUUID sessionID;        // The session ID for the agent (used by client)
        public LLUUID secureSessionID;  // The secure session ID for the agent (used by client)
        public LLUUID regionID;         // The region ID the agent occupies
        public int loginTime;          // EPOCH based Timestamp
        public int logoutTime;         // Timestamp or 0 if N/A
        public LLUUID currentRegion;    // UUID of the users current region
        public ulong currentHandle;     // RegionHandle of the users current region
        public LLVector3 currentPos;    // Current position in the region
    }
}
