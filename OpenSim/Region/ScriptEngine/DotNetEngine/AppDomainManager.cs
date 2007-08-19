using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Runtime.Remoting;
using System.IO;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.Environment.Scenes.Scripting;
using OpenSim.Region.ScriptEngine.DotNetEngine.Compiler.LSL;
using OpenSim.Region.ScriptEngine.Common;

namespace OpenSim.Region.ScriptEngine.DotNetEngine
{
    public class AppDomainManager
    {
        private int MaxScriptsPerAppDomain = 1;
        /// <summary>
        /// Internal list of all AppDomains
        /// </summary>
        private List<AppDomainStructure> AppDomains = new List<AppDomainStructure>();
        /// <summary>
        /// Structure to keep track of data around AppDomain
        /// </summary>
        private struct AppDomainStructure
        {
            /// <summary>
            /// The AppDomain itself
            /// </summary>
            public AppDomain CurrentAppDomain;
            /// <summary>
            /// Number of scripts loaded into AppDomain
            /// </summary>
            public int ScriptsLoaded;
            /// <summary>
            /// Number of dead scripts
            /// </summary>
            public int ScriptsWaitingUnload;
        }
        /// <summary>
        /// Current AppDomain
        /// </summary>
        private AppDomainStructure CurrentAD;
        private object GetLock = new object(); // Mutex
        private object FreeLock = new object(); // Mutex

        //private ScriptEngine m_scriptEngine;
        //public AppDomainManager(ScriptEngine scriptEngine)
        public AppDomainManager()
        {
            //m_scriptEngine = scriptEngine;
        }

        /// <summary>
        /// Find a free AppDomain, creating one if necessary
        /// </summary>
        /// <returns>Free AppDomain</returns>
        private AppDomainStructure GetFreeAppDomain()
        {
            FreeAppDomains();
            lock(GetLock) {
                // Current full?
                if (CurrentAD.ScriptsLoaded >= MaxScriptsPerAppDomain)
                {
                    // Add it to AppDomains list and empty current
                    AppDomains.Add(CurrentAD);
                    CurrentAD = new AppDomainStructure();   
                }
                // No current
                if (CurrentAD.CurrentAppDomain == null)
                {
                    // Create a new current AppDomain
                    CurrentAD = new AppDomainStructure();
                    CurrentAD.ScriptsWaitingUnload = 0; // to avoid compile warning for not in use
                    CurrentAD.CurrentAppDomain = PrepareNewAppDomain();
                    

                }

                // Increase number of scripts loaded into this
                // TODO:
                // - We assume that every time someone wants an AppDomain they will load into it
                //   if this assumption is wrong we end up with a miscount and will never unload it.
                //   
                
                // Return AppDomain
                return CurrentAD;
            } // lock
        }

        private int AppDomainNameCount;
        /// <summary>
        /// Create and prepare a new AppDomain for scripts
        /// </summary>
        /// <returns>The new AppDomain</returns>
        private AppDomain PrepareNewAppDomain()
        {
            // Create and prepare a new AppDomain
            AppDomainNameCount++;
            // TODO: Currently security match current appdomain

            // Construct and initialize settings for a second AppDomain.
            AppDomainSetup ads = new AppDomainSetup();
            ads.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            ads.DisallowBindingRedirects = false;
            ads.DisallowCodeDownload = true;
            ads.ShadowCopyFiles = "true"; // Enabled shadowing
            ads.ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            AppDomain AD = AppDomain.CreateDomain("ScriptAppDomain_" + AppDomainNameCount, null, ads);

            // Return the new AppDomain
            return AD;

        }

        /// <summary>
        /// Unload appdomains that are full and have only dead scripts
        /// </summary>
        private void FreeAppDomains()
        {
            lock (FreeLock)
            {
                // Go through all
                foreach (AppDomainStructure ads in new System.Collections.ArrayList(AppDomains))
                {
                    // Don't process current AppDomain
                    if (ads.CurrentAppDomain != CurrentAD.CurrentAppDomain)
                    {
                        // Not current AppDomain
                        // Is number of unloaded bigger or equal to number of loaded?
                        if (ads.ScriptsLoaded <= ads.ScriptsWaitingUnload)
                        {
                            // Remove from internal list
                            AppDomains.Remove(ads);
                            // Unload
                            AppDomain.Unload(ads.CurrentAppDomain);
                        }
                    }
                } // foreach
            } // lock
        }
        


        public OpenSim.Region.ScriptEngine.DotNetEngine.Compiler.LSL.LSL_BaseClass LoadScript(string FileName, IScriptHost host) 
        {
            //LSL_BaseClass mbrt = (LSL_BaseClass)FreeAppDomain.CreateInstanceAndUnwrap(FileName, "SecondLife.Script");
            //Console.WriteLine("Base directory: " + AppDomain.CurrentDomain.BaseDirectory);
            // * Find next available AppDomain to put it in
            AppDomainStructure FreeAppDomain = GetFreeAppDomain();
            

            LSL_BaseClass mbrt = (LSL_BaseClass)FreeAppDomain.CurrentAppDomain.CreateInstanceFromAndUnwrap(FileName, "SecondLife.Script");
            //LSL_BuiltIn_Commands_Interface mbrt = (LSL_BuiltIn_Commands_Interface)FreeAppDomain.CreateInstanceFromAndUnwrap(FileName, "SecondLife.Script");
            //Type mytype = mbrt.GetType();
            //Console.WriteLine("is proxy={0}", RemotingServices.IsTransparentProxy(mbrt));

            FreeAppDomain.ScriptsLoaded++;

            //mbrt.Start();
            return mbrt;
            //return (LSL_BaseClass)mbrt;

        }


        /// <summary>
        /// Increase "dead script" counter for an AppDomain
        /// </summary>
        /// <param name="ad"></param>
        [Obsolete("Needs fixing!!!")]
        public void StopScript(AppDomain ad)
        {
            lock (FreeLock)
            {
                // Check if it is current AppDomain
                if (CurrentAD.CurrentAppDomain == ad)
                {
                    // Yes - increase
                    CurrentAD.ScriptsWaitingUnload++;
                    return;
                }

                // Lopp through all AppDomains
                foreach (AppDomainStructure ads in new System.Collections.ArrayList(AppDomains))
                {
                    if (ads.CurrentAppDomain == ad)
                    {
                        // Found it - messy code to increase structure
                        AppDomainStructure ads2 = ads;
                        ads2.ScriptsWaitingUnload++;
                        AppDomains.Remove(ads);
                        AppDomains.Add(ads2);
                        return;
                    }
                } // foreach
            } // lock
        }


    }
}
