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

using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Types;
using SilverSim.Types.Inventory;
using SilverSim.Viewer.Messages;
using System;
using System.Collections.Generic;

namespace SilverSim.Tests.Viewer.UDP
{
    public partial class LLUDPInventoryClient : InventoryServiceInterface
    {
        private readonly ViewerCircuit m_ViewerCircuit;
        private readonly UUID m_RootFolderID;

        public LLUDPInventoryClient(ViewerCircuit viewerCircuit, UUID rootFolderID)
        {
            m_RootFolderID = rootFolderID;
            m_ViewerCircuit = viewerCircuit;
            m_ViewerCircuit.MessageRouting.Add(MessageType.BulkUpdateInventory, MessageHandler);
            m_ViewerCircuit.MessageRouting.Add(MessageType.FetchInventoryReply, MessageHandler);
            m_ViewerCircuit.MessageRouting.Add(MessageType.InventoryDescendents, MessageHandler);
            m_ViewerCircuit.MessageRouting.Add(MessageType.UpdateCreateInventoryItem, HandleUpdateCreateInventoryItem);
            m_ViewerCircuit.MessageRouting.Add(MessageType.UpdateInventoryFolder, HandleUpdateInventoryFolder);
            m_ViewerCircuit.MessageRouting.Add(MessageType.UpdateInventoryItem, HandleUpdateInventoryItem);
        }

        private void MessageHandler(Message m)
        {

        }

        public override IInventoryFolderServiceInterface Folder => this;

        public override IInventoryItemServiceInterface Item => this;

        public override List<InventoryItem> GetActiveGestures(UUID principalID)
        {
            throw new NotSupportedException();
        }

        public override void Remove(UUID scopeID, UUID accountID)
        {
            throw new NotSupportedException();
        }
    }
}
