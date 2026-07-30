using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace FrameSight
{
    /// <summary>
    /// Appends readings to a CSV so a session can be graphed afterwards. The header is
    /// written from the first sample's sensor set; if the user changes sensors mid-run
    /// a new file is started rather than writing ragged rows.
    /// </summary>
    public class SessionRecorder : IDisposable
    {
        private readonly string _directory;
        private StreamWriter _writer;
        private List<SensorKind> _columns;
        private DateTime _startedUtc;

        public SessionRecorder(string directory)
        {
            _directory = directory;
        }

        public string CurrentFile { get; private set; }

        public int RowCount { get; private set; }

        public void Write(IList<Reading> readings)
        {
            if (readings == null || readings.Count == 0)
                return;

            var columns = readings.Select(r => r.Kind).ToList();
            if (_writer == null || !columns.SequenceEqual(_columns))
                Start(columns);

            var cells = new List<string> { ((DateTime.UtcNow - _startedUtc).TotalSeconds).ToString("0.0", CultureInfo.InvariantCulture) };
            cells.Add(DateTime.Now.ToString("HH:mm:ss"));
            cells.AddRange(readings.Select(NumericOrText));

            _writer.WriteLine(string.Join(",", cells));
            _writer.Flush();
            RowCount++;
        }

        /// <summary>Strips units so spreadsheets treat values as numbers where possible.</summary>
        private static string NumericOrText(Reading reading)
        {
            var value = reading.Value ?? string.Empty;

            // Clock and uptime read as digits but are not quantities.
            if (value.IndexOf(':') >= 0)
                return "\"" + value + "\"";

            var numeric = new string(value.TakeWhile(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

            double parsed;
            if (numeric.Length > 0 && double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                return numeric;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void Start(List<SensorKind> columns)
        {
            Stop();

            Directory.CreateDirectory(_directory);
            _columns = columns;
            _startedUtc = DateTime.UtcNow;
            RowCount = 0;
            CurrentFile = Path.Combine(_directory,
                "framesight-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".csv");

            _writer = new StreamWriter(CurrentFile, false, Encoding.UTF8);
            _writer.WriteLine("elapsed_s,wall_clock," + string.Join(",", columns.Select(c => c.ToString())));
        }

        public void Stop()
        {
            if (_writer == null)
                return;
            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
