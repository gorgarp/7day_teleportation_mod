using UnityEngine;

public class XUiC_TeleporterNaming : XUiController
{
    public static string ID = "";

    private Vector3i teleporterPosition;
    private int clrIdx;
    private string currentName = "";
    private XUiC_TextInput textInput;
    private XUiC_SimpleButton btnSave;
    private XUiC_SimpleButton btnCancel;

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;

        textInput = GetChildById("txtTeleporterName") as XUiC_TextInput;

        foreach (var child in GetChildrenByType<XUiC_SimpleButton>())
        {
            string id = child.ViewComponent?.ID ?? "";
            if (id == "btnSave")
            {
                btnSave = child;
                btnSave.OnPressed += OnSavePressed;
            }
            if (id == "btnCancel")
            {
                btnCancel = child;
                btnCancel.OnPressed += OnCancelPressed;
            }
        }

        if (textInput != null)
            textInput.OnSubmitHandler += OnTextSubmitted;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (textInput != null)
            textInput.Text = currentName;
        IsDirty = true;
        RefreshBindings(true);
    }

    public static void Open(LocalPlayerUI _playerUI, Vector3i _blockPos, int _clrIdx, string _currentName)
    {
        if (string.IsNullOrEmpty(ID)) return;

        var windowGroup = _playerUI.xui.FindWindowGroupByName(ID);
        if (windowGroup == null) return;

        var controller = windowGroup.GetChildByType<XUiC_TeleporterNaming>();
        if (controller != null)
        {
            controller.teleporterPosition = _blockPos;
            controller.clrIdx = _clrIdx;
            controller.currentName = _currentName ?? "";
        }

        _playerUI.windowManager.Open(ID, true);
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "currentname":
                value = currentName;
                return true;
            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void SaveName()
    {
        var newName = textInput != null ? textInput.Text.Trim() : "";
        if (string.IsNullOrEmpty(newName))
        {
            GameManager.ShowTooltip(xui.playerUI.entityPlayer,
                Localization.Get("teleporter_name_empty"));
            return;
        }

        if (newName.Length > 24)
            newName = newName.Substring(0, 24);

        var world = GameManager.Instance.World;
        var te = world.GetTileEntity(clrIdx, teleporterPosition) as TileEntityTeleporter;
        if (te != null)
            te.SetTeleporterName(newName);

        var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
        if (cm.IsServer)
        {
            TeleporterManager.Instance.RegisterTeleporter(teleporterPosition, newName);
        }
        else
        {
            cm.SendToServer(
                NetPackageManager.GetPackage<NetPackageTeleporterRename>().Setup(teleporterPosition, newName, clrIdx));
        }

        GameManager.ShowTooltip(xui.playerUI.entityPlayer,
            string.Format(Localization.Get("teleporter_named"), newName));

        xui.playerUI.windowManager.Close(ID);
    }

    private void OnSavePressed(XUiController _sender, int _mouseButton)
    {
        SaveName();
    }

    private void OnCancelPressed(XUiController _sender, int _mouseButton)
    {
        xui.playerUI.windowManager.Close(ID);
    }

    private void OnTextSubmitted(XUiController _sender, string _text)
    {
        SaveName();
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (btnSave != null)
            btnSave.OnPressed -= OnSavePressed;
        if (btnCancel != null)
            btnCancel.OnPressed -= OnCancelPressed;
        if (textInput != null)
            textInput.OnSubmitHandler -= OnTextSubmitted;
    }
}
