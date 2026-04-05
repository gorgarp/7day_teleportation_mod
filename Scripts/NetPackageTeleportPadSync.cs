using System.Collections.Generic;
using UnityEngine;

public class NetPackageTeleporterSync : NetPackage
{
    private Dictionary<Vector3i, string> _teleporterMap;

    public NetPackageTeleporterSync Setup(Dictionary<Vector3i, string> teleporterMap)
    {
        _teleporterMap = teleporterMap;
        return this;
    }

    public override void read(PooledBinaryReader br)
    {
        _teleporterMap = new Dictionary<Vector3i, string>();
        var count = br.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
            var name = br.ReadString();
            _teleporterMap[pos] = name;
        }
    }

    public override void write(PooledBinaryWriter bw)
    {
        base.write(bw);
        bw.Write(_teleporterMap.Count);
        foreach (var entry in _teleporterMap)
        {
            bw.Write(entry.Key.x);
            bw.Write(entry.Key.y);
            bw.Write(entry.Key.z);
            bw.Write(entry.Value);
        }
    }

    public override void ProcessPackage(World world, GameManager callbacks)
    {
        if (world == null) return;
        TeleporterManager.Instance.ReplaceMap(_teleporterMap);
    }

    public override int GetLength()
    {
        int len = 4;
        if (_teleporterMap != null)
        {
            foreach (var entry in _teleporterMap)
                len += 12 + (entry.Value != null ? entry.Value.Length * 2 + 4 : 4);
        }
        return len;
    }
}
