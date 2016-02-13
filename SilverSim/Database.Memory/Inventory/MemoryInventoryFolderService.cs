﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Threading;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace SilverSim.Database.Memory.Inventory
{
    public class MemoryInventoryFolderService : InventoryFolderServiceInterface
    {
        readonly MemoryInventoryService m_Service;

        public MemoryInventoryFolderService(MemoryInventoryService service)
        {
            m_Service = service;
        }

        public override bool TryGetValue(UUID key, out InventoryFolder folder)
        {
            foreach(RwLockedDictionary<UUID, InventoryFolder> dict in m_Service.m_Folders.Values)
            {
                if(dict.TryGetValue(key, out folder))
                {
                    folder = new InventoryFolder(folder);
                    return true;
                }
            }
            folder = default(InventoryFolder);
            return false;
        }

        public override bool ContainsKey(UUID key)
        {
            foreach (RwLockedDictionary<UUID, InventoryFolder> dict in m_Service.m_Folders.Values)
            {
                if (dict.ContainsKey(key))
                {
                    return true;
                }
            }
            return false;
        }

        public override InventoryFolder this[UUID key]
        {
            get
            {
                InventoryFolder folder;
                if(!TryGetValue(key, out folder))
                {
                    throw new InventoryFolderNotFoundException(key);
                }
                return folder;
            }
        }

        public override bool TryGetValue(UUID principalID, UUID key, out InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if(m_Service.m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(key, out folder))
            {
                folder = new InventoryFolder(folder);
                return true;
            }

            folder = default(InventoryFolder);
            return false;
        }

        public override bool ContainsKey(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Service.m_Folders.TryGetValue(principalID, out folderSet) && folderSet.ContainsKey(key);
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override InventoryFolder this[UUID principalID, UUID key]
        {
            get 
            {
                InventoryFolder folder;
                if(!TryGetValue(principalID, key, out folder))
                {
                    throw new InventoryFolderNotFoundException(key);
                }
                return folder;
            }
        }

        public override bool ContainsKey(UUID principalID, AssetType type)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if(m_Service.m_Folders.TryGetValue(principalID, out folderSet))
            {
                if(type == AssetType.RootFolder)
                {
                    foreach(InventoryFolder folder in folderSet.Values)
                    {
                        if(folder.ParentFolderID == UUID.Zero)
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    InventoryType invtype = (InventoryType)type;

                    foreach(InventoryFolder folder in folderSet.Values)
                    {
                        if(folder.InventoryType == invtype)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public override bool TryGetValue(UUID principalID, AssetType type, out InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            if (m_Service.m_Folders.TryGetValue(principalID, out folderSet))
            {
                if (type == AssetType.RootFolder)
                {
                    foreach (InventoryFolder sfolder in folderSet.Values)
                    {
                        if (sfolder.ParentFolderID == UUID.Zero)
                        {
                            folder = new InventoryFolder(sfolder);
                            return true;
                        }
                    }
                }
                else
                {
                    InventoryType invtype = (InventoryType)type;

                    foreach (InventoryFolder sfolder in folderSet.Values)
                    {
                        if (sfolder.InventoryType == invtype)
                        {
                            folder = new InventoryFolder(sfolder);
                            return true;
                        }
                    }
                }
            }

            folder = default(InventoryFolder);
            return false;
        }

        [SuppressMessage("Gendarme.Rules.Design", "AvoidMultidimensionalIndexerRule")]
        public override InventoryFolder this[UUID principalID, AssetType type]
        {
            get 
            {
                InventoryFolder folder;
                if(TryGetValue(principalID, type, out folder))
                {
                    return folder;
                }

                throw new InventoryFolderTypeNotFoundException(type);
            }
        }

        public override List<InventoryFolder> GetFolders(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Service.m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<InventoryFolder>(from folder in folderSet.Values where folder.ParentFolderID == key select new InventoryFolder(folder)) :
                new List<InventoryFolder>();
        }

        public override List<InventoryFolder> GetInventorySkeleton(UUID principalID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Service.m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<InventoryFolder>(from folder in folderSet.Values where true select new InventoryFolder(folder)) :
                new List<InventoryFolder>();
        }

        public override List<InventoryItem> GetItems(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryItem> itemSet;
            return m_Service.m_Items.TryGetValue(principalID, out itemSet) ?
                new List<InventoryItem>(from item in itemSet.Values where item.ParentFolderID == key select new InventoryItem(item)) :
                new List<InventoryItem>();
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void Add(InventoryFolder folder)
        {
            m_Service.m_Folders[folder.Owner.ID].Add(folder.ID, new InventoryFolder(folder));

            if (folder.ParentFolderID != UUID.Zero)
            {
                IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
            }
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public override void Update(InventoryFolder folder)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder internFolder;
            if(m_Service.m_Folders.TryGetValue(folder.Owner.ID, out folderSet) &&
                folderSet.TryGetValue(folder.ID, out internFolder))
            {
                lock(internFolder)
                {
                    internFolder.Version = folder.Version;
                    internFolder.Name = folder.Name;
                }
                IncrementVersionNoExcept(folder.Owner.ID, folder.ParentFolderID);
            }
        }

        public override void Move(UUID principalID, UUID folderID, UUID toFolderID)
        {
            if (folderID == toFolderID)
            {
                throw new ArgumentException("folderID != toFolderID");
            }
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder internFolder;
            if (m_Service.m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out internFolder))
            {
                UUID oldFolderID = internFolder.ParentFolderID;
                lock (internFolder)
                {
                    internFolder.ParentFolderID = toFolderID;
                }
                IncrementVersionNoExcept(principalID, toFolderID);
                IncrementVersionNoExcept(principalID, oldFolderID);
            }
            throw new InventoryFolderNotStoredException(folderID);
        }

        #region Delete and Purge
        public override void Delete(UUID principalID, UUID folderID)
        {
            PurgeOrDelete(principalID, folderID, true);
        }

        public override void Purge(UUID principalID, UUID folderID)
        {
            PurgeOrDelete(principalID, folderID, false);
        }

        public override void Purge(UUID folderID)
        {
            InventoryFolder folder = this[folderID];
            Purge(folder.Owner.ID, folderID);
        }

        [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        void PurgeOrDelete(UUID principalID, UUID folderID, bool deleteFolder)
        {
            List<UUID> folders;
            InventoryFolder thisfolder = this[principalID, folderID];

            if (deleteFolder)
            {
                folders = new List<UUID>();
                folders.Add(folderID);
            }
            else
            {
                folders = GetFolderIDs(principalID, folderID);
            }

            for (int index = 0; index < folders.Count; ++index)
            {
                foreach (UUID folder in GetFolderIDs(principalID, folders[index]))
                {
                    if (!folders.Contains(folder))
                    {
                        folders.Insert(0, folder);
                    }
                }
            }

            RwLockedDictionary<UUID, InventoryItem> itemSet = m_Service.m_Items[principalID];
            foreach(InventoryItem item in itemSet.Values)
            {
                if(folders.Contains(item.ParentFolderID))
                {
                    itemSet.Remove(item.ID);
                }
            }

            foreach (UUID folder in folders)
            {
                m_Service.m_Folders.Remove(folder);
            }

            IncrementVersionNoExcept(principalID, folderID);
        }

        List<UUID> GetFolderIDs(UUID principalID, UUID key)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            return m_Service.m_Folders.TryGetValue(principalID, out folderSet) ?
                new List<UUID>(from folder in folderSet.Values where folder.ParentFolderID == key select folder.ID) :
                new List<UUID>();
        }

        #endregion

        public override void IncrementVersion(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if(m_Service.m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
            else
            {
                throw new InventoryFolderNotStoredException(folderID);
            }
        }

        void IncrementVersionNoExcept(UUID principalID, UUID folderID)
        {
            RwLockedDictionary<UUID, InventoryFolder> folderSet;
            InventoryFolder folder;
            if (m_Service.m_Folders.TryGetValue(principalID, out folderSet) &&
                folderSet.TryGetValue(folderID, out folder))
            {
                Interlocked.Increment(ref folder.Version);
            }
        }
    }
}