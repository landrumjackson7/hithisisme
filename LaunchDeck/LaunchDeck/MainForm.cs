using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LaunchDeck
{
    public class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(22, 24, 30);
        private static readonly Color Panel = Color.FromArgb(31, 34, 42);
        private static readonly Color Foreground = Color.FromArgb(228, 232, 240);
        private static readonly Color Accent = Color.FromArgb(120, 190, 255);

        private readonly Library _library;
        private readonly GameLauncher _launcher = new GameLauncher();

        private readonly FlowLayoutPanel _grid = new FlowLayoutPanel();
        private readonly TextBox _search = new TextBox();
        private readonly ComboBox _platformFilter = new ComboBox();
        private readonly ComboBox _sortBox = new ComboBox();
        private readonly CheckBox _showHidden = new CheckBox();
        private readonly Label _status = new Label();
        private readonly Label _detailTitle = new Label();
        private readonly Label _detailMeta = new Label();
        private readonly Label _detailPath = new Label();
        private readonly Button _playButton = new Button();

        private GameTile _selectedTile;

        public MainForm()
        {
            _library = Library.Load();

            Text = "LaunchDeck — every game, one library";
            BackColor = Background;
            ForeColor = Foreground;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(1120, 760);
            MinimumSize = new Size(940, 620);
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            BuildLayout();

            _launcher.Status += SetStatus;
            _launcher.SessionEnded += OnSessionEnded;

            RefreshGrid();

            if (_library.Games.Count == 0)
                SetStatus("Library is empty — press \"Scan stores\" to find installed games, or \"Add game\" for anything else.");
            else
                SetStatus(_library.Games.Count + " game(s) in the library.");
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12),
                BackColor = Background
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var header = BuildHeader();
            root.Controls.Add(header, 0, 0);
            root.SetColumnSpan(header, 2);

            _grid.Dock = DockStyle.Fill;
            _grid.AutoScroll = true;
            _grid.BackColor = Background;
            _grid.Padding = new Padding(4);
            _grid.Margin = new Padding(0, 0, 10, 0);
            root.Controls.Add(_grid, 0, 1);

            root.Controls.Add(BuildDetails(), 1, 1);

            _status.AutoSize = true;
            _status.ForeColor = Color.FromArgb(150, 158, 176);
            _status.Margin = new Padding(4, 10, 4, 0);
            root.Controls.Add(_status, 0, 2);
            root.SetColumnSpan(_status, 2);

            Controls.Add(root);
        }

        private Control BuildHeader()
        {
            var header = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Panel,
                Padding = new Padding(12, 10, 12, 10),
                Margin = new Padding(0, 0, 0, 10)
            };

            header.Controls.Add(MakeLabel("Search"));
            _search.Width = 190;
            _search.BackColor = Color.FromArgb(44, 48, 58);
            _search.ForeColor = Foreground;
            _search.BorderStyle = BorderStyle.FixedSingle;
            _search.Margin = new Padding(4, 8, 8, 0);
            _search.TextChanged += (s, e) => RefreshGrid();
            header.Controls.Add(_search);

            header.Controls.Add(MakeLabel("Store"));
            _platformFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _platformFilter.Width = 110;
            _platformFilter.Margin = new Padding(4, 6, 8, 0);
            _platformFilter.Items.Add("All");
            foreach (GamePlatform platform in Enum.GetValues(typeof(GamePlatform)))
                _platformFilter.Items.Add(platform);
            _platformFilter.SelectedIndex = 0;
            _platformFilter.SelectedIndexChanged += (s, e) => RefreshGrid();
            header.Controls.Add(_platformFilter);

            header.Controls.Add(MakeLabel("Sort by"));
            _sortBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _sortBox.Width = 110;
            _sortBox.Margin = new Padding(4, 6, 8, 0);
            foreach (GameSort sort in Enum.GetValues(typeof(GameSort)))
                _sortBox.Items.Add(sort);
            _sortBox.SelectedIndex = 0;
            _sortBox.SelectedIndexChanged += (s, e) => RefreshGrid();
            header.Controls.Add(_sortBox);

            _showHidden.Text = "Show hidden";
            _showHidden.AutoSize = true;
            _showHidden.ForeColor = Foreground;
            _showHidden.Margin = new Padding(4, 10, 8, 0);
            _showHidden.Checked = _library.ShowHidden;
            _showHidden.CheckedChanged += (s, e) =>
            {
                _library.ShowHidden = _showHidden.Checked;
                RefreshGrid();
            };
            header.Controls.Add(_showHidden);

            header.Controls.Add(MakeButton("Scan stores", ScanStores));
            header.Controls.Add(MakeButton("Add game", AddManualGame));

            return header;
        }

        private Control BuildDetails()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Panel,
                Padding = new Padding(12),
                AutoScroll = true
            };

            _detailTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _detailTitle.ForeColor = Foreground;
            _detailTitle.AutoSize = false;
            _detailTitle.Size = new Size(240, 44);
            panel.Controls.Add(_detailTitle);

            _detailMeta.ForeColor = Color.FromArgb(170, 178, 196);
            _detailMeta.AutoSize = false;
            _detailMeta.Size = new Size(240, 60);
            panel.Controls.Add(_detailMeta);

            _detailPath.ForeColor = Color.FromArgb(130, 138, 156);
            _detailPath.AutoSize = false;
            _detailPath.Size = new Size(240, 70);
            panel.Controls.Add(_detailPath);

            _playButton.Text = "Play";
            _playButton.Size = new Size(240, 38);
            _playButton.FlatStyle = FlatStyle.Flat;
            _playButton.BackColor = Accent;
            _playButton.ForeColor = Color.FromArgb(12, 20, 30);
            _playButton.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _playButton.Enabled = false;
            _playButton.Click += (s, e) => PlaySelected();
            panel.Controls.Add(_playButton);

            panel.Controls.Add(MakeWideButton("Set box art\u2026", SetArt));
            panel.Controls.Add(MakeWideButton("Toggle favourite", ToggleFavourite));
            panel.Controls.Add(MakeWideButton("Toggle hidden", ToggleHidden));
            panel.Controls.Add(MakeWideButton("Open install folder", OpenFolder));
            panel.Controls.Add(MakeWideButton("Edit\u2026", EditSelected));
            panel.Controls.Add(MakeWideButton("Remove from library", RemoveSelected));

            return panel;
        }

        private void RefreshGrid()
        {
            var selectedId = _selectedTile == null ? null : _selectedTile.Game.Id;

            _grid.SuspendLayout();
            foreach (Control control in _grid.Controls.Cast<Control>().ToList())
                control.Dispose();
            _grid.Controls.Clear();
            _selectedTile = null;

            var platform = _platformFilter.SelectedIndex <= 0
                ? (GamePlatform?)null
                : (GamePlatform)_platformFilter.SelectedItem;
            var sort = (GameSort)_sortBox.SelectedItem;

            foreach (var game in _library.Filter(_search.Text, platform, sort))
            {
                var tile = new GameTile(game);
                tile.Click += (s, e) => Select((GameTile)s);
                tile.DoubleClick += (s, e) =>
                {
                    Select((GameTile)s);
                    PlaySelected();
                };
                _grid.Controls.Add(tile);

                if (game.Id == selectedId)
                    Select(tile);
            }

            _grid.ResumeLayout();

            if (_selectedTile == null)
                Select(_grid.Controls.OfType<GameTile>().FirstOrDefault());
        }

        private void Select(GameTile tile)
        {
            if (_selectedTile != null)
                _selectedTile.Selected = false;

            _selectedTile = tile;

            if (tile == null)
            {
                _detailTitle.Text = "Nothing selected";
                _detailMeta.Text = string.Empty;
                _detailPath.Text = string.Empty;
                _playButton.Enabled = false;
                return;
            }

            tile.Selected = true;
            var game = tile.Game;
            _detailTitle.Text = game.Title;
            _detailMeta.Text = string.Join(Environment.NewLine, new[]
            {
                game.Platform.ToString(),
                game.DescribePlaytime(),
                game.DescribeLastPlayed()
            }.Where(line => !string.IsNullOrEmpty(line)));
            _detailPath.Text = game.LaunchTarget + Environment.NewLine +
                (string.IsNullOrEmpty(game.TrackProcessName)
                    ? "No tracked process — playtime will not be recorded"
                    : "Tracks " + game.TrackProcessName + ".exe");
            _playButton.Enabled = true;
        }

        private void PlaySelected()
        {
            if (_selectedTile == null)
                return;
            _launcher.Launch(_selectedTile.Game);
        }

        private void ScanStores()
        {
            var scanners = new IGameScanner[] { new SteamScanner(), new EpicScanner(), new GogScanner() };
            var messages = new List<string>();

            foreach (var scanner in scanners)
            {
                try
                {
                    var found = scanner.Scan().ToList();
                    var added = _library.Merge(found);
                    messages.Add(string.Format("{0}: {1} found, {2} new", scanner.Name, found.Count, added));
                }
                catch (Exception ex)
                {
                    messages.Add(scanner.Name + ": failed (" + ex.Message + ")");
                }
            }

            _library.Save();
            RefreshGrid();
            SetStatus(string.Join("   •   ", messages));
        }

        private void AddManualGame()
        {
            using (var dialog = new OpenFileDialog
            {
                Filter = "Games and shortcuts (*.exe;*.lnk;*.url)|*.exe;*.lnk;*.url|All files (*.*)|*.*",
                Title = "Pick the game executable"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var game = new Game
                {
                    Title = Path.GetFileNameWithoutExtension(dialog.FileName),
                    Platform = GamePlatform.Manual,
                    LaunchTarget = dialog.FileName,
                    InstallDirectory = Path.GetDirectoryName(dialog.FileName),
                    TrackProcessName = Path.GetFileNameWithoutExtension(dialog.FileName)
                };

                _library.Games.Add(game);
                _library.Save();
                RefreshGrid();
                SetStatus("Added " + game.Title + ".");
            }
        }

        private void EditSelected()
        {
            if (_selectedTile == null)
                return;

            var game = _selectedTile.Game;
            var title = Prompt("Title", game.Title);
            if (title != null)
                game.Title = title;

            var process = Prompt("Process name to track for playtime (no .exe)", game.TrackProcessName ?? string.Empty);
            if (process != null)
                game.TrackProcessName = process;

            _library.Save();
            RefreshGrid();
        }

        private void SetArt()
        {
            if (_selectedTile == null)
                return;

            using (var dialog = new OpenFileDialog
            {
                Filter = "Images (*.png;*.jpg;*.jpeg;*.webp;*.bmp)|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
                Title = "Pick box art"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                _selectedTile.Game.ArtPath = dialog.FileName;
                _selectedTile.InvalidateArt();
                _library.Save();
            }
        }

        private void ToggleFavourite()
        {
            if (_selectedTile == null)
                return;
            _selectedTile.Game.Favorite = !_selectedTile.Game.Favorite;
            _library.Save();
            RefreshGrid();
        }

        private void ToggleHidden()
        {
            if (_selectedTile == null)
                return;
            _selectedTile.Game.Hidden = !_selectedTile.Game.Hidden;
            _library.Save();
            RefreshGrid();
        }

        private void OpenFolder()
        {
            if (_selectedTile == null)
                return;

            var directory = _selectedTile.Game.InstallDirectory;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                SetStatus("No install folder recorded for this game.");
                return;
            }

            System.Diagnostics.Process.Start("explorer.exe", "\"" + directory + "\"");
        }

        private void RemoveSelected()
        {
            if (_selectedTile == null)
                return;

            var game = _selectedTile.Game;
            if (MessageBox.Show("Remove \"" + game.Title + "\" from the library? The game itself is not uninstalled.",
                    "LaunchDeck", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _library.Remove(game);
            _library.Save();
            RefreshGrid();
            SetStatus("Removed " + game.Title + ".");
        }

        private void OnSessionEnded(Game game, long seconds)
        {
            game.PlaytimeSeconds += seconds;
            game.LastPlayedUtc = DateTime.UtcNow;
            _library.Save();
            RefreshGrid();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _selectedTile != null && !_search.Focused)
            {
                PlaySelected();
                e.Handled = true;
                return;
            }

            if (e.KeyCode == Keys.F5)
            {
                ScanStores();
                e.Handled = true;
                return;
            }

            if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) && !_search.Focused)
            {
                var tiles = _grid.Controls.OfType<GameTile>().ToList();
                if (tiles.Count > 0)
                {
                    var index = _selectedTile == null ? 0 : tiles.IndexOf(_selectedTile);
                    index += e.KeyCode == Keys.Right ? 1 : -1;
                    Select(tiles[(index + tiles.Count) % tiles.Count]);
                    _grid.ScrollControlIntoView(_selectedTile);
                }
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        private void SetStatus(string message)
        {
            _status.Text = message;
        }

        private static Label MakeLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Foreground,
                Margin = new Padding(8, 11, 2, 0)
            };
        }

        private Button MakeButton(string text, Action onClick)
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 50, 62),
                ForeColor = Foreground,
                Margin = new Padding(6, 6, 0, 6)
            };
            button.Click += (s, e) => onClick();
            return button;
        }

        private Button MakeWideButton(string text, Action onClick)
        {
            var button = MakeButton(text, onClick);
            button.AutoSize = false;
            button.Size = new Size(240, 30);
            button.Margin = new Padding(0, 6, 0, 0);
            return button;
        }

        private static string Prompt(string caption, string initial)
        {
            using (var dialog = new Form
            {
                Text = "LaunchDeck",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(380, 120),
                MinimizeBox = false,
                MaximizeBox = false,
                BackColor = Background,
                ForeColor = Foreground
            })
            {
                var label = new Label { Text = caption, AutoSize = true, Location = new Point(12, 14) };
                var input = new TextBox { Text = initial, Location = new Point(12, 42), Width = 350 };
                var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(206, 76), FlatStyle = FlatStyle.Flat };
                var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(287, 76), FlatStyle = FlatStyle.Flat };

                dialog.Controls.AddRange(new Control[] { label, input, ok, cancel });
                dialog.AcceptButton = ok;
                dialog.CancelButton = cancel;

                return dialog.ShowDialog() == DialogResult.OK ? input.Text.Trim() : null;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _library.Save();
            base.OnFormClosing(e);
        }
    }
}
