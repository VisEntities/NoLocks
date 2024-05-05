This plugin prevents players from placing locks on entities like doors, boxes, tool cupboards, and others specified in the config. It also allows you to block the deployment of either type of lock—key or code lock—separately. Once loaded, the plugin will review all existing locked entities on the server to determine if their locks should be removed.

[Demonstration](https://www.youtube.com/watch?v=V20BC1DWyW4)

-----------------

## Permissions
- `nolocks.bypass` - Allows to ignore lock deployment restrictions.

---------------

## Configuration

```json
{
  "Version": "3.0.0",
  "Remove Locks On Startup": false,
  "Entity Groups": [
    {
      "Prefab Short Names": [
        "cupboard.tool.deployed",
        "box.wooden.large",
        "woodbox_deployed"
      ],
      "Allow Code Lock Deployment": false,
      "Allow Key Lock Deployment": true
    },
    {
      "Prefab Short Names": [
        "wall.frame.garagedoor",
        "door.hinged.metal"
      ],
      "Allow Code Lock Deployment": true,
      "Allow Key Lock Deployment": false
    },
    {
      "Prefab Short Names": [
        "locker.deployed",
        "fridge.deployed"
      ],
      "Allow Code Lock Deployment": true,
      "Allow Key Lock Deployment": true
    }
  ]
}
```

-----------------------

## Localization

```json
{
  "DeployCodeLockBlocked": "Code locks cannot be deployed on this entity.",
  "DeployKeyLockBlocked": "Key locks cannot be deployed on this entity."
}
```

---------------


## Credits

 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Orange**, up to version 1.0.0