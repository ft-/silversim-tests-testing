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
using SilverSim.Scene.Types.SceneEnvironment;
using SilverSim.Types;
using System.Reflection;

namespace SilverSim.Tests.SimulationData
{
    public sealed class LightShareTests : CommonSimDataTest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override bool Run()
        {
            EnvironmentController.WindlightSkyData skyTestData = EnvironmentController.WindlightSkyData.Defaults;
            EnvironmentController.WindlightWaterData waterTestData = EnvironmentController.WindlightWaterData.Defaults;
            UUID regionID = new UUID("11223344-1122-1122-1122-112233445566");
            EnvironmentController.WindlightSkyData skyRetrieveData;
            EnvironmentController.WindlightWaterData waterRetrieveData;

            m_Log.Info("Testing non-existence of LightShare data");
            if (SimulationData.LightShare.TryGetValue(regionID, out skyRetrieveData, out waterRetrieveData))
            {
                return false;
            }

            m_Log.Info("Storing LightShare data");
            SimulationData.LightShare.Store(regionID, skyTestData, waterTestData);

            m_Log.Info("Testing existence of LightShare data");
            if (!SimulationData.LightShare.TryGetValue(regionID, out skyRetrieveData, out waterRetrieveData))
            {
                return false;
            }

            m_Log.Info("Removing LightShare data via Remove");
            if(!SimulationData.LightShare.Remove(regionID))
            {
                return false;
            }

            m_Log.Info("Testing non-existence of LightShare data");
            if (SimulationData.LightShare.TryGetValue(regionID, out skyRetrieveData, out waterRetrieveData))
            {
                return false;
            }
            m_Log.Info("Storing LightShare data");
            SimulationData.LightShare.Store(regionID, skyTestData, waterTestData);

            m_Log.Info("Testing existence of LightShare data");
            if (!SimulationData.LightShare.TryGetValue(regionID, out skyRetrieveData, out waterRetrieveData))
            {
                return false;
            }

            m_Log.Info("Removing LightShare data via RemoveRegion");
            SimulationData.RemoveRegion(regionID);

            m_Log.Info("Testing non-existence of LightShare data");
            if (SimulationData.LightShare.TryGetValue(regionID, out skyRetrieveData, out waterRetrieveData))
            {
                return false;
            }

            return true;
        }
    }
}
