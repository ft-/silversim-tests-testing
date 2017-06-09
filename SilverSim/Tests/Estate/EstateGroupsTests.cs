﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Estate;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Estate;
using System.Reflection;

namespace SilverSim.Tests.Estate
{
    public sealed class EstateGroupsTests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        EstateServiceInterface m_EstateService;
        UUI m_EstateOwner;
        UGI m_EstateGroup1;
        UGI m_EstateGroup2;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_EstateService = loader.GetService<EstateServiceInterface>(config.GetString("EstateService"));
            m_EstateOwner = new UUI(config.GetString("EstateOwner"));
            m_EstateGroup1 = new UGI(config.GetString("EstateGroup1"));
            m_EstateGroup2 = new UGI(config.GetString("EstateGroup2"));
        }

        public void Setup()
        {

        }

        public void Cleanup()
        {

        }

        public bool Run()
        {
            m_Log.Info("Creating estate");
            EstateInfo info = new EstateInfo()
            {
                Name = "Test Estate",
                ID = 101,
                Owner = m_EstateOwner
            };
            m_EstateService.Add(info);

            m_Log.Info("Testing non-existence of Estate Group 1");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup1])
            {
                return false;
            }

            m_Log.Info("Testing non-existence of Estate Group 2");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup2])
            {
                return false;
            }

            m_Log.Info("Testing returned entries to match");
            if (m_EstateService.EstateGroup.All[info.ID].Count != 0)
            {
                return false;
            }

            m_Log.Info("Enabling Estate Group 1");
            m_EstateService.EstateGroup[info.ID, m_EstateGroup1] = true;

            m_Log.Info("Testing existence of Estate Group 1");
            if (!m_EstateService.EstateGroup[info.ID, m_EstateGroup1])
            {
                return false;
            }

            m_Log.Info("Testing non-existence of Estate Group 2");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup2])
            {
                return false;
            }

            m_Log.Info("Testing returned entries to match");
            if (m_EstateService.EstateGroup.All[info.ID].Count != 1)
            {
                return false;
            }

            m_Log.Info("Enabling Estate Group 2");
            m_EstateService.EstateGroup[info.ID, m_EstateGroup2] = true;

            m_Log.Info("Testing existence of Estate Group 1");
            if (!m_EstateService.EstateGroup[info.ID, m_EstateGroup1])
            {
                return false;
            }

            m_Log.Info("Testing existence of Estate Group 2");
            if (!m_EstateService.EstateGroup[info.ID, m_EstateGroup2])
            {
                return false;
            }

            m_Log.Info("Testing returned entries to match");
            if (m_EstateService.EstateGroup.All[info.ID].Count != 2)
            {
                return false;
            }

            m_Log.Info("Disabling Estate Group 1");
            m_EstateService.EstateGroup[info.ID, m_EstateGroup1] = false;

            m_Log.Info("Testing non-existence of Estate Group 1");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup1])
            {
                return false;
            }

            m_Log.Info("Testing existence of Estate Group 2");
            if (!m_EstateService.EstateGroup[info.ID, m_EstateGroup2])
            {
                return false;
            }

            m_Log.Info("Testing returned entries to match");
            if (m_EstateService.EstateGroup.All[info.ID].Count != 1)
            {
                return false;
            }

            m_Log.Info("Disabling Estate Group 2");
            m_EstateService.EstateGroup[info.ID, m_EstateGroup2] = false;

            m_Log.Info("Testing non-existence of Estate Group 1");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup1])
            {
                return false;
            }

            m_Log.Info("Testing non-existence of Estate Group 2");
            if (m_EstateService.EstateGroup[info.ID, m_EstateGroup2])
            {
                return false;
            }

            m_Log.Info("Testing returned entries to match");
            if (m_EstateService.EstateGroup.All[info.ID].Count != 0)
            {
                return false;
            }

            m_Log.Info("Testing deletion");
            if (!m_EstateService.Remove(info.ID))
            {
                return false;
            }
            return true;
        }
    }
}