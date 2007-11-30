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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AS IS AND ANY
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
using System.Collections;
using System.Collections.Generic;
using libsecondlife;
using Nwc.XmlRpc;
using OpenSim.Framework.Console;

namespace OpenSim.Framework.UserManagement
{
    /// <summary>
    /// A temp class to handle login response.
    /// Should make use of UserProfileManager where possible.
    /// </summary>
    public class LoginResponse
    {
        private Hashtable loginFlagsHash;
        private Hashtable globalTexturesHash;
        private Hashtable loginError;
        private Hashtable uiConfigHash;

        private ArrayList loginFlags;
        private ArrayList globalTextures;
        private ArrayList eventCategories;
        private ArrayList uiConfig;
        private ArrayList classifiedCategories;
        private ArrayList inventoryRoot;
        private ArrayList initialOutfit;
        private ArrayList agentInventory;
        private ArrayList inventoryLibraryOwner;
        private ArrayList inventoryLibRoot;
        private ArrayList inventoryLibrary;

        private UserInfo userProfile;

        private LLUUID agentID;
        private LLUUID sessionID;
        private LLUUID secureSessionID;

        // Login Flags
        private string dst;
        private string stipendSinceLogin;
        private string gendered;
        private string everLoggedIn;
        private string login;
        private int simPort;
        private string simAddress;
        private string agentAccess;
        private Int32 circuitCode;
        private uint regionX;
        private uint regionY;

        // Login
        private string firstname;
        private string lastname;

        // Global Textures
        private string sunTexture;
        private string cloudTexture;
        private string moonTexture;

        // Error Flags
        private string errorReason;
        private string errorMessage;

        // Response
        private XmlRpcResponse xmlRpcResponse;
        private XmlRpcResponse defaultXmlRpcResponse;

        private string welcomeMessage;
        private string startLocation;
        private string allowFirstLife;
        private string home;
        private string seedCapability;
        private string lookAt;

        private BuddyList m_buddyList = null;

        public LoginResponse()
        {
            loginFlags = new ArrayList();
            globalTextures = new ArrayList();
            eventCategories = new ArrayList();
            uiConfig = new ArrayList();
            classifiedCategories = new ArrayList();

            loginError = new Hashtable();
            uiConfigHash = new Hashtable();

            defaultXmlRpcResponse = new XmlRpcResponse();
            userProfile = new UserInfo();
            inventoryRoot = new ArrayList();
            initialOutfit = new ArrayList();
            agentInventory = new ArrayList();
            inventoryLibrary = new ArrayList();
            inventoryLibraryOwner = new ArrayList();

            xmlRpcResponse = new XmlRpcResponse();
            defaultXmlRpcResponse = new XmlRpcResponse();

            SetDefaultValues();
        } // LoginServer

        public void SetDefaultValues()
        {
            DST = "N";
            StipendSinceLogin = "N";
            Gendered = "Y";
            EverLoggedIn = "Y";
            login = "false";
            firstname = "Test";
            lastname = "User";
            agentAccess = "M";
            startLocation = "last";
            allowFirstLife = "Y";

            SunTexture = "cce0f112-878f-4586-a2e2-a8f104bba271";
            CloudTexture = "dc4b9f0b-d008-45c6-96a4-01dd947ac621";
            MoonTexture = "ec4b9f0b-d008-45c6-96a4-01dd947ac621";

            ErrorMessage = "You have entered an invalid name/password combination.  Check Caps/lock.";
            ErrorReason = "key";
            welcomeMessage = "Welcome to OpenSim!";
            seedCapability = "";
            home = "{'region_handle':[r" + (1000*256).ToString() + ",r" + (1000*256).ToString() + "], 'position':[r" +
                   userProfile.homepos.X.ToString() + ",r" + userProfile.homepos.Y.ToString() + ",r" +
                   userProfile.homepos.Z.ToString() + "], 'look_at':[r" + userProfile.homelookat.X.ToString() + ",r" +
                   userProfile.homelookat.Y.ToString() + ",r" + userProfile.homelookat.Z.ToString() + "]}";
            lookAt = "[r0.99949799999999999756,r0.03166859999999999814,r0]";
            RegionX = (uint) 255232;
            RegionY = (uint) 254976;

            // Classifieds;
            AddClassifiedCategory((Int32) 1, "Shopping");
            AddClassifiedCategory((Int32) 2, "Land Rental");
            AddClassifiedCategory((Int32) 3, "Property Rental");
            AddClassifiedCategory((Int32) 4, "Special Attraction");
            AddClassifiedCategory((Int32) 5, "New Products");
            AddClassifiedCategory((Int32) 6, "Employment");
            AddClassifiedCategory((Int32) 7, "Wanted");
            AddClassifiedCategory((Int32) 8, "Service");
            AddClassifiedCategory((Int32) 9, "Personal");


            SessionID = LLUUID.Random();
            SecureSessionID = LLUUID.Random();
            AgentID = LLUUID.Random();

            Hashtable InitialOutfitHash = new Hashtable();
            InitialOutfitHash["folder_name"] = "Nightclub Female";
            InitialOutfitHash["gender"] = "female";
            initialOutfit.Add(InitialOutfitHash);
        } // SetDefaultValues

        #region Login Failure Methods

        public XmlRpcResponse GenerateFailureResponse(string reason, string message, string login)
        {
            // Overwrite any default values;
            xmlRpcResponse = new XmlRpcResponse();

            // Ensure Login Failed message/reason;
            ErrorMessage = message;
            ErrorReason = reason;

            loginError["reason"] = ErrorReason;
            loginError["message"] = ErrorMessage;
            loginError["login"] = login;
            xmlRpcResponse.Value = loginError;
            return (xmlRpcResponse);
        } // GenerateResponse

        public XmlRpcResponse CreateFailedResponse()
        {
            return (CreateLoginFailedResponse());
        } // CreateErrorConnectingToGridResponse()

        public XmlRpcResponse CreateLoginFailedResponse()
        {
            return
                (GenerateFailureResponse("key",
                                         "Could not authenticate your avatar. Please check your username and password, and check the grid if problems persist.",
                                         "false"));
        } // LoginFailedResponse

        public XmlRpcResponse CreateAlreadyLoggedInResponse()
        {
            return
                (GenerateFailureResponse("presence",
                                         "You appear to be already logged in, if this is not the case please wait for your session to timeout, if this takes longer than a few minutes please contact the grid owner",
                                         "false"));
        } // CreateAlreadyLoggedInResponse()

        public XmlRpcResponse CreateDeadRegionResponse()
        {
            return
                (GenerateFailureResponse("key",
                                         "The region you are attempting to log into is not responding. Please select another region and try again.",
                                         "false"));
        }

        public XmlRpcResponse CreateGridErrorResponse()
        {
            return
                (GenerateFailureResponse("key",
                                         "Error connecting to grid. Could not percieve credentials from login XML.",
                                         "false"));
        }

        #endregion

        public XmlRpcResponse ToXmlRpcResponse()
        {
            try
            {
                Hashtable responseData = new Hashtable();

                loginFlagsHash = new Hashtable();
                loginFlagsHash["daylight_savings"] = DST;
                loginFlagsHash["stipend_since_login"] = StipendSinceLogin;
                loginFlagsHash["gendered"] = Gendered;
                loginFlagsHash["ever_logged_in"] = EverLoggedIn;
                loginFlags.Add(loginFlagsHash);

                responseData["first_name"] = Firstname;
                responseData["last_name"] = Lastname;
                responseData["agent_access"] = agentAccess;

                globalTexturesHash = new Hashtable();
                globalTexturesHash["sun_texture_id"] = SunTexture;
                globalTexturesHash["cloud_texture_id"] = CloudTexture;
                globalTexturesHash["moon_texture_id"] = MoonTexture;
                globalTextures.Add(globalTexturesHash);
                // this.eventCategories.Add(this.eventCategoriesHash);

                AddToUIConfig("allow_first_life", allowFirstLife);
                uiConfig.Add(uiConfigHash);

                responseData["sim_port"] = (Int32) SimPort;
                responseData["sim_ip"] = SimAddress;

                responseData["agent_id"] = AgentID.ToStringHyphenated();
                responseData["session_id"] = SessionID.ToStringHyphenated();
                responseData["secure_session_id"] = SecureSessionID.ToStringHyphenated();
                responseData["circuit_code"] = CircuitCode;
                responseData["seconds_since_epoch"] = (Int32) (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                responseData["login-flags"] = loginFlags;
                responseData["global-textures"] = globalTextures;
                responseData["seed_capability"] = seedCapability;

                responseData["event_categories"] = eventCategories;
                responseData["event_notifications"] = new ArrayList(); // todo
                responseData["classified_categories"] = classifiedCategories;
                responseData["ui-config"] = uiConfig;

                responseData["inventory-skeleton"] = agentInventory;
                responseData["inventory-skel-lib"] = inventoryLibrary;
                responseData["inventory-root"] = inventoryRoot;
                responseData["inventory-lib-root"] = inventoryLibRoot;
                responseData["gestures"] = new ArrayList(); // todo
                responseData["inventory-lib-owner"] = inventoryLibraryOwner;
                responseData["initial-outfit"] = initialOutfit;
                responseData["start_location"] = startLocation;
                responseData["seed_capability"] = seedCapability;
                responseData["home"] = home;
                responseData["look_at"] = lookAt;
                responseData["message"] = welcomeMessage;
                responseData["region_x"] = (Int32) RegionX*256;
                responseData["region_y"] = (Int32) RegionY*256;

                //responseData["inventory-lib-root"] = new ArrayList(); // todo

                if (m_buddyList != null)
                {
                    responseData["buddy-list"] = m_buddyList.ToArray();
                }

                responseData["login"] = "true";
                xmlRpcResponse.Value = responseData;

                return (xmlRpcResponse);
            }
            catch (Exception e)
            {
                MainLog.Instance.Warn(
                    "CLIENT",
                    "LoginResponse: Error creating XML-RPC Response: " + e.Message
                    );
                return (GenerateFailureResponse("Internal Error", "Error generating Login Response", "false"));
            }
        } // ToXmlRpcResponse

        public void SetEventCategories(string category, string value)
        {
            //  this.eventCategoriesHash[category] = value;
            //TODO
        } // SetEventCategories

        public void AddToUIConfig(string itemName, string item)
        {
            uiConfigHash[itemName] = item;
        } // SetUIConfig

        public void AddClassifiedCategory(Int32 ID, string categoryName)
        {
            Hashtable hash = new Hashtable();
            hash["category_name"] = categoryName;
            hash["category_id"] = ID;
            classifiedCategories.Add(hash);
            // this.classifiedCategoriesHash.Clear();
        } // SetClassifiedCategory

        #region Properties

        public string Login
        {
            get { return login; }
            set { login = value; }
        } // Login

        public string DST
        {
            get { return dst; }
            set { dst = value; }
        } // DST

        public string StipendSinceLogin
        {
            get { return stipendSinceLogin; }
            set { stipendSinceLogin = value; }
        } // StipendSinceLogin

        public string Gendered
        {
            get { return gendered; }
            set { gendered = value; }
        } // Gendered

        public string EverLoggedIn
        {
            get { return everLoggedIn; }
            set { everLoggedIn = value; }
        } // EverLoggedIn

        public int SimPort
        {
            get { return simPort; }
            set { simPort = value; }
        } // SimPort

        public string SimAddress
        {
            get { return simAddress; }
            set { simAddress = value; }
        } // SimAddress

        public LLUUID AgentID
        {
            get { return agentID; }
            set { agentID = value; }
        } // AgentID

        public LLUUID SessionID
        {
            get { return sessionID; }
            set { sessionID = value; }
        } // SessionID

        public LLUUID SecureSessionID
        {
            get { return secureSessionID; }
            set { secureSessionID = value; }
        } // SecureSessionID

        public Int32 CircuitCode
        {
            get { return circuitCode; }
            set { circuitCode = value; }
        } // CircuitCode

        public uint RegionX
        {
            get { return regionX; }
            set { regionX = value; }
        } // RegionX

        public uint RegionY
        {
            get { return regionY; }
            set { regionY = value; }
        } // RegionY

        public string SunTexture
        {
            get { return sunTexture; }
            set { sunTexture = value; }
        } // SunTexture

        public string CloudTexture
        {
            get { return cloudTexture; }
            set { cloudTexture = value; }
        } // CloudTexture

        public string MoonTexture
        {
            get { return moonTexture; }
            set { moonTexture = value; }
        } // MoonTexture

        public string Firstname
        {
            get { return firstname; }
            set { firstname = value; }
        } // Firstname

        public string Lastname
        {
            get { return lastname; }
            set { lastname = value; }
        } // Lastname

        public string AgentAccess
        {
            get { return agentAccess; }
            set { agentAccess = value; }
        }

        public string StartLocation
        {
            get { return startLocation; }
            set { startLocation = value; }
        } // StartLocation

        public string LookAt
        {
            get { return lookAt; }
            set { lookAt = value; }
        }

        public string SeedCapability
        {
            get { return seedCapability; }
            set { seedCapability = value; }
        } // SeedCapability

        public string ErrorReason
        {
            get { return errorReason; }
            set { errorReason = value; }
        } // ErrorReason

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        } // ErrorMessage

        public ArrayList InventoryRoot
        {
            get { return inventoryRoot; }
            set { inventoryRoot = value; }
        }

        public ArrayList InventorySkeleton
        {
            get { return agentInventory; }
            set { agentInventory = value; }
        }

        public ArrayList InventoryLibrary
        {
            get { return inventoryLibrary; }
            set { inventoryLibrary = value; }
        }

        public ArrayList InventoryLibraryOwner
        {
            get { return inventoryLibraryOwner; }
            set { inventoryLibraryOwner = value; }
        }

        public ArrayList InventoryLibRoot
        {
            get { return inventoryLibRoot; }
            set { inventoryLibRoot = value; }
        }

        public string Home
        {
            get { return home; }
            set { home = value; }
        }

        public string Message
        {
            get { return welcomeMessage; }
            set { welcomeMessage = value; }
        }

       public BuddyList BuddList
        {
            get{return m_buddyList;}
            set { m_buddyList = value; }
        }

        #endregion

        public class UserInfo
        {
            public string firstname;
            public string lastname;
            public ulong homeregionhandle;
            public LLVector3 homepos;
            public LLVector3 homelookat;
        }

        public class BuddyList
        {
            public List<BuddyInfo> Buddies = new List<BuddyInfo>();

            public void AddNewBuddy(BuddyInfo buddy)
            {
                if (!Buddies.Contains(buddy))
                {
                    Buddies.Add(buddy);
                }
            }

            public ArrayList ToArray()
            {
                ArrayList buddyArray = new ArrayList();
                foreach (BuddyInfo buddy in Buddies)
                {
                    buddyArray.Add(buddy.ToHashTable());
                }
                return buddyArray;
            }

            public class BuddyInfo
            {
                public int BuddyRightsHave = 1;
                public int BuddyRightsGiven = 1;
                public LLUUID BuddyID;

                public BuddyInfo(string buddyID)
                {
                    BuddyID = new LLUUID(buddyID);
                }

                public BuddyInfo(LLUUID buddyID)
                {
                    BuddyID = buddyID;
                }

                public Hashtable ToHashTable()
                {
                    Hashtable hTable = new Hashtable();
                    hTable["buddy_rights_has"] = BuddyRightsHave;
                    hTable["buddy_rights_given"] = BuddyRightsGiven;
                    hTable["buddy_id"] = BuddyID.ToStringHyphenated();
                    return hTable;
                }
            }
        }
    }
}
