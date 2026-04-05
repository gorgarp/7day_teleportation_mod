using System;
using UnityEngine;

public class XUiC_TeleporterEntry : XUiController
{
    public event Action<Vector3i> TeleportRequested;

    private string teleporterName = "";
    private Vector3i teleporterPosition = Vector3i.zero;
    private string distanceText = "";
    private XUiC_SimpleButton btnGo;

    public override void Init()
    {
        base.Init();
        OnPress += OnEntryClicked;

        var btn = GetChildById("btnGo");
        if (btn is XUiC_SimpleButton simpleBtn)
        {
            btnGo = simpleBtn;
            btnGo.OnPressed += OnEntryClicked;
        }
    }

    public void SetData(string name, Vector3i position)
    {
        teleporterName = name ?? "";
        teleporterPosition = position;

        var player = xui?.playerUI?.entityPlayer;
        if (player != null && position != Vector3i.zero)
        {
            float dist = Vector3.Distance(player.position, position.ToVector3());
            distanceText = $"{dist:F0}m";
        }
        else
        {
            distanceText = "";
        }

        IsDirty = true;
        RefreshBindings(true);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "teleportername":
                value = teleporterName;
                return true;
            case "distance":
                value = distanceText;
                return true;
            case "coords":
                if (teleporterPosition == Vector3i.zero)
                    value = "";
                else
                    value = $"({teleporterPosition.x}, {teleporterPosition.y}, {teleporterPosition.z})";
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void OnEntryClicked(XUiController _sender, int _mouseButton)
    {
        if (teleporterPosition != Vector3i.zero)
            TeleportRequested?.Invoke(teleporterPosition);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        OnPress -= OnEntryClicked;
        if (btnGo != null)
            btnGo.OnPressed -= OnEntryClicked;
    }
}
