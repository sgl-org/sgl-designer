using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace SglDesigner
{
    internal class EditorSnapshot
    {
        public List<SglPageData> ScreenList { get; set; } = new List<SglPageData>();
        public string SelectedScreenId { get; set; }
    }

    public class UndoManager
    {
        private Stack<string> _undoStack = new Stack<string>();
        private Stack<string> _redoStack = new Stack<string>();

        private const int MaxSteps = 50;

        public void SaveState(string currentStateJson)
        {
            if (string.IsNullOrWhiteSpace(currentStateJson))
                return;

            if (_undoStack.Count > 0 && string.Equals(_undoStack.Peek(), currentStateJson, StringComparison.Ordinal))
                return;

            _undoStack.Push(currentStateJson);
            _redoStack.Clear();

            if (_undoStack.Count > MaxSteps)
                _undoStack = new Stack<string>(_undoStack.Take(MaxSteps).Reverse());
        }

        public string Undo(string currentStateJson)
        {
            if (_undoStack.Count == 0) return null;
            _redoStack.Push(currentStateJson);
            return _undoStack.Pop();
        }

        public string Redo(string currentStateJson)
        {
            if (_redoStack.Count == 0) return null;
            _undoStack.Push(currentStateJson);
            return _redoStack.Pop();
        }
    }

    public partial class MainWindow
    {
        private UndoManager undoManager = new UndoManager();
        private static bool _isRestoringSnapshot;
        private static bool _isModelChangeRecordPending;
        private static bool _isRecordSuspended;
        private bool _isPropertyPanelEditingSession;

        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore
        };

        private string CaptureEditorSnapshot()
        {
            var snapshot = new EditorSnapshot
            {
                ScreenList = SglScreen.Instance.ScreenList?.ToList() ?? new List<SglPageData>(),
                SelectedScreenId = SglScreen.Instance.SelectedScreen?.Id
            };

            return JsonConvert.SerializeObject(snapshot, settings);
        }

        public void Record()
        {
            if (_isRecordSuspended)
                return;

            undoManager.SaveState(CaptureEditorSnapshot());
        }

        public void RecordBeforeChange()
        {
            Record();
        }

        public static bool IsRestoringSnapshot => _isRestoringSnapshot;

        public static void NotifyModelPropertyChanging(string propertyName)
        {
            if (_isRestoringSnapshot || _isModelChangeRecordPending || _isRecordSuspended)
                return;
        }

        private void BeginPropertyPanelEditSession()
        {
            if (_isPropertyPanelEditingSession || _isRecordSuspended || SglScreen.Instance.SelectedScreen?.SelectedWidgetData == null)
                return;

            _isPropertyPanelEditingSession = true;
            RecordBeforeChange();
        }

        private void EndPropertyPanelEditSession()
        {
            _isPropertyPanelEditingSession = false;
        }

        private void RestoreSnapshot(string snapshotJson)
        {
            if (string.IsNullOrWhiteSpace(snapshotJson))
                return;

            var restored = JsonConvert.DeserializeObject<EditorSnapshot>(snapshotJson, settings);
            if (restored == null)
                return;

            _isRestoringSnapshot = true;
            _isRecordSuspended = true;

            try
            {
            var screenList = new ObservableCollection<SglPageData>(restored.ScreenList ?? new List<SglPageData>());
            foreach (var screen in screenList)
            {
                foreach (var rootWidget in screen.Widgets)
                {
                    RebuildParentReferences(rootWidget);
                }
            }

            SglScreen.Instance.ScreenList = screenList;

            var selectedScreen = screenList.FirstOrDefault(x => x.Id == restored.SelectedScreenId);
            if (selectedScreen == null && screenList.Count > 0)
                selectedScreen = screenList[0];

            SglScreen.Instance.SelectedScreen = selectedScreen;
            SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();

            ClearSelection();
            RefreshCanvas(SglScreen.Instance.SelectedScreen);
            UpdateAdornerLayer();
            WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(SglScreen.Instance.SelectedScreen));
            }
            finally
            {
                _isRestoringSnapshot = false;
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    _isModelChangeRecordPending = false;
                    _isRecordSuspended = false;
                }), DispatcherPriority.ApplicationIdle);
            }
        }

        public void PerformRedo()
        {
            string current = CaptureEditorSnapshot();
            string next = undoManager.Redo(current);

            if (next != null)
            {
                RestoreSnapshot(next);
            }
        }

        public void PerformUndo()
        {
            string current = CaptureEditorSnapshot();
            string previous = undoManager.Undo(current);

            if (previous != null)
            {
                RestoreSnapshot(previous);
            }
        }
    }
}
