using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class XUiC_TeleporterWindow : XUiController
{
    public static string ID = "";

    private Vector3i sourceTeleporterPos;
    private List<KeyValuePair<Vector3i, string>> allDestinations;
    private List<KeyValuePair<Vector3i, string>> filteredDestinations;
    private XUiC_TeleporterEntry[] entryControllers;
    private XUiC_TextInput searchInput;
    private int currentPage = 0;
    private const int ENTRIES_PER_PAGE = 8;
    private string searchText = "";
    private int sortMode = 0;

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;

        entryControllers = GetChildrenByType<XUiC_TeleporterEntry>();
        foreach (var entry in entryControllers)
            entry.TeleportRequested += OnTeleportRequested;

        searchInput = GetChildById("txtSearch") as XUiC_TextInput;
        if (searchInput != null)
            searchInput.OnChangeHandler += OnSearchChanged;

        foreach (var child in GetChildrenByType<XUiC_SimpleButton>())
        {
            string id = child.ViewComponent?.ID ?? "";
            if (id == "btnClose") child.OnPressed += OnClosePressed;
            if (id == "btnPrev") child.OnPressed += OnPrevPage;
            if (id == "btnNext") child.OnPressed += OnNextPage;
            if (id == "btnSortName") child.OnPressed += OnSortName;
            if (id == "btnSortDist") child.OnPressed += OnSortDist;
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        currentPage = 0;
        searchText = "";
        if (searchInput != null)
            searchInput.Text = "";
        RefreshDestinations();
    }

    public override void OnClose()
    {
        base.OnClose();
    }

    public static void Open(LocalPlayerUI _playerUI, Vector3i _blockPos)
    {
        if (string.IsNullOrEmpty(ID)) return;

        var windowGroup = _playerUI.xui.FindWindowGroupByName(ID);
        if (windowGroup == null) return;

        var controller = windowGroup.GetChildByType<XUiC_TeleporterWindow>();
        if (controller != null)
            controller.sourceTeleporterPos = _blockPos;

        _playerUI.windowManager.Open(ID, true);
    }

    private void RefreshDestinations()
    {
        allDestinations = TeleporterManager.Instance.GetDestinations(sourceTeleporterPos);
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (string.IsNullOrEmpty(searchText))
            filteredDestinations = new List<KeyValuePair<Vector3i, string>>(allDestinations);
        else
            filteredDestinations = allDestinations
                .Where(d => d.Value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

        var player = xui?.playerUI?.entityPlayer;
        switch (sortMode)
        {
            case 0:
                filteredDestinations.Sort((a, b) =>
                    string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase));
                break;
            case 1:
                filteredDestinations.Sort((a, b) =>
                    string.Compare(b.Value, a.Value, StringComparison.OrdinalIgnoreCase));
                break;
            case 2:
                if (player != null)
                    filteredDestinations.Sort((a, b) =>
                        Vector3.Distance(player.position, a.Key.ToVector3())
                        .CompareTo(Vector3.Distance(player.position, b.Key.ToVector3())));
                break;
            case 3:
                if (player != null)
                    filteredDestinations.Sort((a, b) =>
                        Vector3.Distance(player.position, b.Key.ToVector3())
                        .CompareTo(Vector3.Distance(player.position, a.Key.ToVector3())));
                break;
        }

        if (currentPage * ENTRIES_PER_PAGE >= filteredDestinations.Count)
            currentPage = 0;

        UpdateEntries();
        IsDirty = true;
        RefreshBindings(true);
    }

    private void UpdateEntries()
    {
        if (entryControllers == null) return;

        int startIdx = currentPage * ENTRIES_PER_PAGE;
        for (int i = 0; i < entryControllers.Length && i < ENTRIES_PER_PAGE; i++)
        {
            int dataIdx = startIdx + i;
            if (dataIdx < filteredDestinations.Count)
            {
                entryControllers[i].SetData(filteredDestinations[dataIdx].Value, filteredDestinations[dataIdx].Key);
                entryControllers[i].ViewComponent.IsVisible = true;
            }
            else
            {
                entryControllers[i].SetData("", Vector3i.zero);
                entryControllers[i].ViewComponent.IsVisible = false;
            }
        }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "teleportercount":
                if (filteredDestinations == null)
                    value = "0";
                else if (allDestinations != null && filteredDestinations.Count != allDestinations.Count)
                    value = $"{filteredDestinations.Count} / {allDestinations.Count} teleporters";
                else
                    value = $"{filteredDestinations.Count} teleporters";
                return true;
            case "sourcename":
                value = TeleporterManager.Instance.GetTeleporterName(sourceTeleporterPos);
                return true;
            case "pageinfo":
                if (filteredDestinations == null || filteredDestinations.Count == 0)
                    value = "";
                else
                {
                    int totalPages = Mathf.CeilToInt((float)filteredDestinations.Count / ENTRIES_PER_PAGE);
                    value = totalPages > 1 ? $"Page {currentPage + 1}/{totalPages}" : "";
                }
                return true;
            case "hasprev":
                value = (currentPage > 0).ToString();
                return true;
            case "hasnext":
                if (filteredDestinations == null)
                    value = "false";
                else
                {
                    int totalPages = Mathf.CeilToInt((float)filteredDestinations.Count / ENTRIES_PER_PAGE);
                    value = (currentPage < totalPages - 1).ToString();
                }
                return true;
            case "sortlabel":
                switch (sortMode)
                {
                    case 0: value = "Name A-Z"; break;
                    case 1: value = "Name Z-A"; break;
                    case 2: value = "Nearest"; break;
                    case 3: value = "Farthest"; break;
                    default: value = ""; break;
                }
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void OnSearchChanged(XUiController _sender, string _text, bool _changeFromCode)
    {
        searchText = _text ?? "";
        currentPage = 0;
        ApplyFilterAndSort();
    }

    private void OnSortName(XUiController _sender, int _mouseButton)
    {
        sortMode = (sortMode == 0) ? 1 : 0;
        currentPage = 0;
        ApplyFilterAndSort();
    }

    private void OnSortDist(XUiController _sender, int _mouseButton)
    {
        sortMode = (sortMode == 2) ? 3 : 2;
        currentPage = 0;
        ApplyFilterAndSort();
    }

    private void OnTeleportRequested(Vector3i destination)
    {
        var player = xui.playerUI.entityPlayer;
        if (player == null) return;

        xui.playerUI.windowManager.Close(ID);

        var destName = TeleporterManager.Instance.GetTeleporterName(destination);
        var chunkObserver = GameManager.Instance.AddChunkObserver(destination, true, 2, -1);
        var ctx = SynchronizationContext.Current;

        Task.Delay(500).ContinueWith(_ =>
            ctx.Post(__ =>
            {
                GameManager.Instance.RemoveChunkObserver(chunkObserver);
                var teleportPos = new Vector3(destination.x + 0.5f, destination.y + 1.1f, destination.z + 0.5f);
                player.motion = Vector3.zero;
                player.SetPosition(teleportPos);
                player.fallDistance = 0f;
                GameManager.ShowTooltip(player,
                    string.Format(Localization.Get("teleporter_teleported"), destName));
            }, null));
    }

    private void OnClosePressed(XUiController _sender, int _mouseButton)
    {
        xui.playerUI.windowManager.Close(ID);
    }

    private void OnPrevPage(XUiController _sender, int _mouseButton)
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateEntries();
            RefreshBindings(true);
        }
    }

    private void OnNextPage(XUiController _sender, int _mouseButton)
    {
        if (filteredDestinations != null)
        {
            int totalPages = Mathf.CeilToInt((float)filteredDestinations.Count / ENTRIES_PER_PAGE);
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdateEntries();
                RefreshBindings(true);
            }
        }
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (entryControllers != null)
            foreach (var entry in entryControllers)
                entry.TeleportRequested -= OnTeleportRequested;
    }
}
