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

using SilverSim.Scene.Types.Agent;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scripting.Lsl;
using SilverSim.ServiceInterfaces.Presence;
using SilverSim.Tests.Viewer.UDP;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Account;
using SilverSim.Types.Grid;
using SilverSim.Types.Presence;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.Circuit;
using SilverSim.Viewer.Messages.Telehub;
using SilverSim.Viewer.Messages.Teleport;
using System;
using System.Net;
using System.Net.Sockets;

namespace SilverSim.Tests.Viewer
{
    public partial class ViewerControlApi
    {
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_SETHOMETOTARGET = 1;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_SETLASTTOTARGET = 2;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIALURE = 4;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIALANDMARK = 8;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIALOCATION = 16;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIAHOME = 32;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIATELEHUB = 64;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIALOGIN = 128;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIAGODLIKELURE = 256;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_GODLIKE = 512;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_NINEONEONE = 1024;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_DISABLECANCEL = 2048;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIAREGIONID = 4096;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_ISFLYING = 8192;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_RESETHOME = 16384;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_FORCEREDIRECT = 32768;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_FINISHEDVIALURE = 67108864;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_FINISHEDVIANEWSIM = 268435456;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_FINISHEDVIASAMESIM = 536870912;
        [APIExtension("ViewerControl")]
        public const int TELEPORT_FLAGS_VIAHGLOGIN = 1073741824;

        [APIExtension("ViewerControl", "vcCreateAccount")]
        public LSLKey CreateAccount(
            ScriptInstance instance,
            string firstName,
            string lastName)
        {
            lock(instance)
            {
                var acc = new UserAccount()
                {
                    Principal = new UUI { ID = UUID.Random, FirstName = firstName, LastName = lastName }
                };
                m_UserAccountService.Add(acc);
                return acc.Principal.ID;
            }
        }

        [APIExtension("ViewerControl", "vcSetAgentLimit")]
        public void SetAgentLimit(
            ScriptInstance instance,
            int agentlimit)
        {
            lock(instance)
            {
                SceneInterface scene = instance.Part.ObjectGroup.Scene;
                scene.RegionSettings.AgentLimit = agentlimit;
                scene.TriggerRegionSettingsChanged();
            }
        }

        [APIExtension("ViewerControl", "vcLoginAgent")]
        public string LoginAgent(
            ScriptInstance instance,
            int circuitCode,
            LSLKey regionId,
            LSLKey agentId,
            LSLKey sessionId,
            LSLKey secureSessionId,
            string serviceSessionId,
            string viewerChannel,
            string viewerVersion,
            string id0,
            string mac,
            int teleportFlags,
            Vector3 position,
            Vector3 lookAt)
        {
            lock (instance)
            {
                string externalHostName = m_CapsRedirector.ExternalHostName;
                IPAddress[] addresses = DnsNameCache.GetHostAddresses(externalHostName);

                string clientIP = string.Empty;

                if (addresses.Length == 0)
                {
                    m_Log.InfoFormat("ExternalHostName \"{0}\" does not resolve", externalHostName);
                    return string.Empty;
                }

                foreach (IPAddress addr in addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        clientIP = addr.ToString();
                    }
                }

                if (string.IsNullOrEmpty(clientIP))
                {
                    m_Log.InfoFormat("ExternalHostName \"{0}\" does not resolve", externalHostName);
                    return string.Empty;
                }

                UUID capsId = UUID.Random;
                string capsPath = m_CapsRedirector.ServerURI + "CAPS/" + capsId.ToString() + "0000/";
                SceneInterface scene;
                if (!m_Scenes.TryGetValue(regionId, out scene))
                {
                    return string.Empty;
                }
                var clientInfo = new ClientInfo()
                {
                    ClientIP = clientIP,
                    Channel = viewerChannel,
                    ClientVersion = viewerVersion,
                    ID0 = id0,
                    Mac = mac
                };
                UserAccount userAccount;
                if (!m_UserAccountService.TryGetValue(UUID.Zero, agentId, out userAccount))
                {
                    m_Log.InfoFormat("User account {0} does not exist", agentId.ToString());
                    return string.Empty;
                }

                var presenceInfo = new PresenceInfo()
                {
                    RegionID = regionId,
                    SecureSessionID = secureSessionId,
                    SessionID = sessionId,
                    UserID = userAccount.Principal
                };
                var serviceList = new AgentServiceList
                {
                    m_AgentAssetService,
                    m_AgentInventoryService,
                    m_AgentFriendsService,
                    m_AgentUserAgentService,
                    m_PresenceService,
                    m_GridUserService,
                    m_GridService
                };
                if (m_AgentProfileService != null)
                {
                    serviceList.Add(m_AgentProfileService);
                }
                if (m_OfflineIMService != null)
                {
                    serviceList.Add(m_OfflineIMService);
                }

                var agent = new ViewerAgent(
                    m_Scenes,
                    agentId,
                    userAccount.Principal.FirstName,
                    userAccount.Principal.LastName,
                    userAccount.Principal.HomeURI,
                    sessionId.AsUUID,
                    secureSessionId.AsUUID,
                    serviceSessionId,
                    clientInfo,
                    userAccount,
                    serviceList);

                try
                {
                    scene.DetermineInitialAgentLocation(agent, (TeleportFlags)teleportFlags, position, lookAt);
                }
                catch (Exception e)
                {
                    m_Log.InfoFormat("Failed to determine initial location for agent {0}: {1}: {2}", userAccount.Principal.FullName, e.GetType().FullName, e.Message);
                    return string.Empty;
                }

                var udpServer = (UDPCircuitsManager)scene.UDPServer;

                IPAddress ipAddr;
                if (!IPAddress.TryParse(clientInfo.ClientIP, out ipAddr))
                {
                    m_Log.InfoFormat("Invalid IP address for agent {0}", userAccount.Principal.FullName);
                    return string.Empty;
                }

                ViewerConnection vc = AddAgent(instance, userAccount.Principal.ID);

                var ep = new IPEndPoint(ipAddr, vc.ClientUDP.LocalPort);
                var regionEndPoint = new IPEndPoint(ipAddr, (int)scene.RegionPort);
                var circuit = new AgentCircuit(
                    m_Commands,
                    agent,
                    udpServer,
                    (uint)circuitCode,
                    m_CapsRedirector,
                    capsId,
                    agent.ServiceURLs,
                    string.Empty,
                    m_PacketHandlerPlugins,
                    ep)
                {
                    LastTeleportFlags = (TeleportFlags)teleportFlags,
                    Agent = agent,
                    AgentID = userAccount.Principal.ID,
                    SessionID = sessionId.AsUUID,
                    ForceUseCircuitCode = true
                };
                agent.Circuits.Add(circuit.Scene.ID, circuit);

                try
                {
                    scene.Add(agent);
                    try
                    {
                        udpServer.AddCircuit(circuit);
                    }
                    catch
                    {
                        scene.Remove(agent);
                        throw;
                    }
                }
                catch (Exception e)
                {
                    m_Log.Debug("Failed agent post", e);
                    agent.Circuits.Clear();
                    return string.Empty;
                }
                /* make agent a root agent */
                agent.SceneID = scene.ID;
                if (null != m_GridUserService)
                {
                    try
                    {
                        m_GridUserService.SetPosition(agent.Owner, scene.ID, agent.GlobalPosition, agent.LookAt);
                    }
                    catch (Exception e)
                    {
                        m_Log.Warn("Could not contact GridUserService", e);
                    }
                }

                try
                {
                    m_PresenceService.Report(new PresenceInfo()
                    {
                        UserID = agent.Owner,
                        SessionID = agent.SessionID,
                        SecureSessionID = secureSessionId.AsUUID,
                        RegionID = scene.ID
                    });
                }
                catch (Exception e)
                {
                    m_Log.Warn("Could not contact PresenceService", e);
                }
                circuit.LogIncomingAgent(m_Log, false);
                var useCircuit = new UseCircuitCode()
                {
                    AgentID = agentId,
                    SessionID = sessionId.AsUUID,
                    CircuitCode = (uint)circuitCode
                };

                var viewerCircuit = new ViewerCircuit(vc.ClientUDP, (uint)circuitCode, sessionId.AsUUID, agentId, regionEndPoint);
                vc.ClientUDP.AddCircuit(viewerCircuit);
                viewerCircuit.Start();
                viewerCircuit.MessageRouting.Add(MessageType.RegionHandshake, vc.HandleRegionHandshake);
                viewerCircuit.SendMessage(useCircuit);
                viewerCircuit.MessageRouting.Add(MessageType.LogoutReply, (Message m) => HandleLogoutReply((uint)circuitCode, vc));
                viewerCircuit.MessageRouting.Add(MessageType.TelehubInfo, (Message m) => HandleTelehubInfo((TelehubInfo)m, vc));
                viewerCircuit.MessageRouting.Add(MessageType.TeleportLocal, (Message m) => HandleTeleportLocal((TeleportLocal)m, vc));
                viewerCircuit.MessageRouting.Add(MessageType.EconomyData, (Message m) => vc.PostEvent(new EconomyDataReceivedEvent { AgentID = m.CircuitAgentID, RegionID = m.CircuitSceneID }));
                viewerCircuit.MessageRouting.Add(MessageType.TeleportProgress, (Message m) => HandleTeleportProgress(m, vc));
                viewerCircuit.MessageRouting.Add(MessageType.TeleportStart, (Message m) => HandleTeleportStart(m, vc));
                viewerCircuit.MessageRouting.Add(MessageType.TeleportFailed, (Message m) => HandleTeleportFailed(m, vc));
                vc.ViewerCircuits.Add((uint)circuitCode, viewerCircuit);
                return capsPath;
            }
        }

        private void HandleTeleportFailed(Message m, ViewerConnection vc)
        {
            var res = (TeleportFailed)m;
            var ev = new TeleportFailedReceivedEvent
            {
                AgentID = res.CircuitAgentID,
                RegionID = res.CircuitSceneID,
                Reason = res.Reason
            };

            foreach(TeleportFailed.AlertInfoEntry e in res.AlertInfo)
            {
                ev.AlertInfo.Add(e.Message);
                ev.AlertInfo.Add(e.ExtraParams);
            }
            vc.PostEvent(ev);
        }

        private void HandleTeleportStart(Message m, ViewerConnection vc)
        {
            var res = (TeleportStart)m;
            vc.PostEvent(new TeleportStartReceivedEvent
            {
                AgentID = res.CircuitAgentID,
                RegionID = res.CircuitSceneID,
                TeleportFlags = (int)res.TeleportFlags
            });
        }

        private void HandleTeleportProgress(Message m, ViewerConnection vc)
        {
            var res = (TeleportProgress)m;
            vc.PostEvent(new TeleportProgressReceivedEvent
            {
                AgentID = res.CircuitAgentID,
                RegionID = res.CircuitSceneID,
                TeleportFlags = (int)res.TeleportFlags,
                Message = res.Message
            });
        }

        private void HandleLogoutReply(uint circuitCode, ViewerConnection vc)
        {
            ViewerCircuit removeCircuit;
            if (vc.ViewerCircuits.TryGetValue(circuitCode, out removeCircuit))
            {
                vc.PostEvent(new LogoutReplyReceivedEvent(removeCircuit.AgentID, removeCircuit.RegionData.RegionID, (int)removeCircuit.CircuitCode));
                removeCircuit.Stop();
                vc.ClientUDP.RemoveCircuit(removeCircuit);
            }
        }

        private void HandleTelehubInfo(TelehubInfo m, ViewerConnection vc)
        {
            var ev = new TelehubInfoReceivedEvent
            {
                AgentID = m.CircuitAgentID,
                RegionID = m.CircuitSceneID,
                ObjectID = m.ObjectID,
                ObjectName = m.ObjectName,
                TelehubPos = m.TelehubPos,
                TelehubRot = m.TelehubRot
            };
            foreach (Vector3 v in m.SpawnPoints)
            {
                ev.SpawnPointPos.Add(v);
            }
            vc.PostEvent(ev);
        }

        private void HandleTeleportLocal(TeleportLocal m, ViewerConnection vc)
        {
            vc.PostEvent(new TeleportLocalReceivedEvent
            {
                AgentID = m.CircuitAgentID,
                RegionID = m.CircuitSceneID,
                Position = m.Position,
                LookAt = m.LookAt,
                TeleportFlags = (int)m.TeleportFlags
            });
        }

        [APIExtension("ViewerControl", "vcLogoutAgent")]
        public void LogoutAgent(ScriptInstance instance, LSLKey agentId, int circuitCode)
        {
            lock (instance)
            {
                ViewerConnection vc;
                ViewerCircuit viewerCircuit;
                if (m_Clients.TryGetValue(agentId.AsUUID, out vc) &&
                    vc.ViewerCircuits.TryGetValue((uint)circuitCode, out viewerCircuit))
                {
                    var logoutreq = new LogoutRequest()
                    {
                        AgentID = viewerCircuit.AgentID,
                        SessionID = viewerCircuit.SessionID
                    };
                    viewerCircuit.SendMessage(logoutreq);
                }
            }
        }
    }
}
