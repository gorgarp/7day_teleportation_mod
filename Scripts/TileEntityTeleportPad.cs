using Platform;
using System.Collections.Generic;

public class TileEntityTeleporter : TileEntity
{
    public string TeleporterName = "";
    public PlatformUserIdentifierAbs OwnerID;

    public TileEntityTeleporter(Chunk _chunk) : base(_chunk)
    {
        OwnerID = null;
    }

    public override TileEntityType GetTileEntityType()
    {
        return (TileEntityType)210;
    }

    public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
    {
        base.read(_br, _eStreamMode);
        _br.ReadByte();
        TeleporterName = _br.ReadString();
        bool hasOwner = _br.ReadBoolean();
        OwnerID = hasOwner ? PlatformUserIdentifierAbs.FromStream(_br, false, false) : null;
    }

    public override void write(PooledBinaryWriter _bw, StreamModeWrite _eStreamMode)
    {
        base.write(_bw, _eStreamMode);
        _bw.Write((byte)1);
        _bw.Write(TeleporterName ?? "");
        _bw.Write(OwnerID != null);
        if (OwnerID != null)
            OwnerID.ToStream(_bw, false);
    }

    public override TileEntity Clone()
    {
        var te = new TileEntityTeleporter(chunk);
        te.localChunkPos = localChunkPos;
        te.TeleporterName = TeleporterName;
        te.OwnerID = OwnerID;
        return te;
    }

    public override void CopyFrom(TileEntity _other)
    {
        base.CopyFrom(_other);
        var other = (TileEntityTeleporter)_other;
        TeleporterName = other.TeleporterName;
        OwnerID = other.OwnerID;
    }

    public void SetTeleporterName(string name)
    {
        TeleporterName = name ?? "";
        setModified();
    }

    public void SetOwner(PlatformUserIdentifierAbs owner)
    {
        OwnerID = owner;
        setModified();
    }

    public Vector3i GetWorldPos()
    {
        return ToWorldPos();
    }
}
