using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Originally created by Orange, up to version 1.0.0
 */

namespace Oxide.Plugins
{
    [Info("No Locks", "VisEntities", "3.0.0")]
    [Description("Blocks the deployment of key and code locks on certain entities.")]
    public class NoLocks : RustPlugin
    {
        #region Fields

        private static NoLocks _plugin;
        private static Configuration _config;
        private const int ITEM_ID_KEY_LOCK = -850982208;
        private const int ITEM_ID_CODE_LOCK = 1159991980;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Remove Locks On Startup")]
            public bool RemoveLocksOnStartup { get; set; }

            [JsonProperty("Entity Groups")]
            public List<LockConfig> EntityGroups { get; set; }
        }

        private class LockConfig
        {
            [JsonProperty("Prefab Short Names")]
            public List<string> PrefabShortNames { get; set; }

            [JsonProperty("Allow Code Lock Deployment")]
            public bool AllowCodeLockDeployment { get; set; }

            [JsonProperty("Allow Key Lock Deployment")]
            public bool AllowKeyLockDeployment { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                RemoveLocksOnStartup = false,
                EntityGroups = new List<LockConfig>
                {
                    new LockConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "cupboard.tool.deployed",
                            "box.wooden.large",
                            "woodbox_deployed"
                        },
                        AllowCodeLockDeployment = false,
                        AllowKeyLockDeployment = true
                    },
                    new LockConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "wall.frame.garagedoor",
                            "door.hinged.metal"
                        },
                        AllowCodeLockDeployment = true,
                        AllowKeyLockDeployment = false
                    },
                    new LockConfig
                    {
                        PrefabShortNames = new List<string>
                        {
                            "locker.deployed",
                            "fridge.deployed"
                        },
                        AllowCodeLockDeployment = true,
                        AllowKeyLockDeployment = true
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            CoroutineUtil.StopAllCoroutines();
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            if (_config.RemoveLocksOnStartup)
            {
                CoroutineUtil.StartCoroutine(Guid.NewGuid().ToString(), RemoveLocksCoroutine());
            }
        }

        private object CanDeployItem(BasePlayer player, Deployer deployerItem, NetworkableId targetEntityId)
        {
            if (player == null || deployerItem == null)
                return null;

            if (PermissionUtil.HasPermission(player, PermissionUtil.BYPASS))
                return null;

            Deployable deployable = deployerItem.GetDeployable();
            if (deployable == null)
                return null;

            Item activeItem = player.GetActiveItem();
            if (activeItem == null)
                return null;

            BaseEntity targetEntity = BaseNetworkable.serverEntities.Find(targetEntityId) as BaseEntity;
            if (targetEntity == null)
                return null;

            LockConfig lockConfig = GetLockConfigForPrefab(targetEntity.ShortPrefabName);
            if (lockConfig == null)
                return null;

            int itemId = activeItem.info.itemid;
            switch (itemId)
            {
                case ITEM_ID_CODE_LOCK:
                    {
                        if (!lockConfig.AllowCodeLockDeployment)
                        {
                            SendMessage(player, Lang.DeployCodeLockBlocked);
                            return true;
                        }
                        break;
                    }
                case ITEM_ID_KEY_LOCK:
                    {
                        if (!lockConfig.AllowKeyLockDeployment)
                        {
                            SendMessage(player, Lang.DeployKeyLockBlocked);
                            return true;
                        }
                        break;
                    }
            }
        
            return null;
        }

        #endregion Oxide Hooks

        #region Locks Removal

        private IEnumerator RemoveLocksCoroutine()
        {
            foreach (BaseEntity entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                yield return CoroutineEx.waitForSeconds(0.1f);

                LockConfig lockConfig = GetLockConfigForPrefab(entity.ShortPrefabName);
                if (entity == null || lockConfig == null)
                    continue;

                BaseLock baseLock = GetEntityLock(entity);
                if (baseLock == null)
                    continue;
    
                bool shouldRemoveLock = false;

                if (baseLock is CodeLock && !lockConfig.AllowCodeLockDeployment)
                {
                    shouldRemoveLock = true;
                }
                else if (baseLock is KeyLock && !lockConfig.AllowKeyLockDeployment)
                {
                    shouldRemoveLock = true;
                }

                if (shouldRemoveLock)
                {
                    if (entity.OwnerID != 0)
                    {
                        BasePlayer ownerPlayer = FindPlayerById(entity.OwnerID);
                        if (ownerPlayer != null && PermissionUtil.HasPermission(ownerPlayer, PermissionUtil.BYPASS))
                            continue;
                    }
                    baseLock.Kill();
                }        
            }
        }

        #endregion Locks Removal

        #region Helper Functions

        private LockConfig GetLockConfigForPrefab(string prefabName)
        {
            foreach (var group in _config.EntityGroups)
            {
                if (group.PrefabShortNames.Contains(prefabName))
                    return group;
            }
            return null;
        }

        private static BaseLock GetEntityLock(BaseEntity entity)
        {
            return entity.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
        }

        public static BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId);
        }

        #endregion Helper Functions

        #region Coroutine Util

        private static class CoroutineUtil
        {
            private static readonly Dictionary<string, Coroutine> _activeCoroutines = new Dictionary<string, Coroutine>();

            public static void StartCoroutine(string coroutineName, IEnumerator coroutineFunction)
            {
                StopCoroutine(coroutineName);

                Coroutine coroutine = ServerMgr.Instance.StartCoroutine(coroutineFunction);
                _activeCoroutines[coroutineName] = coroutine;
            }

            public static void StopCoroutine(string coroutineName)
            {
                if (_activeCoroutines.TryGetValue(coroutineName, out Coroutine coroutine))
                {
                    if (coroutine != null)
                        ServerMgr.Instance.StopCoroutine(coroutine);

                    _activeCoroutines.Remove(coroutineName);
                }
            }

            public static void StopAllCoroutines()
            {
                foreach (string coroutineName in _activeCoroutines.Keys.ToArray())
                {
                    StopCoroutine(coroutineName);
                }
            }
        }

        #endregion Coroutine Util

        #region Permission

        private static class PermissionUtil
        {
            public const string BYPASS = "nolocks.bypass";
            private static readonly List<string> _permissions = new List<string>
            {
                BYPASS,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permission

        #region Localization

        private class Lang
        {
            public const string DeployCodeLockBlocked = "DeployCodeLockBlocked";
            public const string DeployKeyLockBlocked = "DeployKeyLockBlocked";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.DeployCodeLockBlocked] = "Code locks cannot be deployed on this entity.",
                [Lang.DeployKeyLockBlocked] = "Key locks cannot be deployed on this entity.",
            }, this, "en");
        }
        
        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}