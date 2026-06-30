using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SglDesigner
{
    public partial class SglConfigWindow : Window
    {
        private static readonly Regex BoolRegex = new Regex(@"^\s*#cmakedefine01\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
        private static readonly Regex DefinePlaceholderRegex = new Regex(@"^\s*#define\s+(?<define>[A-Za-z_][A-Za-z0-9_]*)\s+\$\{(?<placeholder>[A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.Compiled);
        private static readonly Regex ExistingDefineRegex = new Regex(@"^\s*#define\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+(?<value>\S+)", RegexOptions.Compiled);

        private readonly string _templatePath;
        private readonly string _outputPath;
        private readonly List<string> _templateLines = new List<string>();

        public ObservableCollection<BoolSetting> BoolSettings { get; } = new ObservableCollection<BoolSetting>();
        public ObservableCollection<ValueSetting> ValueSettings { get; } = new ObservableCollection<ValueSetting>();

        public SglConfigWindow()
        {
            InitializeComponent();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _templatePath = Path.Combine(baseDir, "sgl", "cmake", "config.h.in");
            _outputPath = Path.Combine(baseDir, "simulator", "sgl_config.h");

            DataContext = this;
            //PathText.Text = $"≈‰÷√Œƒº˛Œª÷√: {_outputPath}";
            LoadTemplate();
        }

        private void LoadTemplate()
        {
            if (!File.Exists(_templatePath))
            {
                StatusText.Text = "Template file not found.";
                return;
            }

            _templateLines.Clear();
            _templateLines.AddRange(File.ReadAllLines(_templatePath));

            Dictionary<string, string> existingValues = LoadExistingValues();

            foreach (string line in _templateLines)
            {
                Match boolMatch = BoolRegex.Match(line);
                if (boolMatch.Success)
                {
                    string name = boolMatch.Groups["name"].Value;
                    BoolSettings.Add(new BoolSetting
                    {
                        Name = name,
                        IsEnabled = existingValues.TryGetValue(name, out string value) && IsTruthy(value)
                    });
                    continue;
                }

                Match defineMatch = DefinePlaceholderRegex.Match(line);
                if (defineMatch.Success)
                {
                    string defineName = defineMatch.Groups["define"].Value;
                    string placeholder = defineMatch.Groups["placeholder"].Value;
                    ValueSettings.Add(new ValueSetting
                    {
                        DefineName = defineName,
                        Placeholder = placeholder,
                        Value = existingValues.TryGetValue(defineName, out string value) ? value : GetDefaultValue(placeholder),
                        Suggestions = GetSuggestions(placeholder)
                    });
                }
            }
        }

        private Dictionary<string, string> LoadExistingValues()
        {
            var values = new Dictionary<string, string>();
            if (!File.Exists(_outputPath))
                return values;

            foreach (string line in File.ReadAllLines(_outputPath))
            {
                Match match = ExistingDefineRegex.Match(line);
                if (match.Success)
                    values[match.Groups["name"].Value] = TrimValue(match.Groups["value"].Value);
            }

            return values;
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_outputPath));

                Dictionary<string, bool> bools = BoolSettings.ToDictionary(x => x.Name, x => x.IsEnabled);
                Dictionary<string, string> values = ValueSettings.ToDictionary(
                    x => x.Placeholder,
                    x => string.IsNullOrWhiteSpace(x.Value) ? "0" : x.Value.Trim());
                var output = new List<string>(_templateLines.Count);

                foreach (string line in _templateLines)
                {
                    Match boolMatch = BoolRegex.Match(line);
                    if (boolMatch.Success)
                    {
                        string name = boolMatch.Groups["name"].Value;
                        output.Add($"#define {name} {(bools.TryGetValue(name, out bool enabled) && enabled ? 1 : 0)}");
                        continue;
                    }

                    string rendered = line;
                    foreach (Match match in Regex.Matches(line, @"\$\{(?<name>[A-Za-z_][A-Za-z0-9_]*)\}"))
                    {
                        string placeholder = match.Groups["name"].Value;
                        string value = values.TryGetValue(placeholder, out string configuredValue)
                            ? configuredValue
                            : GetDefaultValue(placeholder);
                        rendered = rendered.Replace("${" + placeholder + "}", value);
                    }

                    output.Add(rendered);
                }

                File.WriteAllLines(_outputPath, output);
                StatusText.Text = $"Generated: {_outputPath}";
                MessageBox.Show($"Config generated:\n{_outputPath}", "SGL Config", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Generate config failed: " + ex.Message, "SGL Config", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static bool IsTruthy(string value)
        {
            value = TrimValue(value);
            return value == "1"
                || value.Equals("ON", StringComparison.OrdinalIgnoreCase)
                || value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
        }

        private static string TrimValue(string value)
        {
            return (value ?? string.Empty).Trim().Trim('"').Trim('(', ')');
        }

        private static string GetDefaultValue(string placeholder)
        {
            switch (placeholder)
            {
                case "SGL_LOG_LEVEL": return "0";
                case "SGL_PANEL_PIXEL_DEPTH": return "16";
                case "SGL_EVENT_QUEUE_SIZE": return "16";
                case "SGL_SYSTICK_MS": return "10";
                case "SGL_DIRTY_AREA_THRESHOLD": return "64";
                case "SGL_HEAP_ALGO": return "lwmem";
                case "SGL_HEAP_MEMORY_SIZE": return "10240";
                case "CONFIG_SGL_QRCODE_QR_VERSION_MAX": return "5";
                default: return "0";
            }
        }

        private static ObservableCollection<string> GetSuggestions(string placeholder)
        {
            IEnumerable<string> values;
            switch (placeholder)
            {
                case "SGL_LOG_LEVEL":
                    values = new[] { "0", "1", "2", "3", "4", "5" };
                    break;
                case "SGL_PANEL_PIXEL_DEPTH":
                    values = new[] { "16", "24", "32" };
                    break;
                case "SGL_EVENT_QUEUE_SIZE":
                    values = new[] { "8", "16", "32", "64" };
                    break;
                case "SGL_SYSTICK_MS":
                    values = new[] { "1", "5", "10", "16", "20" };
                    break;
                case "SGL_DIRTY_AREA_THRESHOLD":
                    values = new[] { "16", "32", "64", "128", "256" };
                    break;
                case "SGL_HEAP_ALGO":
                    values = new[] { "lwmem", "tlsf", "bump", "other" };
                    break;
                case "SGL_HEAP_MEMORY_SIZE":
                    values = new[] { "10240", "20480", "51200", "102400", "204800" };
                    break;
                case "CONFIG_SGL_QRCODE_QR_VERSION_MAX":
                    values = new[] { "1", "5", "10", "20", "40" };
                    break;
                default:
                    values = new[] { GetDefaultValue(placeholder) };
                    break;
            }

            return new ObservableCollection<string>(values);
        }
    }

    public class BoolSetting : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string Name { get; set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ValueSetting : INotifyPropertyChanged
    {
        private string _value;

        public string DefineName { get; set; }
        public string Placeholder { get; set; }
        public ObservableCollection<string> Suggestions { get; set; }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
