using UnityEngine;

public class NetPackageTeleporterRemove : NetPackage
{
    private Vector3i _position;

    public NetPackageTeleporterRemove Setup(Vector3i position)
    {
        _position = position;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _position = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_position.x);
        bw.Write(_position.y);
        bw.Write(_position.z);
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;
        TeleporterManager.Instance.RemoveFromNetwork(_position);
    }

    public override int GetLength() => 12;
}
