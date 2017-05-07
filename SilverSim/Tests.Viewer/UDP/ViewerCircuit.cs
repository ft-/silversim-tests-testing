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
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.IM;
using SilverSim.Viewer.Core;
using SilverSim.Viewer.Messages;
using SilverSim.Viewer.Messages.IM;
using System;
using System.Collections.Generic;
using System.Net;

namespace SilverSim.Tests.Viewer.UDP
{
    public class ViewerCircuit : Circuit
    {
        public UUID SessionID { get; protected set; }
        public UUID AgentID { get; protected set; }

        private static readonly ILog m_Log = LogManager.GetLogger("VIEWER CIRCUIT");
        private static readonly UDPPacketDecoder m_PacketDecoder = new UDPPacketDecoder(true);
        public readonly BlockingQueue<Message> ReceiveQueue = new BlockingQueue<Message>();
        public bool EnableReceiveQueue = false;

        readonly Dictionary<MessageType, Action<Message>> m_MessageRouting = new Dictionary<MessageType, Action<Message>>();
        readonly Dictionary<string, Action<Message>> m_GenericMessageRouting = new Dictionary<string, Action<Message>>();
        readonly Dictionary<string, Action<Message>> m_GodlikeMessageRouting = new Dictionary<string, Action<Message>>();
        readonly Dictionary<GridInstantMessageDialog, Action<Message>> m_IMMessageRouting = new Dictionary<GridInstantMessageDialog, Action<Message>>();

        public ViewerCircuit(
            UDPCircuitsManager server,
            UInt32 circuitcode,
            UUID sessionID,
            UUID agentID,
            EndPoint remoteEndPoint)
            : base(server, circuitcode)
        {
            RemoteEndPoint = remoteEndPoint;
            SessionID = sessionID;
            AgentID = agentID;
        }

        protected override void CheckForNewDataToSend()
        {
        }

        protected override void LogMsgLogoutReply()
        {
        }

        protected override void LogMsgOnLogoutCompletion()
        {
        }

        protected override void LogMsgOnTimeout()
        {
        }

        public Dictionary<string, Action<Message>> GenericMessageRouting
        {
            get
            {
                return m_GenericMessageRouting;
            }
        }

        public Dictionary<SilverSim.Types.IM.GridInstantMessageDialog, Action<Message>> IMMessageRouting
        {
            get
            {
                return m_IMMessageRouting;
            }
        }

        public Dictionary<MessageType, Action<Message>> MessageRouting
        {
            get
            {
                return m_MessageRouting;
            }
        }

        public Message Receive(int timeout)
        {
            if (!EnableReceiveQueue)
            {
                throw new Exception("Receive queue not enabled");
            }
            return ReceiveQueue.Dequeue(timeout);
        }

        public Message Receive()
        {
            if(!EnableReceiveQueue)
            {
                throw new Exception("Receive queue not enabled");
            }
            return ReceiveQueue.Dequeue();
        }

        protected override void OnCircuitSpecificPacketReceived(MessageType mType, UDPPacket pck)
        {

            Func<UDPPacket, Message> del;
            if (m_PacketDecoder.PacketTypes.TryGetValue(mType, out del))
            {
                Message m = del(pck);
                /* we got a decoder, so we can make use of it */
                m.CircuitAgentID = AgentID;
                try
                {
                    m.CircuitAgentOwner = UUI.Unknown;
                    m.CircuitSessionID = SessionID;
                    m.CircuitSceneID = UUID.Zero;
                }
                catch
                {
                    /* this is a specific error that happens only during logout */
                    return;
                }

                /* we keep the circuit relatively dumb so that we have no other logic than how to send and receive messages to the remote sim.
                    * It merely collects delegates to other objects as well to call specific functions.
                    */
                Action<Message> mdel;
                if (m_MessageRouting.TryGetValue(m.Number, out mdel))
                {
                    mdel(m);
                }
                else if (m.Number == MessageType.ImprovedInstantMessage)
                {
                    ImprovedInstantMessage im = (ImprovedInstantMessage)m;
                    if (im.CircuitAgentID != im.AgentID ||
                        im.CircuitSessionID != im.SessionID)
                    {
                        return;
                    }
                    if (m_IMMessageRouting.TryGetValue(im.Dialog, out mdel))
                    {
                        mdel(m);
                    }
                    else if (EnableReceiveQueue)
                    {
                        ReceiveQueue.Enqueue(m);
                    }
                }
                else if (m.Number == MessageType.GenericMessage)
                {
                    SilverSim.Viewer.Messages.Generic.GenericMessage genMsg = (SilverSim.Viewer.Messages.Generic.GenericMessage)m;
                    if (m_GenericMessageRouting.TryGetValue(genMsg.Method, out mdel))
                    {
                        mdel(m);
                    }
                    else if (EnableReceiveQueue)
                    {
                        ReceiveQueue.Enqueue(m);
                    }
                }
                else if (m.Number == MessageType.GodlikeMessage)
                {
                    SilverSim.Viewer.Messages.Generic.GodlikeMessage genMsg = (SilverSim.Viewer.Messages.Generic.GodlikeMessage)m;
                    if (m_GodlikeMessageRouting.TryGetValue(genMsg.Method, out mdel))
                    {
                        mdel(m);
                    }
                    else if (EnableReceiveQueue)
                    {
                        ReceiveQueue.Enqueue(m);
                    }
                }
                else if (EnableReceiveQueue)
                {
                    ReceiveQueue.Enqueue(m);
                }
            }
            else
            {
                /* Ignore we have no decoder for that */
            }
        }

        protected override void SendSimStats(int dt)
        {
        }

        protected override void SendViaEventQueueGet(Message m)
        {
        }

        protected override void StartSpecificThreads()
        {
        }

        protected override void StopSpecificThreads()
        {
        }
    }
}