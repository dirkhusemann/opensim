﻿/*
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
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using OpenSim.ScriptEngine.Shared;
using log4net;

namespace OpenSim.ApplicationPlugins.ScriptEngine
{
    /// <summary>
    /// Loads all Script Engine Components
    /// </summary>
    public class ScriptEnginePlugin : IApplicationPlugin
    {
        internal static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        internal OpenSimBase m_OpenSim;

        // Component providers are registered here wit a name (string)
        // When a script engine is created the components are instanciated
        public static Dictionary<string, Type> providers = new Dictionary<string, Type>();
        public static Dictionary<string, Type> scriptEngines = new Dictionary<string, Type>();


        public ScriptEnginePlugin()
        {
            // Application startup
#if DEBUG
            m_log.InfoFormat("[{0}] ##################################", Name);
            m_log.InfoFormat("[{0}] # Script Engine Component System #", Name);
            m_log.InfoFormat("[{0}] ##################################", Name);
#else
            m_log.InfoFormat("[{0}] Script Engine Component System", Name);
#endif

            // Load all modules from current directory
            // We only want files named OpenSim.ScriptEngine.*.dll
            Load(".", "OpenSim.ScriptEngine.*.dll");
        }

        public void Initialise(OpenSimBase openSim)
        {

            // Our objective: Load component .dll's
            m_OpenSim = openSim;
            //m_OpenSim.Shutdown();
        }

        private readonly static string nameIScriptEngineComponent = typeof(IScriptEngineComponent).Name; // keep interface name in managed code
        private readonly static string nameIScriptEngine = typeof(IScriptEngine).Name; // keep interface name in managed code
        /// <summary>
        /// Load components from directory
        /// </summary>
        /// <param name="directory"></param>
        public void Load(string directory, string filter)
        {
            // We may want to change how this functions as currently it required unique class names for each component

            foreach (string file in Directory.GetFiles(directory, filter))
            {
                //m_log.DebugFormat("[ScriptEngine]: Loading: [{0}].", file);
                Assembly componentAssembly = null;
                try
                {
                    componentAssembly = Assembly.LoadFrom(file);
                } catch (Exception e)
                {
                    m_log.ErrorFormat("[{0}] Error loading: \"{1}\".", Name, file);
                }
                if (componentAssembly != null)
                {
                    try
                    {
                        // Go through all types in the assembly
                        foreach (Type componentType in componentAssembly.GetTypes())
                        {
                            if (componentType.IsPublic
                                && !componentType.IsAbstract)
                            {
                                //if (componentType.IsSubclassOf(typeof(ComponentBase)))
                                if (componentType.GetInterface(nameIScriptEngineComponent) != null)
                                {
                                    // We have found an type which is derived from ProdiverBase, add it to provider list
                                    m_log.InfoFormat("[{0}] Adding component: {1}", Name, componentType.Name);
                                    lock (providers)
                                    {
                                        providers.Add(componentType.Name, componentType);
                                    }
                                }
                                //if (componentType.IsSubclassOf(typeof(ScriptEngineBase)))
                                if (componentType.GetInterface(nameIScriptEngine) != null)
                                {
                                    // We have found an type which is derived from RegionScriptEngineBase, add it to engine list
                                    m_log.InfoFormat("[{0}] Adding script engine: {1}", Name, componentType.Name);
                                    lock (scriptEngines)
                                    {
                                        scriptEngines.Add(componentType.Name, componentType);
                                    }
                                }
                            }
                        }
                    }
                    catch
                        (ReflectionTypeLoadException re)
                    {
                        m_log.ErrorFormat("[{0}] Could not load component \"{1}\": {2}", Name, componentAssembly.FullName, re.ToString());
                        int c = 0;
                        foreach (Exception e in re.LoaderExceptions)
                        {
                            c++;
                            m_log.ErrorFormat("[{0}] LoaderException {1}: {2}", Name, c, e.ToString());
                        }
                    }
                } //if
            } //foreach
        }

        public static IScriptEngineComponent GetComponentInstance(string name, params Object[] args)
        {
            if (!providers.ContainsKey(name))
                throw new Exception("ScriptEngine requested component named \"" + name +
                                    "\" that does not exist.");

            return Activator.CreateInstance(providers[name], args) as IScriptEngineComponent;
        }

        private readonly static string nameIScriptEngineRegionComponent = typeof(IScriptEngineRegionComponent).Name; // keep interface name in managed code
        public static IScriptEngineComponent GetComponentInstance(RegionInfoStructure info, string name, params Object[] args)
        {
            IScriptEngineComponent c = GetComponentInstance(name, args);

            // If module is IScriptEngineRegionComponent then it will have one instance per region and we will initialize it
            if (c.GetType().GetInterface(nameIScriptEngineRegionComponent) != null)
                ((IScriptEngineRegionComponent)c).Initialize(info);

            return c;
        }

        #region IApplicationPlugin stuff
        /// <summary>
        /// Returns the plugin version
        /// </summary>
        /// <returns>Plugin version in MAJOR.MINOR.REVISION.BUILD format</returns>
        public string Version
        {
            get { return "1.0.0.0"; }
        }

        /// <summary>
        /// Returns the plugin name
        /// </summary>
        /// <returns>Plugin name, eg MySQL User Provider</returns>
        public string Name
        {
            get { return "SECS"; }
        }

        /// <summary>
        /// Default-initialises the plugin
        /// </summary>
        public void Initialise() { }

        ///<summary>
        ///Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        ///</summary>
        ///<filterpriority>2</filterpriority>
        public void Dispose()
        {
            //throw new NotImplementedException();
        }
        #endregion

    }
}
