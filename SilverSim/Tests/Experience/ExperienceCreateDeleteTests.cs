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
using SilverSim.ServiceInterfaces.Experience;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Experience;
using SilverSim.Types.Grid;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Tests.Experience
{
    public class ExperienceCreateDeleteTests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        string m_ExperienceServiceName;
        ExperienceServiceInterface m_ExperienceService;
        UUID m_ExperienceID = new UUID("11112222-3333-4444-5555-666666777777");
        UGUI m_Creator = new UGUIWithName("11111111-2222-3333-4444-555555555555", "Experience", "Creator");
        UGUI m_Owner = new UGUIWithName("11223344-2222-3333-4444-555555555555", "Experience", "Owner");
        UGI m_Group = new UGI("11223344-1111-2222-3333-444444444444", "Experience Group", null);
        UUID m_GroupID = new UUID("11223344-1122-1122-1122-112233445566");
        UUID m_InsigniaID = new UUID("11223344-1122-1122-1122-112233445577");
        UEI m_UEI;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            m_ExperienceServiceName = config.GetString("ExperienceService", "ExperienceService");

            m_ExperienceService = loader.GetService<ExperienceServiceInterface>(m_ExperienceServiceName);
            m_UEI = new UEI(m_ExperienceID, "Name", new Uri("http://example.com/"));
        }

        public void Setup()
        {
        }

        bool CheckForEquality(ExperienceInfo gInfo, ExperienceInfo testGroupInfo)
        {
            var unequal = new List<string>();

            if (!gInfo.ID.ID.Equals(testGroupInfo.ID.ID))
            {
                unequal.Add($"ExperienceID ({gInfo.ID.ID}!={testGroupInfo.ID.ID})");
            }

            if (!gInfo.ID.ExperienceName.Equals(testGroupInfo.ID.ExperienceName))
            {
                unequal.Add($"ExperienceName ({gInfo.ID.ExperienceName}!={testGroupInfo.ID.ExperienceName})");
            }

            if (gInfo.Properties != testGroupInfo.Properties)
            {
                unequal.Add($"Properties ({gInfo.Properties}!={testGroupInfo.Properties})");
            }

            if (gInfo.Owner != testGroupInfo.Owner)
            {
                unequal.Add($"Owner ({gInfo.Owner}!={testGroupInfo.Owner})");
            }

            if (gInfo.Creator != testGroupInfo.Creator)
            {
                unequal.Add($"Creator ({gInfo.Owner}!={testGroupInfo.Owner})");
            }

            if (gInfo.ID.ExperienceName != testGroupInfo.ID.ExperienceName)
            {
                unequal.Add($"Name ({gInfo.ID.ExperienceName}!={testGroupInfo.ID.ExperienceName})");
            }

            if(gInfo.ID.HomeURI == null || testGroupInfo.ID.HomeURI == null || gInfo.ID.HomeURI.ToString() != testGroupInfo.ID.HomeURI.ToString())
            {
                unequal.Add($"HomeURI ({gInfo.ID.HomeURI}!={testGroupInfo.ID.HomeURI})");
            }

            if (gInfo.Description != testGroupInfo.Description)
            {
                unequal.Add($"Description ({gInfo.Description}!={testGroupInfo.Description})");
            }

            if (gInfo.Group != testGroupInfo.Group)
            {
                unequal.Add($"Group ({gInfo.Group}!={testGroupInfo.Group})");
            }

            if (gInfo.LogoID != testGroupInfo.LogoID)
            {
                unequal.Add($"LogoID ({gInfo.LogoID}!={testGroupInfo.LogoID})");
            }

            if (gInfo.Marketplace != testGroupInfo.Marketplace)
            {
                unequal.Add($"Marketplace ({gInfo.Marketplace}!={testGroupInfo.Marketplace}");
            }

            if (gInfo.SlUrl != testGroupInfo.SlUrl)
            {
                unequal.Add($"SlUrl ({gInfo.SlUrl}!={testGroupInfo.SlUrl})");
            }
            if (unequal.Count != 0)
            {
                m_Log.InfoFormat("Equality not given! {0}", string.Join(" ", unequal));
            }
            return unequal.Count == 0;
        }

        public bool Run()
        {
            ExperienceInfo gInfo;
            ExperienceInfo testGInfo;


            m_Log.Info("Checking for experience non-existence 1");
            try
            {
                gInfo = m_ExperienceService[m_ExperienceID];
                return false;
            }
            catch (KeyNotFoundException)
            {
                /* intentionally ignored */
            }

            m_Log.Info("Checking for experience non-existence 2");
            if (m_ExperienceService.TryGetValue(m_ExperienceID, out gInfo))
            {
                return false;
            }

            m_Log.Info("Checking for experience non-existence 3");
            if (m_ExperienceService.TryGetValue(m_UEI, out gInfo))
            {
                return false;
            }

            gInfo = new ExperienceInfo
            {
                Description = "Description",
                Owner = m_Owner,
                Creator = m_Creator,
                ID = m_UEI,
                LogoID = m_InsigniaID,
                Group = m_Group,
                Maturity = RegionAccess.Mature,
                Marketplace = "Market",
                Properties = ExperiencePropertyFlags.Grid,
                SlUrl = "http://slurl.com/"
            };
            m_Log.Info("Creating experience");
            m_ExperienceService.Add(gInfo);
            testGInfo = gInfo;

            m_Log.Info("Checking for experience existence 1");
            gInfo = m_ExperienceService[m_ExperienceID];

            if (!CheckForEquality(gInfo, testGInfo))
            {
                return false;
            }

            m_Log.Info("Checking for experience existence 2");
            if (!m_ExperienceService.TryGetValue(m_ExperienceID, out gInfo))
            {
                return false;
            }
            if (!CheckForEquality(gInfo, testGInfo))
            {
                return false;
            }

            m_Log.Info("Checking for experience existence 3");
            if (!m_ExperienceService.TryGetValue(m_UEI, out gInfo))
            {
                return false;
            }
            if (!CheckForEquality(gInfo, testGInfo))
            {
                return false;
            }

            try
            {
                m_Log.Info("Delete experience");
                m_ExperienceService.Remove(m_Owner, m_UEI);
            }
            catch (NotSupportedException)
            {
                return true;
            }

            m_Log.Info("Checking for experience non-existence 1");
            try
            {
                gInfo = m_ExperienceService[m_UEI];
                return false;
            }
            catch (KeyNotFoundException)
            {
                /* intentionally ignored */
            }

            m_Log.Info("Checking for experience non-existence 2");
            if (m_ExperienceService.TryGetValue(m_ExperienceID, out gInfo))
            {
                return false;
            }

            m_Log.Info("Checking for experience non-existence 3");
            if (m_ExperienceService.TryGetValue(m_UEI, out gInfo))
            {
                return false;
            }

            return true;
        }

        public void Cleanup()
        {
            try
            {
                m_ExperienceService.Remove(m_Owner, m_UEI);
            }
            catch
            {
                /* intentionally ignored */
            }
        }
    }
}
