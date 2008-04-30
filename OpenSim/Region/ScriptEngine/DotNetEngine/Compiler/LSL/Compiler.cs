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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.CSharp;
using Microsoft.JScript;
using Microsoft.VisualBasic;
using OpenSim.Region.Environment.Interfaces;

namespace OpenSim.Region.ScriptEngine.DotNetEngine.Compiler.LSL
{
    public class Compiler
    {
        // * Uses "LSL2Converter" to convert LSL to C# if necessary.
        // * Compiles C#-code into an assembly
        // * Returns assembly name ready for AppDomain load.
        //
        // Assembly is compiled using LSL_BaseClass as base. Look at debug C# code file created when LSL script is compiled for full details.
        //

        internal enum enumCompileType
        {
            lsl = 0,
            cs = 1,
            vb = 2,
            js = 3
        }

        /// <summary>
        /// This contains number of lines WE use for header when compiling script. User will get error in line x-LinesToRemoveOnError when error occurs.
        /// </summary>
        public int LinesToRemoveOnError = 3;
        private enumCompileType DefaultCompileLanguage;
        private bool WriteScriptSourceToDebugFile;
        private bool CompileWithDebugInformation;
        private bool CleanUpOldScriptsOnStartup;
        private Dictionary<string, bool> AllowedCompilers = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
        private Dictionary<string, enumCompileType> LanguageMapping = new Dictionary<string, enumCompileType>(StringComparer.CurrentCultureIgnoreCase);

        private string FilePrefix;
        private string ScriptEnginesPath = "ScriptEngines";

        private static LSL2CSConverter LSL_Converter = new LSL2CSConverter();
        private static CSharpCodeProvider CScodeProvider = new CSharpCodeProvider();
        private static VBCodeProvider VBcodeProvider = new VBCodeProvider();
        private static JScriptCodeProvider JScodeProvider = new JScriptCodeProvider();

        private static int instanceID = new Random().Next(0, int.MaxValue);                 // Unique number to use on our compiled files
        private static UInt64 scriptCompileCounter = 0;                                     // And a counter

        public Common.ScriptEngineBase.ScriptEngine m_scriptEngine;
        public Compiler(Common.ScriptEngineBase.ScriptEngine scriptEngine)
        {
            m_scriptEngine = scriptEngine;
            ReadConfig();
        }
        public bool in_startup = true;
        public void ReadConfig()
        {

            // Get some config
            WriteScriptSourceToDebugFile = m_scriptEngine.ScriptConfigSource.GetBoolean("WriteScriptSourceToDebugFile", true);
            CompileWithDebugInformation = m_scriptEngine.ScriptConfigSource.GetBoolean("CompileWithDebugInformation", true);
            CleanUpOldScriptsOnStartup = m_scriptEngine.ScriptConfigSource.GetBoolean("CleanUpOldScriptsOnStartup", true);

            // Get file prefix from scriptengine name and make it file system safe:
            FilePrefix = m_scriptEngine.ScriptEngineName;
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                FilePrefix = FilePrefix.Replace(c, '_');
            }

            // First time we start? Delete old files
            if (in_startup)
            {
                in_startup = false;
                DeleteOldFiles();
            }

            // Map name and enum type of our supported languages
            LanguageMapping.Add(enumCompileType.cs.ToString(), enumCompileType.cs);
            LanguageMapping.Add(enumCompileType.vb.ToString(), enumCompileType.vb);
            LanguageMapping.Add(enumCompileType.lsl.ToString(), enumCompileType.lsl);
            LanguageMapping.Add(enumCompileType.js.ToString(), enumCompileType.js);

            // Allowed compilers
            string allowComp = m_scriptEngine.ScriptConfigSource.GetString("AllowedCompilers", "lsl,cs,vb,js");
            AllowedCompilers.Clear();

#if DEBUG
            m_scriptEngine.Log.Debug("[" + m_scriptEngine.ScriptEngineName + "]: Allowed languages: " + allowComp);
#endif


            foreach (string strl in allowComp.Split(','))
            {
                string strlan = strl.Trim(" \t".ToCharArray()).ToLower();
                if (!LanguageMapping.ContainsKey(strlan))
                {
                    m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Config error. Compiler is unable to recognize language type \"" + strlan + "\" specified in \"AllowedCompilers\".");
                }
                else
                {
#if DEBUG
                    //m_scriptEngine.Log.Debug("[" + m_scriptEngine.ScriptEngineName + "]: Config OK. Compiler recognized language type \"" + strlan + "\" specified in \"AllowedCompilers\".");
#endif
                }
                AllowedCompilers.Add(strlan, true);
            }
            if (AllowedCompilers.Count == 0)
                m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Config error. Compiler could not recognize any language in \"AllowedCompilers\". Scripts will not be executed!");

            // Default language
            string defaultCompileLanguage = m_scriptEngine.ScriptConfigSource.GetString("DefaultCompileLanguage", "lsl").ToLower();

            // Is this language recognized at all?
            if (!LanguageMapping.ContainsKey(defaultCompileLanguage))
            {
                m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: " +
                                            "Config error. Default language \"" + defaultCompileLanguage + "\" specified in \"DefaultCompileLanguage\" is not recognized as a valid language. Changing default to: \"lsl\".");
                defaultCompileLanguage = "lsl";
            }

            // Is this language in allow-list?
            if (!AllowedCompilers.ContainsKey(defaultCompileLanguage))
            {
                m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: " +
                                            "Config error. Default language \"" + defaultCompileLanguage + "\"specified in \"DefaultCompileLanguage\" is not in list of \"AllowedCompilers\". Scripts may not be executed!");
            }
            else
            {
#if DEBUG
//                m_scriptEngine.Log.Debug("[" + m_scriptEngine.ScriptEngineName + "]: " +
//                                            "Config OK. Default language \"" + defaultCompileLanguage + "\" specified in \"DefaultCompileLanguage\" is recognized as a valid language.");
#endif
                // LANGUAGE IS IN ALLOW-LIST
                DefaultCompileLanguage = LanguageMapping[defaultCompileLanguage];
            }

            // We now have an allow-list, a mapping list, and a default language

        }

        /// <summary>
        /// Delete old script files
        /// </summary>
        private void DeleteOldFiles()
        {

            // CREATE FOLDER IF IT DOESNT EXIST
            if (!Directory.Exists(ScriptEnginesPath))
            {
                try
                {
                    Directory.CreateDirectory(ScriptEnginesPath);
                }
                catch (Exception ex)
                {
                    m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Exception trying to create ScriptEngine directory \"" + ScriptEnginesPath + "\": " + ex.ToString());
                }
            }

            foreach (string file in Directory.GetFiles(ScriptEnginesPath))
            {
                //m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: FILE FOUND: " + file);

                if (file.ToLower().StartsWith(FilePrefix + "_compiled_") ||
                    file.ToLower().StartsWith(FilePrefix + "_source_"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Exception trying delete old script file \"" + file + "\": " + ex.ToString());
                    }

                }
            }

        }

        ////private ICodeCompiler icc = codeProvider.CreateCompiler();
        //public string CompileFromFile(string LSOFileName)
        //{
        //    switch (Path.GetExtension(LSOFileName).ToLower())
        //    {
        //        case ".txt":
        //        case ".lsl":
        //            Common.ScriptEngineBase.Common.SendToDebug("Source code is LSL, converting to CS");
        //            return CompileFromLSLText(File.ReadAllText(LSOFileName));
        //        case ".cs":
        //            Common.ScriptEngineBase.Common.SendToDebug("Source code is CS");
        //            return CompileFromCSText(File.ReadAllText(LSOFileName));
        //        default:
        //            throw new Exception("Unknown script type.");
        //    }
        //}

        /// <summary>
        /// Converts script from LSL to CS and calls CompileFromCSText
        /// </summary>
        /// <param name="Script">LSL script</param>
        /// <returns>Filename to .dll assembly</returns>
        public string PerformScriptCompile(string Script)
        {
            enumCompileType l = DefaultCompileLanguage;


            if (Script.StartsWith("//c#", true, CultureInfo.InvariantCulture))
                l = enumCompileType.cs;
            if (Script.StartsWith("//vb", true, CultureInfo.InvariantCulture))
            {
                l = enumCompileType.vb;
                // We need to remove //vb, it won't compile with that

                Script = Script.Substring(4, Script.Length - 4);
            }
            if (Script.StartsWith("//lsl", true, CultureInfo.InvariantCulture))
                l = enumCompileType.lsl;

            if (Script.StartsWith("//js", true, CultureInfo.InvariantCulture))
                l = enumCompileType.js;

            if (!AllowedCompilers.ContainsKey(l.ToString()))
            {
                // Not allowed to compile to this language!
                string errtext = String.Empty;
                errtext += "The compiler for language \"" + l.ToString() + "\" is not in list of allowed compilers. Script will not be executed!";
                throw new Exception(errtext);
            }

            string compileScript = Script;

            if (l == enumCompileType.lsl)
            {
                // Its LSL, convert it to C#
                compileScript = LSL_Converter.Convert(Script);
                l = enumCompileType.cs;
            }

            // Insert additional assemblies here

            //ADAM: Disabled for the moment until it's working right.
            bool enableCommanderLSL = false;

            if (enableCommanderLSL == true && l == enumCompileType.cs)
            {
                foreach (KeyValuePair<string, 
                    ICommander> com 
                    in m_scriptEngine.World.GetCommanders())
                {
                    compileScript = com.Value.GenerateRuntimeAPI() + compileScript;
                }
            }

            // End of insert


            switch (l)
            {
                case enumCompileType.cs:
                    compileScript = CreateCSCompilerScript(compileScript);
                    break;
                case enumCompileType.vb:
                    compileScript = CreateVBCompilerScript(compileScript);
                    break;
                case enumCompileType.js:
                    compileScript = CreateJSCompilerScript(compileScript);
                    break;
            }

            Console.WriteLine("\n\n\n");
            Console.WriteLine(compileScript);
            Console.WriteLine("\n\n\n");

            return CompileFromDotNetText(compileScript, l);
        }

        private static string CreateJSCompilerScript(string compileScript)
        {
            compileScript = String.Empty +
                "import OpenSim.Region.ScriptEngine.Common; import System.Collections.Generic;\r\n" +
                "package SecondLife {\r\n" +
                "class Script extends OpenSim.Region.ScriptEngine.Common.BuiltIn_Commands_BaseClass { \r\n" +
                compileScript +
                "} }\r\n";
            return compileScript;
        }

        private static string CreateCSCompilerScript(string compileScript)
        {
            compileScript = EventReaderRewriter.ReWriteScriptWithPublishedEventsCS(compileScript);

            compileScript = String.Empty +
                        "using OpenSim.Region.ScriptEngine.Common; using System.Collections.Generic;\r\n" +
                        String.Empty + "namespace SecondLife { " +
                        String.Empty + "public class Script : OpenSim.Region.ScriptEngine.Common.BuiltIn_Commands_BaseClass { \r\n" +
                        @"public Script() { } " +
                        compileScript +
                        "} }\r\n";
            return compileScript;
        }

        private static string CreateVBCompilerScript(string compileScript)
        {
            compileScript = String.Empty +
                        "Imports OpenSim.Region.ScriptEngine.Common: Imports System.Collections.Generic: " +
                        String.Empty + "NameSpace SecondLife:" +
                        String.Empty + "Public Class Script: Inherits OpenSim.Region.ScriptEngine.Common.BuiltIn_Commands_BaseClass: " +
                        "\r\nPublic Sub New()\r\nEnd Sub: " +
                        compileScript +
                        ":End Class :End Namespace\r\n";
            return compileScript;
        }

        /// <summary>
        /// Compile .NET script to .Net assembly (.dll)
        /// </summary>
        /// <param name="Script">CS script</param>
        /// <returns>Filename to .dll assembly</returns>
        internal string CompileFromDotNetText(string Script, enumCompileType lang)
        {
            string ext = "." + lang.ToString();

            // Output assembly name
            scriptCompileCounter++;
            string OutFile =
                Path.Combine("ScriptEngines",
                             FilePrefix + "_compiled_" + instanceID.ToString() + "_" + scriptCompileCounter.ToString() + ".dll");
#if DEBUG
            m_scriptEngine.Log.Debug("[" + m_scriptEngine.ScriptEngineName + "]: Starting compile of \"" + OutFile + "\".");
#endif
            try
            {
                File.Delete(OutFile);
            }
            catch (Exception e) // NOTLEGIT - Should be just catching FileIOException
            {
                //m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Unable to delete old existring script-file before writing new. Compile aborted: " + e.ToString());
                throw new Exception("Unable to delete old existring script-file before writing new. Compile aborted: " + e.ToString());
            }
            //string OutFile = Path.Combine("ScriptEngines", "SecondLife.Script.dll");

            // DEBUG - write source to disk
            if (WriteScriptSourceToDebugFile)
            {
                string srcFileName = FilePrefix + "_source_" + Path.GetFileNameWithoutExtension(OutFile) + ext;
                try
                {
                    File.WriteAllText(
                        Path.Combine("ScriptEngines", srcFileName),
                        Script);
                }
                catch (Exception ex) // NOTLEGIT - Should be just catching FileIOException
                {
                    m_scriptEngine.Log.Error("[" + m_scriptEngine.ScriptEngineName + "]: Exception while trying to write script source to file \"" + srcFileName + "\": " + ex.ToString());
                }
            }

            // Do actual compile
            CompilerParameters parameters = new CompilerParameters();

            parameters.IncludeDebugInformation = true;

            // Add all available assemblies
//             foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
//             {
//                 Console.WriteLine("Adding assembly: " + asm.Location);
//                 parameters.ReferencedAssemblies.Add(asm.Location);
//             }

            string rootPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            string rootPathSE = Path.GetDirectoryName(GetType().Assembly.Location);
            //Console.WriteLine("Assembly location: " + rootPath);
            parameters.ReferencedAssemblies.Add(Path.Combine(rootPath, "OpenSim.Region.ScriptEngine.Common.dll"));
            parameters.ReferencedAssemblies.Add(Path.Combine(rootPathSE, "OpenSim.Region.ScriptEngine.DotNetEngine.dll"));

            //parameters.ReferencedAssemblies.Add("OpenSim.Region.Environment");
            parameters.GenerateExecutable = false;
            parameters.OutputAssembly = OutFile;
            parameters.IncludeDebugInformation = CompileWithDebugInformation;
            //parameters.WarningLevel = 1; // Should be 4?
            parameters.TreatWarningsAsErrors = false;

            CompilerResults results;
            switch (lang)
            {
                case enumCompileType.vb:
                    results = VBcodeProvider.CompileAssemblyFromSource(parameters, Script);
                    break;
                case enumCompileType.cs:
                    results = CScodeProvider.CompileAssemblyFromSource(parameters, Script);
                    break;
                case enumCompileType.js:
                    results = JScodeProvider.CompileAssemblyFromSource(parameters, Script);
                    break;
                default:
                    throw new Exception("Compiler is not able to recongnize language type \"" + lang.ToString() + "\"");
            }

            // Check result
            // Go through errors

            //
            // WARNINGS AND ERRORS
            //
            if (results.Errors.Count > 0)
            {
                string errtext = String.Empty;
                foreach (CompilerError CompErr in results.Errors)
                {
                    errtext += "Line number " + (CompErr.Line - LinesToRemoveOnError) +
                               ", Error Number: " + CompErr.ErrorNumber +
                               ", '" + CompErr.ErrorText + "'\r\n";
                }
                if (!File.Exists(OutFile))
                {
                    throw new Exception(errtext);
                }
            }


            //
            // NO ERRORS, BUT NO COMPILED FILE
            //
            if (!File.Exists(OutFile))
            {
                string errtext = String.Empty;
                errtext += "No compile error. But not able to locate compiled file.";
                throw new Exception(errtext);
            }
            return OutFile;
        }
    }
}
