using System.Drawing;
using System.Windows.Forms;

namespace ExitClone.UI.Controls
{
    public class MetricCard : Panel
    {
        private readonly Label _value;
        private readonly Label _caption;
        private readonly Label _hint;

        public MetricCard(string caption, string hint = "")
        {
            BackColor = Theme.Surface;
            Padding = new Padding(14, 10, 14, 10);
            Size = new Size(180, 92);

            _caption = new Label
            {
                Text = caption.ToUpperInvariant(),
                ForeColor = Theme.TextDim,
                Font = Theme.Small,
                Dock = DockStyle.Top,
                Height = 18
            };
            _value = new Label
            {
                Text = "--",
                ForeColor = Theme.Text,
                Font = Theme.Metric,
                Dock = DockStyle.Top,
                Height = 38
            };
            _hint = new Label
            {
                Text = hint,
                ForeColor = Theme.TextDim,
                Font = Theme.Small,
                Dock = DockStyle.Top,
                Height = 16
            };

            Controls.Add(_hint);
            Controls.Add(_value);
            Controls.Add(_caption);
        }

        public void Set(string value, Color? color = null, string hint = null)
        {
            _value.Text = value;
            _value.ForeColor = color ?? Theme.Text;
            if (hint != null) _hint.Text = hint;
        }
    }
}
