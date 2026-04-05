using UnityEngine;

public class NetPackageTeleporterAdd : NetPackage
{
    private Vector3i _position;
    private string _name;

    public NetPackageTeleporterAdd Setup(Vector3i position, string name)
    {
        _position = position;
        _name = name;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _position = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
        _name = br.ReadString();
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_position.x);
        bw.Write(_position.y);
        bw.Write(_position.z);
        bw.Write(_name);
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;
        TeleporterManager.Instance.AddFromNetwork(_position, _name);
    }

    public override int GetLength()
    {
        return 12 + (_name != null ? _name.Length * 2 + 4 : 4);
    }
}
