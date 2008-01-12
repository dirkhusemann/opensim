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
* THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
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
/* Original code: Tedd Hansen */
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using libsecondlife;
using OpenSim.Framework;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Region.ScriptEngine.Common;

namespace OpenSim.Region.ScriptEngine.Common.ScriptEngineBase
{
    /// <summary>
    /// Loads scripts
    /// Compiles them if necessary
    /// Execute functions for EventQueueManager (Sends them to script on other AppDomain for execution)
    /// </summary>
    /// 

    // This class is as close as you get to the script without being inside script class. It handles all the dirty work for other classes.
    // * Keeps track of running scripts
    // * Compiles script if necessary (through "Compiler")
    // * Loads script (through "AppDomainManager" called from for example "EventQueueManager")
    // * Executes functions inside script (called from for example "EventQueueManager" class)
    // * Unloads script (through "AppDomainManager" called from for example "EventQueueManager")
    // * Dedicated load/unload thread, and queues loading/unloading.
    //   This so that scripts starting or stopping will not slow down other theads or whole system.
    //
    [Serializable]
    public abstract class ScriptManager
    {
        #region Declares

        private Thread scriptLoadUnloadThread;
        private int scriptLoadUnloadThread_IdleSleepms = 100;
        private Queue<LUStruct> LUQueue = new Queue<LUStruct>();


        // Load/Unload structure
        private struct LUStruct
        {
            public uint localID;
            public LLUUID itemID;
            public string script;
            public LUType Action;
        }

        private enum LUType
        {
            Unknown = 0,
            Load = 1,
            Unload = 2
        }

        // Object<string, Script<string, script>>
        // IMPORTANT: Types and MemberInfo-derived objects require a LOT of memory.
        // Instead use RuntimeTypeHandle, RuntimeFieldHandle and RunTimeHandle (IntPtr) instead!
        public Dictionary<uint, Dictionary<LLUUID, IScript>> Scripts =
            new Dictionary<uint, Dictionary<LLUUID, IScript>>();

        public Scene World
        {
            get { return m_scriptEngine.World; }
        }

        #endregion

        #region Object init/shutdown

        public ScriptEngineBase.ScriptEngine m_scriptEngine;

        public ScriptManager(ScriptEngineBase.ScriptEngine scriptEngine)
        {
            m_scriptEngine = scriptEngine;
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            scriptLoadUnloadThread = new Thread(ScriptLoadUnloadThreadLoop);
            scriptLoadUnloadThread.Name = "ScriptLoadUnloadThread";
            scriptLoadUnloadThread.IsBackground = true;
            scriptLoadUnloadThread.Priority = ThreadPriority.BelowNormal;
            scriptLoadUnloadThread.Start();
        }

        ~ScriptManager()
        {
            // Abort load/unload thread
            try
            {
                if (scriptLoadUnloadThread != null)
                {
                    if (scriptLoadUnloadThread.IsAlive == true)
                    {
                        scriptLoadUnloadThread.Abort();
                        scriptLoadUnloadThread.Join();
                    }
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Load / Unload scripts (Thread loop)

        private void ScriptLoadUnloadThreadLoop()
        {
            try
            {
                while (true)
                {
                    if (LUQueue.Count == 0)
                        Thread.Sleep(scriptLoadUnloadThread_IdleSleepms);
                    if (LUQueue.Count > 0)
                    {
                        LUStruct item = LUQueue.Dequeue();
                        lock (startStopLock)        // Lock so we have only 1 thread working on loading/unloading of scripts
                        {
                            if (item.Action == LUType.Unload)
                            {
                                _StopScript(item.localID, item.itemID);
                            }
                            if (item.Action == LUType.Load)
                            {
                                _StartScript(item.localID, item.itemID, item.script);
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException tae)
            {
                string a = tae.ToString();
                a = "";
                // Expected
            }
        }

        #endregion

        #region Helper functions

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //Console.WriteLine("ScriptManager.CurrentDomain_AssemblyResolve: " + args.Name);
            return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
        }

        #endregion

  

        #region Start/Stop/Reset script

        private readonly Object startStopLock = new Object();

        /// <summary>
        /// Fetches, loads and hooks up a script to an objects events
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="localID"></param>
        public void StartScript(uint localID, LLUUID itemID, string Script)
        {
            LUStruct ls = new LUStruct();
            ls.localID = localID;
            ls.itemID = itemID;
            ls.script = Script;
            ls.Action = LUType.Load;
            LUQueue.Enqueue(ls);
        }

        /// <summary>
        /// Disables and unloads a script
        /// </summary>
        /// <param name="localID"></param>
        /// <param name="itemID"></param>
        public void StopScript(uint localID, LLUUID itemID)
        {
            LUStruct ls = new LUStruct();
            ls.localID = localID;
            ls.itemID = itemID;
            ls.Action = LUType.Unload;
            LUQueue.Enqueue(ls);
        }

        // Create a new instance of the compiler (reuse)
        //private Compiler.LSL.Compiler LSLCompiler = new Compiler.LSL.Compiler();

        public abstract void _StartScript(uint localID, LLUUID itemID, string Script);

        public abstract void _StopScript(uint localID, LLUUID itemID);


        #endregion

        #region Perform event execution in script

        /// <summary>
        /// Execute a LL-event-function in Script
        /// </summary>
        /// <param name="localID">Object the script is located in</param>
        /// <param name="itemID">Script ID</param>
        /// <param name="FunctionName">Name of function</param>
        /// <param name="args">Arguments to pass to function</param>
        internal void ExecuteEvent(uint localID, LLUUID itemID, string FunctionName, object[] args)
        {
#if DEBUG
            Console.WriteLine("ScriptEngine: Inside ExecuteEvent for event " + FunctionName);
#endif
            // Execute a function in the script
            //m_scriptEngine.Log.Verbose("ScriptEngine", "Executing Function localID: " + localID + ", itemID: " + itemID + ", FunctionName: " + FunctionName);
            //ScriptBaseInterface Script = (ScriptBaseInterface)GetScript(localID, itemID);
            IScript Script = GetScript(localID, itemID);
            if (Script == null)
                return;
#if DEBUG
            Console.WriteLine("ScriptEngine: Executing event: " + FunctionName);
#endif
            // Must be done in correct AppDomain, so leaving it up to the script itself
            Script.Exec.ExecuteEvent(FunctionName, args);
        }

        #endregion

        #region Internal functions to keep track of script

        public Dictionary<LLUUID, IScript>.KeyCollection GetScriptKeys(uint localID)
        {
            if (Scripts.ContainsKey(localID) == false)
                return null;

            Dictionary<LLUUID, IScript> Obj;
            Scripts.TryGetValue(localID, out Obj);

            return Obj.Keys;
        }

        public IScript GetScript(uint localID, LLUUID itemID)
        {
            if (Scripts.ContainsKey(localID) == false)
                return null;

            Dictionary<LLUUID, IScript> Obj;
            Scripts.TryGetValue(localID, out Obj);
            if (Obj.ContainsKey(itemID) == false)
                return null;

            // Get script
            IScript Script;
            Obj.TryGetValue(itemID, out Script);

            return Script;
        }

        public void SetScript(uint localID, LLUUID itemID, IScript Script)
        {
            // Create object if it doesn't exist
            if (Scripts.ContainsKey(localID) == false)
            {
                Scripts.Add(localID, new Dictionary<LLUUID, IScript>());
            }

            // Delete script if it exists
            Dictionary<LLUUID, IScript> Obj;
            Scripts.TryGetValue(localID, out Obj);
            if (Obj.ContainsKey(itemID) == true)
                Obj.Remove(itemID);

            // Add to object
            Obj.Add(itemID, Script);
        }

        public void RemoveScript(uint localID, LLUUID itemID)
        {
            // Don't have that object?
            if (Scripts.ContainsKey(localID) == false)
                return;

            // Delete script if it exists
            Dictionary<LLUUID, IScript> Obj;
            Scripts.TryGetValue(localID, out Obj);
            if (Obj.ContainsKey(itemID) == true)
                Obj.Remove(itemID);
        }

        #endregion


        public void ResetScript(uint localID, LLUUID itemID)
        {
            string script = GetScript(localID, itemID).Source;
            StopScript(localID, itemID);
            StartScript(localID, itemID, script);
        }


        #region Script serialization/deserialization

        public void GetSerializedScript(uint localID, LLUUID itemID)
        {
            // Serialize the script and return it
            // Should not be a problem
            FileStream fs = File.Create("SERIALIZED_SCRIPT_" + itemID);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(fs, GetScript(localID, itemID));
            fs.Close();
        }

        public void PutSerializedScript(uint localID, LLUUID itemID)
        {
            // Deserialize the script and inject it into an AppDomain

            // How to inject into an AppDomain?
        }

        #endregion
    }
}