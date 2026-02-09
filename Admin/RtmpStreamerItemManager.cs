using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VideoOS.Platform;
using VideoOS.Platform.Admin;

namespace RtmpStreamerPlugin.Admin
{
    public class RtmpStreamerItemManager : ItemManager
    {
        private StreamConfigUserControl _userControl;
        private readonly Guid _kind;

        public RtmpStreamerItemManager(Guid kind)
        {
            _kind = kind;
        }

        public override void Close()
        {
            ReleaseUserControl();
        }

        #region User Control

        public override UserControl GenerateDetailUserControl()
        {
            _userControl = new StreamConfigUserControl();
            _userControl.ConfigurationChangedByUser += OnConfigurationChangedByUser;
            return _userControl;
        }

        private void OnConfigurationChangedByUser(object sender, EventArgs e)
        {
            if (CurrentItem != null && _userControl != null)
            {
                _userControl.UpdateItem(CurrentItem);
                Configuration.Instance.SaveItemConfiguration(RtmpStreamerPluginDefinition.PluginId, CurrentItem);
            }
        }

        public override void ReleaseUserControl()
        {
            _userControl = null;
        }

        public override void FillUserControl(Item item)
        {
            CurrentItem = item;
            _userControl?.FillContent(item);
        }

        public override void ClearUserControl()
        {
            CurrentItem = null;
            _userControl?.ClearContent();
        }

        public override bool ValidateAndSaveUserControl()
        {
            if (CurrentItem != null && _userControl != null)
            {
                _userControl.UpdateItem(CurrentItem);
                Configuration.Instance.SaveItemConfiguration(RtmpStreamerPluginDefinition.PluginId, CurrentItem);
            }
            return true;
        }

        #endregion

        #region Item Management

        public override string GetItemName()
        {
            if (_userControl != null)
                return _userControl.DisplayName;
            return "";
        }

        public override void SetItemName(string name)
        {
            if (CurrentItem != null)
                CurrentItem.Name = name;
        }

        public override List<Item> GetItems()
        {
            return Configuration.Instance.GetItemConfigurations(
                RtmpStreamerPluginDefinition.PluginId, null, _kind);
        }

        public override List<Item> GetItems(Item parentItem)
        {
            return Configuration.Instance.GetItemConfigurations(
                RtmpStreamerPluginDefinition.PluginId, parentItem, _kind);
        }

        public override Item GetItem(FQID fqid)
        {
            return Configuration.Instance.GetItemConfiguration(
                RtmpStreamerPluginDefinition.PluginId, _kind, fqid.ObjectId);
        }

        public override Item CreateItem(Item parentItem, FQID suggestedFQID)
        {
            CurrentItem = new Item(suggestedFQID, "New RTMP Stream");
            CurrentItem.Properties["Enabled"] = "Yes";

            _userControl?.FillContent(CurrentItem);
            Configuration.Instance.SaveItemConfiguration(RtmpStreamerPluginDefinition.PluginId, CurrentItem);
            return CurrentItem;
        }

        public override void DeleteItem(Item item)
        {
            if (item != null)
            {
                Configuration.Instance.DeleteItemConfiguration(RtmpStreamerPluginDefinition.PluginId, item);
            }
        }

        #endregion
    }
}
