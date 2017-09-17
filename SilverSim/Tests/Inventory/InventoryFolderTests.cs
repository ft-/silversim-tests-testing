﻿using log4net;
using Nini.Config;
using SilverSim.Main.Common;
using SilverSim.ServiceInterfaces.Inventory;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SilverSim.Tests.Inventory
{
    public class InventoryFolderTests : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        InventoryServiceInterface m_InventoryService;
        InventoryServiceInterface m_BackendInventoryService;
        UUI m_UserID;

        public void Startup(ConfigurationLoader loader)
        {
            IConfig config = loader.Config.Configs[GetType().FullName];
            string inventoryServiceName = config.GetString("InventoryService");
            m_InventoryService = loader.GetService<InventoryServiceInterface>(inventoryServiceName);
            m_BackendInventoryService = loader.GetService<InventoryServiceInterface>(config.GetString("BackendInventoryService", inventoryServiceName));
            m_UserID = new UUI(config.GetString("User"));
        }

        public void Setup()
        {

        }

        public void Cleanup()
        {

        }

        private bool IsDataEqual(InventoryFolder a, InventoryFolder b)
        {
            var mismatches = new List<string>();
            if (a.Name != b.Name)
            {
                mismatches.Add("Name");
            }
            if (a.Owner.ID != b.Owner.ID)
            {
                mismatches.Add("Owner.ID");
            }
            if (a.ParentFolderID != b.ParentFolderID)
            {
                mismatches.Add("ParentFolderID");
            }
            if (a.InventoryType != b.InventoryType)
            {
                mismatches.Add("InventoryType");
            }
            if (a.Version != b.Version)
            {
                mismatches.Add("Version");
            }

            if (mismatches.Count != 0)
            {
                m_Log.InfoFormat("Mismatch at {0}", string.Join(" ", mismatches));
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool Run()
        {
            List<InventoryFolder> result;
            List<InventoryFolderContent> resultContent;
            m_Log.Info("Create User Inventory");
            try
            {
                m_BackendInventoryService.CheckInventory(m_UserID.ID);
            }
            catch
            {
                return false;
            }

            InventoryFolder folder;
            InventoryFolderContent content;

            InventoryFolder rootFolder = m_InventoryService.Folder[m_UserID.ID, AssetType.RootFolder];
            UUID inventoryId = UUID.Random;

            m_Log.InfoFormat("Testing non-existence 1");
            if (m_InventoryService.Folder.ContainsKey(inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 2");
            if (m_InventoryService.Folder.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 3");
            if (m_InventoryService.Folder.TryGetValue(inventoryId, out folder))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 4");
            if (m_InventoryService.Folder.TryGetValue(m_UserID.ID, inventoryId, out folder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 5");
            try
            {
                folder = m_InventoryService.Folder[inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }
            m_Log.InfoFormat("Testing non-existence 6");
            try
            {
                folder = m_InventoryService.Folder[m_UserID.ID, inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }
            m_Log.InfoFormat("Testing non-existence 7");
            result = m_InventoryService.Folder.GetFolders(m_UserID.ID, rootFolder.ID);
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 8");
            result = m_InventoryService.Folder.Content[m_UserID.ID, rootFolder.ID].Folders;
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 9");
            if(m_InventoryService.Folder.Content.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 10");
            if (m_InventoryService.Folder.Content.TryGetValue(m_UserID.ID, inventoryId, out content))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 11");
            try
            {
                content = m_InventoryService.Folder.Content[m_UserID.ID, inventoryId];
                return false;
            }
            catch(InventoryFolderNotFoundException)
            {
                /* this is the expected one */
            }
            m_Log.InfoFormat("Testing non-existence 12");
            resultContent = m_InventoryService.Folder.Content[m_UserID.ID, new UUID[] { inventoryId }];
            foreach(InventoryFolderContent checkItem in resultContent)
            {
                if(checkItem.FolderID == inventoryId)
                {
                    return false;
                }
            }

            var testFolder = new InventoryFolder(inventoryId)
            {
                Name = "Test Name",
                Version = 5,
                ParentFolderID = rootFolder.ID,
                InventoryType = InventoryType.Notecard,
                Owner = m_UserID,
            };
            m_BackendInventoryService.Folder.Add(testFolder);

            m_Log.InfoFormat("Testing existence 1");
            if (!m_InventoryService.Folder.ContainsKey(inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing existence 2");
            if (!m_InventoryService.Folder.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing existence 3");
            if (!m_InventoryService.Folder.TryGetValue(inventoryId, out folder))
            {
                return false;
            }
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }

            m_Log.InfoFormat("Testing existence 4");
            if (!m_InventoryService.Folder.TryGetValue(m_UserID.ID, inventoryId, out folder))
            {
                return false;
            }
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing existence 5");
            folder = m_InventoryService.Folder[inventoryId];
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing existence 6");
            folder = m_InventoryService.Folder[m_UserID.ID, inventoryId];
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }
            folder = null;
            m_Log.InfoFormat("Testing existence 7");
            result = m_InventoryService.Folder.GetFolders(m_UserID.ID, rootFolder.ID);
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    folder = checkItem;
                }
            }
            if (folder == null)
            {
                return false;
            }
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing existence 8");
            result = m_InventoryService.Folder.Content[m_UserID.ID, rootFolder.ID].Folders;
            folder = null;
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    folder = checkItem;
                }
            }
            if (folder == null)
            {
                return false;
            }
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 9");
            if (!m_InventoryService.Folder.Content.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 10");
            if (!m_InventoryService.Folder.Content.TryGetValue(m_UserID.ID, inventoryId, out content))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 11");
            content = m_InventoryService.Folder.Content[m_UserID.ID, inventoryId];
            m_Log.InfoFormat("Testing non-existence 12");
            resultContent = m_InventoryService.Folder.Content[m_UserID.ID, new UUID[] { inventoryId }];
            bool isFound = false;
            foreach (InventoryFolderContent checkItem in resultContent)
            {
                if (checkItem.FolderID == inventoryId)
                {
                    isFound = true;
                }
            }
            if(!isFound)
            {
                return false;
            }

            m_Log.InfoFormat("Updating folder");
            testFolder.Name = "Test Name 2";

            m_InventoryService.Folder.Update(testFolder);

            m_Log.InfoFormat("Testing changes");
            folder = m_InventoryService.Folder[m_UserID.ID, inventoryId];
            if (!IsDataEqual(folder, testFolder))
            {
                return false;
            }

            m_Log.InfoFormat("Deleting folder");
            m_InventoryService.Folder.Delete(m_UserID.ID, inventoryId);

            m_Log.InfoFormat("Testing non-existence 1");
            if (m_InventoryService.Folder.ContainsKey(inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 2");
            if (m_InventoryService.Folder.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 3");
            if (m_InventoryService.Folder.TryGetValue(inventoryId, out folder))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 4");
            if (m_InventoryService.Folder.TryGetValue(m_UserID.ID, inventoryId, out folder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 5");
            try
            {
                folder = m_InventoryService.Folder[inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }
            m_Log.InfoFormat("Testing non-existence 6");
            try
            {
                folder = m_InventoryService.Folder[m_UserID.ID, inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }

            m_Log.InfoFormat("Testing non-existence 7");
            result = m_InventoryService.Folder.GetFolders(m_UserID.ID, rootFolder.ID);
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 8");
            result = m_InventoryService.Folder.Content[m_UserID.ID, rootFolder.ID].Folders;
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 9");
            if (m_InventoryService.Folder.Content.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 10");
            if (m_InventoryService.Folder.Content.TryGetValue(m_UserID.ID, inventoryId, out content))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 11");
            try
            {
                content = m_InventoryService.Folder.Content[m_UserID.ID, inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the expected one */
            }
            m_Log.InfoFormat("Testing non-existence 12");
            resultContent = m_InventoryService.Folder.Content[m_UserID.ID, new UUID[] { inventoryId }];
            foreach (InventoryFolderContent checkItem in resultContent)
            {
                if (checkItem.FolderID == inventoryId)
                {
                    return false;
                }
            }

            m_Log.InfoFormat("Creating the folder");
            m_BackendInventoryService.Folder.Add(testFolder);

            m_Log.InfoFormat("Deleting folder");
            List<UUID> deleted = m_InventoryService.Folder.Delete(m_UserID.ID, new List<UUID> { inventoryId });
            if (!deleted.Contains(inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 1");
            if (m_InventoryService.Folder.ContainsKey(inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 2");
            if (m_InventoryService.Folder.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 3");
            if (m_InventoryService.Folder.TryGetValue(inventoryId, out folder))
            {
                return false;
            }

            m_Log.InfoFormat("Testing non-existence 4");
            if (m_InventoryService.Folder.TryGetValue(m_UserID.ID, inventoryId, out folder))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 5");
            try
            {
                folder = m_InventoryService.Folder[inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }
            m_Log.InfoFormat("Testing non-existence 6");
            try
            {
                folder = m_InventoryService.Folder[m_UserID.ID, inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the okay case */
            }
            m_Log.InfoFormat("Testing non-existence 7");
            result = m_InventoryService.Folder.GetFolders(m_UserID.ID, rootFolder.ID);
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 8");
            result = m_InventoryService.Folder.Content[m_UserID.ID, rootFolder.ID].Folders;
            foreach (InventoryFolder checkItem in result)
            {
                if (checkItem.ID == inventoryId)
                {
                    return false;
                }
            }
            m_Log.InfoFormat("Testing non-existence 9");
            if (m_InventoryService.Folder.Content.ContainsKey(m_UserID.ID, inventoryId))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 10");
            if (m_InventoryService.Folder.Content.TryGetValue(m_UserID.ID, inventoryId, out content))
            {
                return false;
            }
            m_Log.InfoFormat("Testing non-existence 11");
            try
            {
                content = m_InventoryService.Folder.Content[m_UserID.ID, inventoryId];
                return false;
            }
            catch (InventoryFolderNotFoundException)
            {
                /* this is the expected one */
            }
            m_Log.InfoFormat("Testing non-existence 12");
            resultContent = m_InventoryService.Folder.Content[m_UserID.ID, new UUID[] { inventoryId }];
            foreach (InventoryFolderContent checkItem in resultContent)
            {
                if (checkItem.FolderID == inventoryId)
                {
                    return false;
                }
            }

            return true;
        }
    }
}