﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SilverSim.Tests.Scripting
{
    public class Compile : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        Dictionary<UUID, string> Files = new Dictionary<UUID, string>();
        TestRunner m_Runner;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            foreach (string key in config.GetKeys())
            {
                UUID uuid;
                if (UUID.TryParse(key, out uuid))
                {
                    Files[uuid] = config.GetString(key);
                }
            }
            CompilerRegistry.ScriptCompilers.DefaultCompilerName = config.GetString("DefaultCompiler");
            m_Runner = loader.GetServicesByValue<TestRunner>()[0];
            m_Runner.ExcludeSummaryCount = true;
        }

        public void Setup()
        {

        }

        public void Cleanup()
        {
            m_Runner = null;
        }

        public bool Run()
        {
            bool success = true;
            int count = 0;
            int successcnt = 0;
            foreach (KeyValuePair<UUID, string> file in Files)
            {
                ++count;
                TestRunner.TestResult tr = new TestRunner.TestResult();
                tr.Name = "Script " + file.Key + "(" + file.Value + ")";
                tr.Result = false;
                tr.Message = string.Empty;
                int startTime = Environment.TickCount;
                m_Log.InfoFormat("Testing compilation of {1} ({0})", file.Key, file.Value);
                try
                {
                    using (TextReader reader = new StreamReader(file.Value, new UTF8Encoding(false)))
                    {
                        CompilerRegistry.ScriptCompilers.Compile(AppDomain.CurrentDomain, UUI.Unknown, file.Key, reader);
                    }
                    m_Log.InfoFormat("Compilation of {1} ({0}) successful", file.Key, file.Value);
                    ++successcnt;
                    tr.Result = true;
                }
                catch (CompilerException e)
                {
                    m_Log.ErrorFormat("Compilation of {1} ({0}) failed: {2}", file.Key, file.Value, e.Message);
                    m_Log.WarnFormat("Stack Trace:\n{0}", e.StackTrace);
                    tr.Message = e.Message + "\n" + e.StackTrace;
                    success = false;
                }
                catch (Exception e)
                {
                    m_Log.ErrorFormat("Compilation of {1} ({0}) failed: {2}", file.Key, file.Value, e.Message);
                    m_Log.WarnFormat("Stack Trace:\n{0}", e.StackTrace);
                    tr.Message = e.Message + "\n" + e.StackTrace;
                    success = false;
                }
                tr.RunTime = Environment.TickCount - startTime;
                m_Runner.TestResults.Add(tr);
            }
            m_Log.InfoFormat("{0} of {1} compilations successful", successcnt, count);
            return success;
        }
    }
}
