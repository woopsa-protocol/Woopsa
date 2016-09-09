using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WoopsaAds
{
    /// <summary>
    /// Interaction logic for DiagnosticWindow.xaml
    /// </summary>
    public partial class DiagnosticWindow : Window, INotifyPropertyChanged
    {
        private const int _MAX_LINES_DEBUG = 1000;
        private const int _MAX_EXCEPTIONS = 100;
        private static string _debug;
        public string debug
        {
            get { ScrollViewer_TextBlockDebug.ScrollToBottom(); return _debug; }
            set
            {
                if (value != _debug)
                {
                    _debug = value;
                    DoPropertyChanged("debug");
                }
            }
        }

        private ObservableCollection<DateException> _exceptionsList;
        public ObservableCollection<DateException> ExceptionsList
        {
            get { return _exceptionsList; }
            set
            {
                if (value != _exceptionsList)
                {
                    _exceptionsList = value;
                    DoPropertyChanged("ExceptionsList");
                }
            }
        }

        public void AddException(Exception e)
        {
            ExceptionsList.Add(new DateException(e, DateTime.Now));
            while (ExceptionsList.Count > _MAX_EXCEPTIONS)
            {
                ExceptionsList.RemoveAt(0);
            }
            Exceptions_ListBox.SelectedItem = ExceptionsList.Last();
            Exceptions_ListBox.ScrollIntoView(Exceptions_ListBox.Items[Exceptions_ListBox.Items.Count - 1]);
            Exceptions_ListBox.Focus();
            DoPropertyChanged("ExceptionsList");
        }

        public static void AddToDebug(string text)
        {
            _debug += "\n" + text;
            DoGlobalPropertyChanged("debug");
            if (_debug.Count(x => x == '\n') > _MAX_LINES_DEBUG)
            {
                _debug = _debug.Substring(_debug.IndexOf('\n') + 1);
            }
        }

        public DiagnosticWindow()
        {
            InitializeComponent();
            this.Diagnostic_Grid.DataContext = this;
            GlobalPropertyChanged += this.HandleGlobalPropertyChanged;
            ExceptionsList = new ObservableCollection<DateException>();
            this.Width = 0;
            this.Height = 0;
            WindowStyle = WindowStyle.None;
        }

        public void InitWindow()
        {
            this.Show();
            this.Hide();

            this.WindowStyle = WindowStyle.ToolWindow;
            this.Width = 500;
            this.Height = 400;
        }

        private static ObservableCollection<PlcStatus> _plcStatusList;
        public ObservableCollection<PlcStatus> plcStatusList
        {
            get
            {
                if (_plcStatusList != null)
                    return new ObservableCollection<PlcStatus>(_plcStatusList.OrderBy(i => i.plcName));
                return null;
            }
            set
            {
                if (value != _plcStatusList)
                {
                    _plcStatusList = value;
                    DoPropertyChanged("plcStatusList");
                }
            }
        }

        private static object _thisLock = new object();
        public static void PlcStatusChange(string name, bool status, string statusName)
        {
            for (int i = 0; i < _plcStatusList.Count; i++)
            {
                if (_plcStatusList.ElementAt(i).plcName == name)
                {
                    PlcStatus plcStatus = _plcStatusList.ElementAt(i);
                    if (plcStatus.statusName != statusName)
                    {
                        plcStatus.status = status;
                        plcStatus.statusName = statusName;
                        System.Windows.Application.Current.Dispatcher.Invoke(
                            DispatcherPriority.Send,
                            (Action)delegate ()
                            {
                                _plcStatusList.RemoveAt(i);
                            }
                        );
                        System.Windows.Application.Current.Dispatcher.Invoke(
                            DispatcherPriority.Send,
                            (Action)delegate ()
                            {
                                _plcStatusList.Insert(i, plcStatus);
                            }
                        );
                        DoGlobalPropertyChanged("plcStatusList");
                    }
                }
            }

        }

        public static void AddPlcStatus(PlcStatus plcStatus)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                    DispatcherPriority.Normal,
                    (Action)delegate ()
                    {
                        _plcStatusList.Add(plcStatus);
                    }
                );
            DoGlobalPropertyChanged("plcStatusList");
        }

        public static void ResetPlcStatus()
        {
            lock (_thisLock)
            {
                if (_plcStatusList != null)
                {
                    while (_plcStatusList.Count > 0)
                        _plcStatusList.RemoveAt(0);
                }
                else
                    _plcStatusList = new ObservableCollection<PlcStatus>();
            }
        }
        private bool _realyCloseWindow = false;
        public void CloseWindow()
        {
            _realyCloseWindow = true;
            this.Close();
        }

        #region INotifyPropertyChanged Members
        static event PropertyChangedEventHandler GlobalPropertyChanged = delegate { };
        static void DoGlobalPropertyChanged(string propertyName)
        {
            GlobalPropertyChanged(
                typeof(DiagnosticWindow),
                new PropertyChangedEventArgs(propertyName));
        }
        void HandleGlobalPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "debug":
                    DoPropertyChanged("debug");
                    break;
                case "plcStatusList":
                    DoPropertyChanged("plcStatusList");
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void DoPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_realyCloseWindow)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            TabItem item = (TabItem)Diagnostic_TabControl.SelectedItem;
            if (item.Name == "Log")
                debug = "";
            else if (item.Name == "Exceptions")
            {
                ExceptionsList = new ObservableCollection<DateException>();
            }

        }

        private void Exceptions_ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DateException ex = (DateException)(sender as ListBox).SelectedItem;
            ExceptionViewer ev = new ExceptionViewer("An unexpected error occurred in the application.", ex.exception, this);
            ev.ShowDialog();
        }
    }
}
