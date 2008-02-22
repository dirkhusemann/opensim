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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OpenSim.Region.ScriptEngine.Common.ScriptEngineBase
{
    /// <summary>
    /// This class does maintenance on script engine.
    /// </summary>
    public class MaintenanceThread : iScriptEngineFunctionModule
    {
        //public ScriptEngine m_ScriptEngine;
        private int MaintenanceLoopms;
        private int MaintenanceLoopTicks_ScriptLoadUnload;
        private int MaintenanceLoopTicks_Other;


        public MaintenanceThread()
        {
            //m_ScriptEngine = _ScriptEngine;

            ReadConfig();

            // Start maintenance thread
            StartMaintenanceThread();
        }

        ~MaintenanceThread()
        {
            StopMaintenanceThread();
        }

        public void ReadConfig()
        {
            // Bad hack, but we need a m_ScriptEngine :)
            foreach (ScriptEngine m_ScriptEngine in ScriptEngine.ScriptEngines)
            {
                MaintenanceLoopms = m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopms", 50);
                MaintenanceLoopTicks_ScriptLoadUnload = m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopTicks_ScriptLoadUnload", 1);
                MaintenanceLoopTicks_Other = m_ScriptEngine.ScriptConfigSource.GetInt("MaintenanceLoopTicks_Other", 10);

                return;
            }
        }

        #region " Maintenance thread "
        /// <summary>
        /// Maintenance thread. Enforcing max execution time for example.
        /// </summary>
        public Thread MaintenanceThreadThread;

        /// <summary>
        /// Starts maintenance thread
        /// </summary>
        private void StartMaintenanceThread()
        {
            if (MaintenanceThreadThread == null)
            {
                MaintenanceThreadThread = new Thread(MaintenanceLoop);
                MaintenanceThreadThread.Name = "ScriptMaintenanceThread";
                MaintenanceThreadThread.IsBackground = true;
                MaintenanceThreadThread.Start();
                OpenSim.Framework.ThreadTracker.Add(MaintenanceThreadThread);
            }
        }

        /// <summary>
        /// Stops maintenance thread
        /// </summary>
        private void StopMaintenanceThread()
        {
#if DEBUG
            //m_ScriptEngine.Log.Debug("[" + m_ScriptEngine.ScriptEngineName + "]: StopMaintenanceThread() called");
#endif
            //PleaseShutdown = true;
            Thread.Sleep(100);
            try
            {
                if (MaintenanceThreadThread != null && MaintenanceThreadThread.IsAlive)
                {
                    MaintenanceThreadThread.Abort();
                }
            }
            catch (Exception ex)
            {
                //m_ScriptEngine.Log.Error("[" + m_ScriptEngine.ScriptEngineName + "]: Exception stopping maintenence thread: " + ex.ToString());
            }
        }

        private ScriptEngine lastScriptEngine; // Keep track of what ScriptEngine instance we are at so we can give exception
        /// <summary>
        /// A thread should run in this loop and check all running scripts
        /// </summary>
        public void MaintenanceLoop()
        {
            //if (m_ScriptEngine.m_EventQueueManager.maxFunctionExecutionTimens < MaintenanceLoopms)
            //    m_ScriptEngine.Log.Warn("[" + m_ScriptEngine.ScriptEngineName + "]: " +
            //                               "Configuration error: MaxEventExecutionTimeMs is less than MaintenanceLoopms. The Maintenance Loop will only check scripts once per run.");

            long Last_maxFunctionExecutionTimens = 0; // DateTime.Now.Ticks;
            long Last_ReReadConfigFilens = DateTime.Now.Ticks;
            long Last_MaintenanceRun = 0;
            int MaintenanceLoopTicks_ScriptLoadUnload_Count = 0;
            int MaintenanceLoopTicks_Other_Count = 0;

            while (true)
            {
                try
                {
                    while (true)
                    {
                        System.Threading.Thread.Sleep(MaintenanceLoopms); // Sleep before next pass
                        //if (PleaseShutdown)
                        //    return;
                        MaintenanceLoopTicks_ScriptLoadUnload_Count++;
                        MaintenanceLoopTicks_Other_Count++;


                        foreach (ScriptEngine m_ScriptEngine in new ArrayList(ScriptEngine.ScriptEngines))
                        {
                            lastScriptEngine = m_ScriptEngine;
                            // Re-reading config every x seconds
                            if (m_ScriptEngine.RefreshConfigFilens > 0)
                            {

                                if (MaintenanceLoopTicks_Other_Count >= MaintenanceLoopTicks_Other)
                                {
                                    MaintenanceLoopTicks_Other_Count = 0;
                                    // Check if its time to re-read config
                                    if (DateTime.Now.Ticks - Last_ReReadConfigFilens >
                                        m_ScriptEngine.RefreshConfigFilens)
                                    {
                                        //Console.WriteLine("Time passed: " + (DateTime.Now.Ticks - Last_ReReadConfigFilens) + ">" + m_ScriptEngine.RefreshConfigFilens );
                                        // Its time to re-read config file
                                        m_ScriptEngine.ReadConfig();
                                        Last_ReReadConfigFilens = DateTime.Now.Ticks; // Reset time
                                    }


                                    // Adjust number of running script threads if not correct
                                    if (m_ScriptEngine.m_EventQueueManager != null)
                                        m_ScriptEngine.m_EventQueueManager.AdjustNumberOfScriptThreads();

                                    // Check if any script has exceeded its max execution time
                                    if (EventQueueManager.EnforceMaxExecutionTime)
                                    {
                                        // We are enforcing execution time
                                        if (DateTime.Now.Ticks - Last_maxFunctionExecutionTimens >
                                            EventQueueManager.maxFunctionExecutionTimens)
                                        {
                                            // Its time to check again
                                            m_ScriptEngine.m_EventQueueManager.CheckScriptMaxExecTime(); // Do check
                                            Last_maxFunctionExecutionTimens = DateTime.Now.Ticks; // Reset time
                                        }
                                    }
                                }

                                if (MaintenanceLoopTicks_ScriptLoadUnload_Count >= MaintenanceLoopTicks_ScriptLoadUnload)
                                {
                                    MaintenanceLoopTicks_ScriptLoadUnload_Count = 0;
                                    // LOAD / UNLOAD SCRIPTS
                                    if (m_ScriptEngine.m_ScriptManager != null)
                                        m_ScriptEngine.m_ScriptManager.DoScriptLoadUnload();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (lastScriptEngine != null)
                        lastScriptEngine.Log.Error("[" + lastScriptEngine.ScriptEngineName + "]: Exception in MaintenanceLoopThread. Thread will recover after 5 sec throttle. Exception: " + ex.ToString());
                    Thread.Sleep(5000);
                }
            }
        }
        #endregion

        ///// <summary>
        ///// If set to true then threads and stuff should try to make a graceful exit
        ///// </summary>
        //public bool PleaseShutdown
        //{
        //    get { return _PleaseShutdown; }
        //    set { _PleaseShutdown = value; }
        //}
        //private bool _PleaseShutdown = false;
    }
}
