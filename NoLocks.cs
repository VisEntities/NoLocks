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
    [Info("No Locks", "VisEntities", "2.0.0")]
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

            [JsonProperty("Allow Code Lock Deployment")]
            public bool AllowCodeLockDeployment { get; set; }

            [JsonProperty("Allow Key Lock Deployment")]
            public bool AllowKeyLockDeployment { get; set; }

            [JsonProperty("Unlockable Entities")]
            public List<string> UnlockableEntity { get; set; }
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
                AllowCodeLockDeployment = false,
                AllowKeyLockDeployment = false,
                UnlockableEntity = new List<string>
                {
                    "fridge.deployed",
                    "box.wooden.large",
                    "cupboard.tool.deployed",
                    "wall.frame.garagedoor",
                    "door.hinged.metal",
                    "locker.deployed",
                    "woodbox_deployed"
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

            if (_config.UnlockableEntity.Contains(targetEntity.ShortPrefabName))
            {
                SendMessage(player, Lang.UnlockableEntity);
                return true;
            }

            int itemId = activeItem.info.itemid;
            switch (itemId)
            {
                case ITEM_ID_CODE_LOCK:
                    if (!_config.AllowCodeLockDeployment)
                    {
                SendMessage(player, Lang.DeployCodeLockBlocked);
                        return true;
                    }
                    break;
                case ITEM_ID_KEY_LOCK:
                    if (!_config.AllowKeyLockDeployment)
                    {
                        SendMessage(player, Lang.DeployKeyLockBlocked);
                        return true;
                    }
                    break;
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Functions

        private IEnumerator RemoveLocksCoroutine()
        {
            foreach (BaseEntity entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                if (entity != null)
                {
                    BaseLock baseLock = GetEntityLock(entity);
                    if (baseLock != null && _config.UnlockableEntity.Contains(entity.ShortPrefabName))
                    {
                        if (entity.OwnerID != 0)
                        {
                            BasePlayer ownerPlayer = FindPlayerById(entity.OwnerID);
                            if (ownerPlayer != null && PermissionUtil.HasPermission(ownerPlayer, PermissionUtil.BYPASS))
                            {

                            }
                        }
                        else
                            baseLock.Kill();
                    }
                }

                yield return CoroutineEx.waitForSeconds(0.1f);
            }
        }

        #endregion Functions

        #region Helper Functions

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
            public const string UnlockableEntity = "UnlockableEntity";
            public const string DeployCodeLockBlocked = "DeployCodeLockBlocked";
            public const string DeployKeyLockBlocked = "DeployKeyLockBlocked";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.UnlockableEntity] = "Deploying locks on this entity type is blocked.",
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