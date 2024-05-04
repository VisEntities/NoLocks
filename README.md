This plugin prevents players from placing locks on entities like doors, boxes, tool cupboards, and others specified in the config. It also allows you to block the deployment of either key locks or code locks separately. Once loaded, the plugin will review all existing locked entities on the server to determine if their locks should be removed.

[Demonstration](https://youtu.be/TfwtpbyXm5M)

-----------------

## Permissions
- `nolocks.bypass` - Allows to ignore lock deployment restrictions.

---------------

## Configuration

```json
{
  "Version": "2.0.0",
  "Remove Locks On Startup": false,
  "Allow Code Lock Deployment": false,
  "Allow Key Lock Deployment": false,
  "Unlockable Entities": [
    "fridge.deployed",
    "box.wooden.large"
  ]
}
```

-----------------------

## Localization

```json
{
  "UnlockableEntity": "Deploying locks on this entity type is blocked.",
  "DeployCodeLockBlocked": "Code locks cannot be deployed on this entity.",
  "DeployKeyLockBlocked": "Key locks cannot be deployed on this entity."
}
```

---------------


## Credits

 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Orange**, up to version 1.0.0