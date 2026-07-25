using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExitClone.Core;

namespace ExitClone.UI.Pages
{
    public class RoutesPage : UserControl
    {
        private readonly AppState _state;
        private readonly ListView _routes = new ListView();
        private readonly CheckedListBox _relays = new CheckedListBox();
        private readonly Label _summary = new Label();

        public event EventHandler OptimizeRequested;

        public RoutesPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);
            Build();
            LoadRelays();
            _state.Controller.RouteMeasured += (s, e) => Safe(Refresh);
            _state.Controller.StateChanged += (s, e) => Safe(Refresh);
        }

        private void Build()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Background };
            var retest = Theme.GhostButton("Re-test routes");
            retest.SetBounds(0, 8, 140, 30);
            retest.Click += (s, e) => OptimizeRequested?.Invoke(this, EventArgs.Empty);

            var pin = Theme.GhostButton("Pin selected route");
            pin.SetBounds(150, 8, 160, 30);
            pin.Click += (s, e) => PinSelected();

            var clearPin = Theme.GhostButton("Clear pin");
            clearPin.SetBounds(320, 8, 110, 30);
            clearPin.Click += (s, e) =>
            {
                _state.Settings.ProfileFor(_state.SelectedGame?.Id).PinnedRouteKey = null;
                _state.Save();
                Refresh();
            };

            _summary.SetBounds(444, 14, 700, 20);
            _summary.ForeColor = Theme.TextDim;
            _summary.Font = Theme.Small;

            header.Controls.AddRange(new Control[] { retest, pin, clearPin, _summary });

            _routes.Dock = DockStyle.Fill;
            _routes.View = View.Details;
            _routes.FullRowSelect = true;
            _routes.BackColor = Theme.Surface;
            _routes.ForeColor = Theme.Text;
            _routes.Font = Theme.Body;
            _routes.BorderStyle = BorderStyle.None;
            _routes.Columns.Add("#", 40);
            _routes.Columns.Add("Route", 330);
            _routes.Columns.Add("Ping", 90);
            _routes.Columns.Add("Jitter", 90);
            _routes.Columns.Add("Loss", 90);
            _routes.Columns.Add("Score", 90);
            _routes.Columns.Add("State", 120);

            var relayPanel = new Panel { Dock = DockStyle.Right, Width = 260, BackColor = Theme.Background, Padding = new Padding(12, 0, 0, 0) };
            var caption = Theme.Caption("RELAY POOL (unchecked relays are skipped)");
            caption.Dock = DockStyle.Top;
            caption.Height = 22;
            _relays.Dock = DockStyle.Fill;
            _relays.BackColor = Theme.Surface;
            _relays.ForeColor = Theme.Text;
            _relays.BorderStyle = BorderStyle.None;
            _relays.CheckOnClick = true;
            _relays.ItemCheck += (s, e) => BeginInvoke((Action)SaveRelayPreference);
            relayPanel.Controls.Add(_relays);
            relayPanel.Controls.Add(caption);

            Controls.Add(_routes);
            Controls.Add(relayPanel);
            Controls.Add(header);
        }

        private void LoadRelays()
        {
            _relays.Items.Clear();
            foreach (var relay in _state.Relays)
            {
                bool enabled = _state.Settings.PreferredRelayIds.Count == 0 ||
                               _state.Settings.PreferredRelayIds.Contains(relay.Id);
                _relays.Items.Add(relay, enabled);
            }
        }

        private void SaveRelayPreference()
        {
            var selected = _relays.CheckedItems.Cast<Relay>().Select(r => r.Id).ToList();
            _state.Settings.PreferredRelayIds = selected.Count == _state.Relays.Count ? new System.Collections.Generic.List<string>() : selected;
            _state.Save();
        }

        private void PinSelected()
        {
            var route = _routes.SelectedItems.Count == 0 ? null : _routes.SelectedItems[0].Tag as Route;
            if (route == null) return;
            _state.Settings.ProfileFor(_state.SelectedGame?.Id).PinnedRouteKey = route.Key;
            _state.Save();
            Refresh();
        }

        public override void Refresh()
        {
            var controller = _state.Controller;
            var pinned = _state.Settings.ProfileFor(_state.SelectedGame?.Id).PinnedRouteKey;

            _routes.BeginUpdate();
            _routes.Items.Clear();
            int index = 1;
            foreach (var route in controller.Ranked)
            {
                var item = new ListViewItem(index++.ToString()) { Tag = route };
                item.SubItems.Add(route.Label);
                item.SubItems.Add(route.Stats.Average.ToString("0.0") + " ms");
                item.SubItems.Add(route.Stats.Jitter.ToString("0.0") + " ms");
                item.SubItems.Add(route.Stats.LossPercent.ToString("0.0") + "%");
                item.SubItems.Add(route.Score.ToString("0.0"));
                var flags = "";
                if (controller.Active.Any(a => a.Key == route.Key)) flags += "active ";
                if (route.Key == pinned) flags += "pinned";
                item.SubItems.Add(flags.Trim());
                if (flags.Contains("active")) item.ForeColor = Theme.Accent;
                _routes.Items.Add(item);
            }
            _routes.EndUpdate();

            var best = controller.Ranked.FirstOrDefault();
            _summary.Text = best == null
                ? "No measurements yet. Run an optimization to rank the relay fleet."
                : string.Format("{0} routes ranked. Best: {1} at {2:0.0} ms. Active: {3}",
                    controller.Ranked.Count, best.Label, best.Stats.Average, controller.DescribeActive());
            base.Refresh();
        }

        private void Safe(Action action)
        {
            if (!IsHandleCreated || IsDisposed) return;
            try { BeginInvoke(action); } catch (Exception) { }
        }
    }
}
