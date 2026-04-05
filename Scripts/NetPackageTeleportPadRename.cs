using UnityEngine;

public class NetPackageTeleporterRename : NetPackage
{
    private Vector3i _position;
    private string _newName;
    private int _clrIdx;

    public NetPackageTeleporterRename Setup(Vector3i position, string newName, int clrIdx)
    {
        _position = position;
        _newName = newName;
        _clrIdx = clrIdx;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _position = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
        _newName = br.ReadString();
        _clrIdx = br.ReadInt32();
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_position.x);
        bw.Write(_position.y);
        bw.Write(_position.z);
        bw.Write(_newName ?? "");
        bw.Write(_clrIdx);
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;

        var te = world.GetTileEntity(_clrIdx, _position) as TileEntityTeleporter;
        if (te != null)
            te.SetTeleporterName(_newName);

        TeleporterManager.Instance.RenameTeleporter(_position, _newName);
    }

    public override int GetLength()
    {
        return 16 + (_newName != null ? _newName.Length * 2 + 4 : 4);
    }
}
