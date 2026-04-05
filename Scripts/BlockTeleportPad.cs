using Platform;
using UnityEngine;

public class BlockTeleporter : Block
{
    public BlockTeleporter()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();
    }

    private new BlockActivationCommand[] cmds = new BlockActivationCommand[]
    {
        new BlockActivationCommand("teleport", "map_waypoints", true, true),
        new BlockActivationCommand("edit", "pen", false, true),
        new BlockActivationCommand("take", "hand", false, false)
    };

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos,
        BlockValue _blockValue, PlatformUserIdentifierAbs _addedByPlayer)
    {
        if (_blockValue.ischild)
        {
            base.OnBlockAdded(world, _chunk, _blockPos, _blockValue, _addedByPlayer);
            return;
        }

        var te = new TileEntityTeleporter(_chunk);
        te.localChunkPos = World.toBlock(_blockPos);
        te.SetOwner(_addedByPlayer ?? PlatformManager.InternalLocalUserIdentifier);
        _chunk.AddTileEntity(te);
        base.OnBlockAdded(world, _chunk, _blockPos, _blockValue, _addedByPlayer);
    }

    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos,
        BlockValue _blockValue)
    {
        if (!_blockValue.ischild)
        {
            TeleporterManager.Instance.RemoveTeleporter(_blockPos);
            _chunk.RemoveTileEntityAt<TileEntityTeleporter>((World)world, World.toBlock(_blockPos));
        }
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
    }

    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos,
        BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
        if (_blockValue.ischild) return;

        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleporter;
        if (te != null && !string.IsNullOrEmpty(te.TeleporterName))
            TeleporterManager.Instance.RegisterTeleporter(_blockPos, te.TeleporterName);
    }

    public override void OnBlockEntityTransformAfterActivated(WorldBase _world, Vector3i _blockPos,
        int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
    {
        base.OnBlockEntityTransformAfterActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
        if (_blockValue.ischild) return;

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleporter;
        if (te != null && !string.IsNullOrEmpty(te.TeleporterName))
            TeleporterManager.Instance.RegisterTeleporter(_blockPos, te.TeleporterName);
    }

    public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return true;
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleporter;
        if (te == null)
            return "Press [action:Activate] to use Teleporter";

        if (string.IsNullOrEmpty(te.TeleporterName))
            return Localization.Get("teleporter_configure");

        return string.Format(Localization.Get("teleporter_use"), te.TeleporterName);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world,
        BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        var te = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntityTeleporter;
        bool isNamed = te != null && !string.IsNullOrEmpty(te.TeleporterName);
        bool isInLandClaim = _world.IsMyLandProtectedBlock(_blockPos,
            _world.GetGameManager().GetPersistentLocalPlayer());

        cmds[0].enabled = isNamed;
        cmds[1].enabled = true;
        cmds[2].enabled = isInLandClaim;
        return cmds;
    }

    public override bool OnBlockActivated(string commandName, WorldBase _world, int _cIdx,
        Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            var parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return OnBlockActivated(commandName, _world, _cIdx, parentPos,
                _world.GetBlock(parentPos), _player);
        }

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleporter;
        if (te == null) return false;

        switch (commandName)
        {
            case "teleport":
                OpenTeleportUI(_player, _blockPos);
                return true;

            case "edit":
                OpenNamingUI(_player, _blockPos, _cIdx, te);
                return true;

            case "take":
                TakeBlock(_world, _cIdx, _blockPos, _blockValue, _player);
                return true;

            default:
                return false;
        }
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            var parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            return OnBlockActivated(_world, _cIdx, parentPos, _world.GetBlock(parentPos), _player);
        }

        var te = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityTeleporter;
        if (te == null) return false;

        if (string.IsNullOrEmpty(te.TeleporterName))
            OpenNamingUI(_player, _blockPos, _cIdx, te);
        else
            OpenTeleportUI(_player, _blockPos);

        return true;
    }

    private void OpenTeleportUI(EntityPlayerLocal _player, Vector3i _blockPos)
    {
        _player.AimingGun = false;
        XUiC_TeleporterWindow.Open(
            LocalPlayerUI.GetUIForPlayer(_player),
            _blockPos);
    }

    private void OpenNamingUI(EntityPlayerLocal _player, Vector3i _blockPos, int _cIdx,
        TileEntityTeleporter te)
    {
        _player.AimingGun = false;
        XUiC_TeleporterNaming.Open(
            LocalPlayerUI.GetUIForPlayer(_player),
            _blockPos,
            _cIdx,
            te.TeleporterName);
    }

    private void TakeBlock(WorldBase _world, int _cIdx, Vector3i _blockPos,
        BlockValue _blockValue, EntityPlayerLocal _player)
    {
        var uiForPlayer = LocalPlayerUI.GetUIForPlayer(_player);
        var itemStack = new ItemStack(_blockValue.ToItemValue(), 1);
        if (!uiForPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiForPlayer.xui.PlayerInventory.DropItem(itemStack);

        _world.SetBlockRPC(_cIdx, _blockPos, BlockValue.Air);
    }
}
