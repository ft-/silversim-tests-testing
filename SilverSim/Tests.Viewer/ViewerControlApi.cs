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
using SilverSim.Main.Common.Caps;
using SilverSim.Main.Common.CmdIO;
using SilverSim.Scene.Management.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl;
using SilverSim.ServiceInterfaces.Account;
using SilverSim.ServiceInterfaces.Asset;
using SilverSim.ServiceInterfaces.Friends;
using SilverSim.ServiceInterfaces.Grid;
using SilverSim.ServiceInterfaces.GridUser;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.ServiceInterfaces.PortControl;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.ServiceInterfaces.Profile;
using SilverSim.ServiceInterfaces.UserAgents;
using SilverSim.Tests.Viewer.UDP;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.ServerURIs;
using SilverSim.Viewer.Core;
using System;
using System.Collections.Generic;

namespace SilverSim.Tests.Viewer
{
    [LSLImplementation]
    [ScriptApiName("ViewerControl")]
    public partial class ViewerControlApi : IScriptApi, IPlugin, IPluginShutdown
    {
        static readonly ILog m_Log = LogManager.GetLogger("VIEWER CONTROL");

        RwLockedDictionary<uint, ViewerCircuit> m_ViewerCircuits = new RwLockedDictionary<uint, ViewerCircuit>();

        UDPCircuitsManager m_ClientUDP;

        readonly string m_AgentInventoryServiceName;
        readonly string m_AgentAssetServiceName;
        readonly string m_AgentProfileServiceName;
        readonly string m_AgentFriendsServiceName;
        readonly string m_PresenceServiceName;
        readonly string m_GridUserServiceName;
        readonly string m_GridServiceName;
        readonly string m_OfflineIMServiceName;
        readonly string m_UserAccountServiceName;

        InventoryServiceInterface m_AgentInventoryService;
        AssetServiceInterface m_AgentAssetService;
        ProfileServiceInterface m_AgentProfileService;
        FriendsServiceInterface m_AgentFriendsService;
        UserAgentServiceInterface m_AgentUserAgentService;
        PresenceServiceInterface m_PresenceService;
        GridUserServiceInterface m_GridUserService;
        GridServiceInterface m_GridService;
        OfflineIMServiceInterface m_OfflineIMService;
        UserAccountServiceInterface m_UserAccountService;
        SceneList m_Scenes;
        CommandRegistry m_Commands;
        CapsHttpRedirector m_CapsRedirector;
        List<IProtocolExtender> m_PacketHandlerPlugins = new List<IProtocolExtender>();

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.LogoutRegion;
            }
        }

        public void Shutdown()
        {
            foreach(ViewerCircuit circuit in m_ViewerCircuits.Values)
            {
                m_ClientUDP.RemoveCircuit(circuit);
            }
            m_ClientUDP.Shutdown();
        }

        public ViewerControlApi(IConfig ownSection)
        {
            m_AgentInventoryServiceName = ownSection.GetString("InventoryService");
            m_AgentAssetServiceName = ownSection.GetString("AssetService");
            m_AgentProfileServiceName = ownSection.GetString("ProfileService", string.Empty);
            m_AgentFriendsServiceName = ownSection.GetString("FriendsService");
            m_PresenceServiceName = ownSection.GetString("PresenceService");
            m_GridUserServiceName = ownSection.GetString("GridUserService");
            m_GridServiceName = ownSection.GetString("GridService");
            m_OfflineIMServiceName = ownSection.GetString("OfflineIMService", string.Empty);
            m_UserAccountServiceName = ownSection.GetString("UserAccountService");
        }

        sealed class LocalUserAgentService : UserAgentServiceInterface, IDisplayNameAccessor
        {
            readonly PresenceServiceInterface m_PresenceService;
            readonly GridUserServiceInterface m_GridUserService;
            readonly UserAccountServiceInterface m_UserAccountService;

            public LocalUserAgentService(
                PresenceServiceInterface presenceService, 
                GridUserServiceInterface gridUserService,
                UserAccountServiceInterface userAccountService)
            {
                m_PresenceService = presenceService;
                m_GridUserService = gridUserService;
                m_UserAccountService = userAccountService;
            }

            bool IDisplayNameAccessor.TryGetValue(UUI agent, out string displayname)
            {
                displayname = string.Empty;
                return false;
            }

            bool IDisplayNameAccessor.ContainsKey(UUI agent)
            {
                return false;
            }
            string IDisplayNameAccessor.this[UUI agent]
            {
                get
                {
                    throw new KeyNotFoundException();
                }

                set
                {
                    throw new NotSupportedException();
                }
            }


            public override IDisplayNameAccessor DisplayName
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override DestinationInfo GetHomeRegion(UUI user)
            {
                throw new NotImplementedException();
            }

            public override ServerURIs GetServerURLs(UUI user)
            {
                throw new KeyNotFoundException();
            }

            public override UserInfo GetUserInfo(UUI user)
            {
                UserAccount account;
                if (!m_UserAccountService.TryGetValue(UUID.Zero, user.ID, out account))
                {
                    throw new KeyNotFoundException();
                }
                UserInfo info = new UserInfo();
                info.FirstName = account.Principal.FirstName;
                info.LastName = account.Principal.LastName;
                info.UserCreated = account.Created;
                info.UserFlags = account.UserFlags;
                info.UserTitle = account.UserTitle;
                return info;
            }

            public override UUI GetUUI(UUI user, UUI targetUserID)
            {
                UserAccount account;
                if(!m_UserAccountService.TryGetValue(UUID.Zero, targetUserID.ID, out account))
                {
                    throw new KeyNotFoundException();
                }
                return account.Principal;
            }

            public override bool IsOnline(UUI user)
            {
                return m_PresenceService[user.ID].Count != 0;
            }

            public override string LocateUser(UUI user)
            {
                throw new KeyNotFoundException();
            }

            public override List<UUID> NotifyStatus(List<KeyValuePair<UUI, string>> friends, UUI user, bool online)
            {
                return new List<UUID>();
            }

            public override void VerifyAgent(UUID sessionID, string token)
            {
                /* intentionally not implemented */
            }

            public override void VerifyClient(UUID sessionID, string token)
            {
                /* intentionally not implemented */
            }
        }

        public void Startup(ConfigurationLoader loader)
        {
            m_AgentInventoryService = loader.GetService<InventoryServiceInterface>(m_AgentInventoryServiceName);
            m_AgentAssetService = loader.GetService<AssetServiceInterface>(m_AgentAssetServiceName);
            if (!string.IsNullOrEmpty(m_AgentProfileServiceName))
            {
                m_AgentProfileService = loader.GetService<ProfileServiceInterface>(m_AgentProfileServiceName);
            }
            m_AgentFriendsService = loader.GetService<FriendsServiceInterface>(m_AgentFriendsServiceName);
            m_PresenceService = loader.GetService<PresenceServiceInterface>(m_PresenceServiceName);
            m_GridUserService = loader.GetService<GridUserServiceInterface>(m_GridUserServiceName);
            m_GridService = loader.GetService<GridServiceInterface>(m_GridServiceName);
            if (!string.IsNullOrEmpty(m_OfflineIMServiceName))
            {
                m_OfflineIMService = loader.GetService<OfflineIMServiceInterface>(m_OfflineIMServiceName);
            }
            m_UserAccountService = loader.GetService<UserAccountServiceInterface>(m_UserAccountServiceName);
            m_AgentUserAgentService = new LocalUserAgentService(m_PresenceService, m_GridUserService, m_UserAccountService);

            m_Scenes = loader.Scenes;
            m_Commands = loader.CommandRegistry;
            m_CapsRedirector = loader.CapsRedirector;
            m_PacketHandlerPlugins = loader.GetServicesByValue<IProtocolExtender>();

            m_ClientUDP = new UDPCircuitsManager(new System.Net.IPAddress(0), 0, null, null, null, new List<IPortControlServiceInterface>());
        }
    }

    [PluginName("ViewerControl")]
    public class ViewerControlApiFactory : IPluginFactory
    {
        public ViewerControlApiFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new ViewerControlApi(ownSection);
        }
    }
}