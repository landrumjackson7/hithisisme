using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ExitClone.Core;

namespace ExitClone.UI.Pages
{
    public class GamesPage : UserControl
    {
        private readonly AppState _state;
        private readonly TextBox _search = new TextBox();
        private readonly ListView _list = new ListView();
        private readonly CheckBox _favoritesOnly = new CheckBox();

        public event EventHandler GameActivated;

        public GamesPage(AppState state)
        {
            _state = state;
            Dock = DockStyle.Fill;
            BackColor = Theme.Background;
            Padding = new Padding(18);
            Build();
            Reload();
            _state.GamesChanged += (s, e) => BeginInvoke((Action)Reload);
        }

        private void Build()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.Background };

            _search.SetBounds(0, 14, 320, 26);
            _search.BackColor = Theme.SurfaceAlt;
            _search.ForeColor = Theme.Text;
            _search.BorderStyle = BorderStyle.FixedSingle;
            _search.Font = Theme.Body;
            _search.TextChanged += (s, e) => Reload();

            _favoritesOnly.SetBounds(336, 16, 120, 24);
            _favoritesOnly.Text = "Favourites only";
            _favoritesOnly.ForeColor = Theme.TextDim;
            _favoritesOnly.Font = Theme.Body;
            _favoritesOnly.FlatStyle = FlatStyle.Flat;
            _favoritesOnly.CheckedChanged += (s, e) => Reload();

            var add = Theme.GhostButton("Add custom game");
            add.SetBounds(470, 14, 150, 28);
            add.Click += (s, e) => AddCustom();

            var remove = Theme.GhostButton("Remove");
            remove.SetBounds(628, 14, 100, 28);
            remove.Click += (s, e) =>
            {
                var game = Selected();
                if (game == null) return;
                if (!game.Custom)
                {
                    MessageBox.Show("Built-in titles cannot be removed.", "ExitClone");
                    return;
                }
                _state.RemoveGame(game);
            };

            var favorite = Theme.GhostButton("Toggle favourite");
            favorite.SetBounds(736, 14, 140, 28);
            favorite.Click += (s, e) =>
            {
                var game = Selected();
                if (game == null) return;
                game.Favorite = !game.Favorite;
                _state.PersistGames();
                Reload();
            };

            header.Controls.AddRange(new Control[] { _search, _favoritesOnly, add, remove, favorite });

            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.OwnerDraw = false;
            _list.BackColor = Theme.Surface;
            _list.ForeColor = Theme.Text;
            _list.Font = Theme.Body;
            _list.BorderStyle = BorderStyle.None;
            _list.Columns.Add("", 30);
            _list.Columns.Add("Game", 320);
            _list.Columns.Add("Publisher", 200);
            _list.Columns.Add("Regions", 240);
            _list.Columns.Add("Process", 220);
            _list.DoubleClick += (s, e) => Activate(Selected());

            var use = Theme.PrimaryButton("Use selected game");
            use.Dock = DockStyle.Bottom;
            use.Click += (s, e) => Activate(Selected());

            Controls.Add(_list);
            Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 10, BackColor = Theme.Background });
            Controls.Add(use);
            Controls.Add(header);
        }

        private Game Selected()
        {
            return _list.SelectedItems.Count == 0 ? null : _list.SelectedItems[0].Tag as Game;
        }

        private void Activate(Game game)
        {
            if (game == null) return;
            _state.Select(game);
            GameActivated?.Invoke(this, EventArgs.Empty);
        }

        private void Reload()
        {
            var filter = _search.Text.Trim();
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var game in _state.Games
                         .Where(g => string.IsNullOrEmpty(filter) || g.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                         .Where(g => !_favoritesOnly.Checked || g.Favorite))
            {
                var item = new ListViewItem(game.Favorite ? "*" : "")
                {
                    Tag = game,
                    ForeColor = Theme.Text
                };
                item.SubItems.Add(game.Name);
                item.SubItems.Add(game.Publisher ?? "");
                item.SubItems.Add(string.Join(", ", game.Servers.Select(s => s.Name)));
                item.SubItems.Add(string.Join(", ", game.ProcessNames));
                _list.Items.Add(item);
            }
            _list.EndUpdate();
        }

        private void AddCustom()
        {
            using (var dialog = new CustomGameDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                _state.AddGame(dialog.Result);
            }
        }
    }

    public class CustomGameDialog : Form
    {
        private readonly TextBox _name = new TextBox();
        private readonly TextBox _process = new TextBox();
        private readonly TextBox _host = new TextBox();
        private readonly TextBox _port = new TextBox { Text = "443" };

        public Game Result { get; private set; }

        public CustomGameDialog()
        {
            Text = "Add custom game";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ClientSize = new Size(420, 240);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            Font = Theme.Body;

            AddRow("Game name", _name, 16);
            AddRow("Process name (no .exe)", _process, 60);
            AddRow("Server host or IP", _host, 104);
            AddRow("Server port", _port, 148);

            var ok = Theme.PrimaryButton("Add");
            ok.SetBounds(220, 192, 90, 32);
            ok.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_host.Text))
                {
                    MessageBox.Show("Name and server host are required.", "ExitClone");
                    return;
                }
                int port;
                if (!int.TryParse(_port.Text, out port)) port = 443;
                Result = new Game
                {
                    Id = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = _name.Text.Trim(),
                    Publisher = "Custom",
                    Custom = true,
                    ProcessNames = { _process.Text.Trim() },
                    Servers = { new GameServer { Name = "Custom", Region = "Custom", Host = _host.Text.Trim(), Port = port } }
                };
                DialogResult = DialogResult.OK;
            };

            var cancel = Theme.GhostButton("Cancel");
            cancel.SetBounds(316, 192, 90, 32);
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(ok);
            Controls.Add(cancel);
        }

        private void AddRow(string caption, TextBox box, int y)
        {
            var label = Theme.Caption(caption);
            label.Location = new Point(16, y);
            box.SetBounds(16, y + 18, 390, 24);
            box.BackColor = Theme.SurfaceAlt;
            box.ForeColor = Theme.Text;
            box.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(label);
            Controls.Add(box);
        }
    }
}
