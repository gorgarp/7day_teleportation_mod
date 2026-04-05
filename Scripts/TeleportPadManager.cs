using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TeleporterManager
{
    private const string SAVE_FILE = "Teleporters.dat";
    private static readonly byte Version = 1;

    private Dictionary<Vector3i, string> TeleporterMap = new Dictionary<Vector3i, string>();
    private readonly object _lock = new object();
    private static TeleporterManager _instance;
    private static readonly object _singletonLock = new object();
    private ThreadManager.ThreadInfo dataSaveThreadInfo;

    public static TeleporterManager Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new TeleporterManager();
                        _instance.Init();
                    }
                }
            }
            return _instance;
        }
    }

    public void Init()
    {
        ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawned);
    }

    private void OnGameStartDone(ref ModEvents.SGameStartDoneData data)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm == null || !cm.IsServer) return;
        Log.Out("[Teleporters] Loading teleporter data...");
        Load();
    }

    private void OnPlayerSpawned(ref ModEvents.SPlayerSpawnedInWorldData data)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm == null || !cm.IsServer) return;
        if (data.ClientInfo == null) return;

        Dictionary<Vector3i, string> snapshot;
        lock (_lock)
        {
            snapshot = new Dictionary<Vector3i, string>(TeleporterMap);
        }

        var package = NetPackageManager.GetPackage<NetPackageTeleporterSync>();
        package.Setup(snapshot);
        data.ClientInfo.SendPackage(package);
    }

    public void RegisterTeleporter(Vector3i position, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (!cm.IsServer)
        {
            cm.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleporterAdd>().Setup(position, name));
            return;
        }

        lock (_lock)
        {
            if (TeleporterMap.TryGetValue(position, out var existing) && existing == name) return;
            TeleporterMap[position] = name;
        }

        cm.SendPackage(
            NetPackageManager.GetPackage<NetPackageTeleporterAdd>().Setup(position, name));
        Save();
    }

    public void RemoveTeleporter(Vector3i position)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (!cm.IsServer)
        {
            cm.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleporterRemove>().Setup(position));
            return;
        }

        lock (_lock)
        {
            TeleporterMap.Remove(position);
        }

        cm.SendPackage(
            NetPackageManager.GetPackage<NetPackageTeleporterRemove>().Setup(position));
        Save();
    }

    public void RenameTeleporter(Vector3i position, string newName)
    {
        if (string.IsNullOrEmpty(newName))
        {
            RemoveTeleporter(position);
            return;
        }
        RegisterTeleporter(position, newName);
    }

    public void ReplaceMap(Dictionary<Vector3i, string> newMap)
    {
        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm != null && cm.IsServer) return;

        lock (_lock)
        {
            TeleporterMap.Clear();
            foreach (var entry in newMap)
                TeleporterMap[entry.Key] = entry.Value;
        }
    }

    public void AddFromNetwork(Vector3i position, string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        lock (_lock)
        {
            TeleporterMap[position] = name;
        }

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm.IsServer)
        {
            cm.SendPackage(
                NetPackageManager.GetPackage<NetPackageTeleporterAdd>().Setup(position, name));
            Save();
        }
    }

    public void RemoveFromNetwork(Vector3i position)
    {
        lock (_lock)
        {
            TeleporterMap.Remove(position);
        }

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm.IsServer)
        {
            cm.SendPackage(
                NetPackageManager.GetPackage<NetPackageTeleporterRemove>().Setup(position));
            Save();
        }
    }

    public List<KeyValuePair<Vector3i, string>> GetDestinations(Vector3i excludePos)
    {
        var result = new List<KeyValuePair<Vector3i, string>>();
        lock (_lock)
        {
            foreach (var entry in TeleporterMap)
            {
                if (entry.Key != excludePos)
                    result.Add(entry);
            }
        }
        result.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public string GetTeleporterName(Vector3i position)
    {
        lock (_lock)
        {
            return TeleporterMap.TryGetValue(position, out var name) ? name : "";
        }
    }

    public int TeleporterCount
    {
        get { lock (_lock) { return TeleporterMap.Count; } }
    }

    private void Save()
    {
        if (dataSaveThreadInfo != null && ThreadManager.ActiveThreads.ContainsKey("silent_TeleporterSave"))
            return;

        var stream = MemoryPools.poolMemoryStream.AllocSync(true);
        using (var bw = MemoryPools.poolBinaryWriter.AllocSync(false))
        {
            bw.SetBaseStream(stream);
            lock (_lock)
            {
                bw.Write(Version);
                bw.Write(TeleporterMap.Count);
                foreach (var entry in TeleporterMap)
                {
                    bw.Write(entry.Key.x);
                    bw.Write(entry.Key.y);
                    bw.Write(entry.Key.z);
                    bw.Write(entry.Value);
                }
            }
        }

        dataSaveThreadInfo = ThreadManager.StartThread("silent_TeleporterSave", null,
            new ThreadManager.ThreadFunctionLoopDelegate(SaveThreaded), null, stream, null, false);
    }

    private int SaveThreaded(ThreadManager.ThreadInfo _threadInfo)
    {
        var stream = (PooledExpandableMemoryStream)_threadInfo.parameter;
        var path = $"{GameIO.GetSaveGameDir()}/{SAVE_FILE}";
        if (!Directory.Exists(GameIO.GetSaveGameDir())) return -1;

        if (File.Exists(path))
            File.Copy(path, $"{path}.bak", true);

        stream.Position = 0L;
        StreamUtils.WriteStreamToFile(stream, path);
        MemoryPools.poolMemoryStream.FreeSync(stream);
        return -1;
    }

    private void Load()
    {
        lock (_lock)
        {
            TeleporterMap.Clear();
        }

        var path = $"{GameIO.GetSaveGameDir()}/{SAVE_FILE}";
        if (!File.Exists(path))
        {
            Log.Out("[Teleporters] No save file found, starting fresh.");
            return;
        }

        try
        {
            LoadFromFile(path);
            Log.Out($"[Teleporters] Loaded {TeleporterCount} teleporters.");
        }
        catch (Exception ex)
        {
            Log.Error($"[Teleporters] Error loading teleporter data: {ex.Message}");
            var backup = $"{path}.bak";
            if (File.Exists(backup))
            {
                try
                {
                    LoadFromFile(backup);
                    Log.Out($"[Teleporters] Loaded {TeleporterCount} teleporters from backup.");
                }
                catch (Exception ex2)
                {
                    Log.Error($"[Teleporters] Error loading backup: {ex2.Message}");
                }
            }
        }
    }

    private void LoadFromFile(string filePath)
    {
        using (var fs = File.OpenRead(filePath))
        using (var br = MemoryPools.poolBinaryReader.AllocSync(false))
        {
            br.SetBaseStream(fs);
            br.ReadByte();
            var count = br.ReadInt32();
            lock (_lock)
            {
                TeleporterMap.Clear();
                for (int i = 0; i < count; i++)
                {
                    var pos = new Vector3i(br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                    var name = br.ReadString();
                    TeleporterMap[pos] = name;
                }
            }
        }
    }
}
