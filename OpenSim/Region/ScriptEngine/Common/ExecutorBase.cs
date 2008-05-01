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
 */

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Lifetime;

namespace OpenSim.Region.ScriptEngine.Common
{
    public abstract class ExecutorBase : MarshalByRefObject
    {
        /// <summary>
        /// Contains the script to execute functions in.
        /// </summary>
        protected IScript m_Script;
        /// <summary>
        /// If set to False events will not be executed.
        /// </summary>
        protected bool m_Running = true;
        /// <summary>
        /// True indicates that the ScriptManager has stopped 
        /// this script. This prevents a script that has been
        /// stopped as part of deactivation from being
        /// resumed by a pending llSetScriptState request.
        /// </summary>
        protected bool m_Disable = false;

        /// <summary>
        /// Indicate the scripts current running status.
        /// </summary>
        public bool Running
        {
            get { return m_Running; }
            set
            {
                if (!m_Disable)
                    m_Running = value;
            }
        }

        protected Dictionary<string, scriptEvents> m_eventFlagsMap = new Dictionary<string, scriptEvents>();

        [Flags]
        public enum scriptEvents : int
        {
            None = 0,
            attach = 1,
            collision = 15,
            collision_end = 32,
            collision_start = 64,
            control = 128,
            dataserver = 256,
            email = 512,
            http_response = 1024,
            land_collision = 2048,
            land_collision_end = 4096,
            land_collision_start = 8192,
            at_target = 16384,
            listen = 32768,
            money = 65536,
            moving_end = 131072,
            moving_start = 262144,
            not_at_rot_target = 524288,
            not_at_target = 1048576,
            remote_data = 8388608,
            run_time_permissions = 268435456,
            state_entry = 1073741824,
            state_exit = 2,
            timer = 4,
            touch = 8,
            touch_end = 536870912,
            touch_start = 2097152,
            object_rez = 4194304
        }

        /// <summary>
        /// Create a new instance of ExecutorBase
        /// </summary>
        /// <param name="Script"></param>
        public ExecutorBase(IScript Script)
        {
            m_Script = Script;
            initEventFlags();
        }

        /// <summary>
        /// Make sure our object does not timeout when in AppDomain. (Called by ILease base class)
        /// </summary>
        /// <returns></returns>
        public override Object InitializeLifetimeService()
        {
            //Console.WriteLine("Executor: InitializeLifetimeService()");
            //            return null;
            ILease lease = (ILease)base.InitializeLifetimeService();

            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.Zero; // TimeSpan.FromMinutes(1);
                //                lease.SponsorshipTimeout = TimeSpan.FromMinutes(2);
                //                lease.RenewOnCallTime = TimeSpan.FromSeconds(2);
            }
            return lease;
        }

        /// <summary>
        /// Get current AppDomain
        /// </summary>
        /// <returns>Current AppDomain</returns>
        public AppDomain GetAppDomain()
        {
            return AppDomain.CurrentDomain;
        }

        /// <summary>
        /// Execute a specific function/event in script.
        /// </summary>
        /// <param name="FunctionName">Name of function to execute</param>
        /// <param name="args">Arguments to pass to function</param>
        public void ExecuteEvent(string FunctionName, object[] args)
        {
            if (m_Running == false)
            {
                // Script is inactive, do not execute!
                return;
            }
            DoExecuteEvent(FunctionName, args);
        }

        protected abstract void DoExecuteEvent(string FunctionName, object[] args);

        /// <summary>
        ///  Compute the events handled by the current state of the script
        /// </summary>
        /// <returns>state mask</returns>
        public scriptEvents GetStateEventFlags()
        {
            return DoGetStateEventFlags();
        }

        protected abstract scriptEvents DoGetStateEventFlags();

        /// <summary>
        /// Stop script from running. Event execution will be ignored.
        /// </summary>
        public void StopScript()
        {
            m_Running = false;
            m_Disable = true;
        }

        protected void initEventFlags()
        {
            // Initialize the table if it hasn't already been done
            if (m_eventFlagsMap.Count > 0)
            {
                return;
            }

            m_eventFlagsMap.Add("attach", scriptEvents.attach);
            // m_eventFlagsMap.Add("at_rot_target",(long)scriptEvents.at_rot_target);
            m_eventFlagsMap.Add("at_target", scriptEvents.at_target);
            // m_eventFlagsMap.Add("changed",(long)scriptEvents.changed);
            m_eventFlagsMap.Add("collision", scriptEvents.collision);
            m_eventFlagsMap.Add("collision_end", scriptEvents.collision_end);
            m_eventFlagsMap.Add("collision_start", scriptEvents.collision_start);
            m_eventFlagsMap.Add("control", scriptEvents.control);
            m_eventFlagsMap.Add("dataserver", scriptEvents.dataserver);
            m_eventFlagsMap.Add("email", scriptEvents.email);
            m_eventFlagsMap.Add("http_response", scriptEvents.http_response);
            m_eventFlagsMap.Add("land_collision", scriptEvents.land_collision);
            m_eventFlagsMap.Add("land_collision_end", scriptEvents.land_collision_end);
            m_eventFlagsMap.Add("land_collision_start", scriptEvents.land_collision_start);
            // m_eventFlagsMap.Add("link_message",scriptEvents.link_message);
            m_eventFlagsMap.Add("listen", scriptEvents.listen);
            m_eventFlagsMap.Add("money", scriptEvents.money);
            m_eventFlagsMap.Add("moving_end", scriptEvents.moving_end);
            m_eventFlagsMap.Add("moving_start", scriptEvents.moving_start);
            m_eventFlagsMap.Add("not_at_rot_target", scriptEvents.not_at_rot_target);
            m_eventFlagsMap.Add("not_at_target", scriptEvents.not_at_target);
            // m_eventFlagsMap.Add("no_sensor",(long)scriptEvents.no_sensor);
            // m_eventFlagsMap.Add("on_rez",(long)scriptEvents.on_rez);
            m_eventFlagsMap.Add("remote_data", scriptEvents.remote_data);
            m_eventFlagsMap.Add("run_time_permissions", scriptEvents.run_time_permissions);
            // m_eventFlagsMap.Add("sensor",(long)scriptEvents.sensor);
            m_eventFlagsMap.Add("state_entry", scriptEvents.state_entry);
            m_eventFlagsMap.Add("state_exit", scriptEvents.state_exit);
            m_eventFlagsMap.Add("timer", scriptEvents.timer);
            m_eventFlagsMap.Add("touch", scriptEvents.touch);
            m_eventFlagsMap.Add("touch_end", scriptEvents.touch_end);
            m_eventFlagsMap.Add("touch_start", scriptEvents.touch_start);
            m_eventFlagsMap.Add("object_rez", scriptEvents.object_rez);
        }
    }
}
