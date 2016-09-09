using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Navigation;

namespace WoopsaAds
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public WoopsaAdsController controller { get; set; }

        public DiagnosticWindow diagnostic;
        public MainWindow(WoopsaAdsController controller)
        {
            InitializeComponent();
            this.controller = controller;
            ConfigWoopsa_TabControl.DataContext = this;
            diagnostic = new DiagnosticWindow();
            this.Width = 0;
            this.Height = 0;
            WindowStyle = WindowStyle.None;
            diagnostic.InitWindow();
            MyNotifyIcon.Visibility = Visibility.Visible;
        }

        public void InitWindow()
        {
            this.Show();
            this.Hide();

            this.Width = 300;
            this.MinWidth = 300;
            this.Height = 350;
            this.MinHeight = 250;
            this.WindowStyle = WindowStyle.ToolWindow;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void DoPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public bool isLocal
        {
            get
            {  
                return controller.isLocal;
            }
            set
            {
                if (value != controller.isLocal)
                {
                    if (value)
                    {
                        if (controller.plcParameterList.Count != 0 && controller.plcParameterList.ElementAt(0).adsNetId != controller.LOCAL_NET_ID ||
                            controller.plcParameterList.Count == 0)
                            controller.plcParameterList.Insert(0,new PlcParameter("local plc", controller.LOCAL_NET_ID));
                    }else
                    {
                        if (controller.plcParameterList.Count != 0 && controller.plcParameterList.ElementAt(0).adsNetId == controller.LOCAL_NET_ID)
                            controller.plcParameterList.RemoveAt(0);
                    }
                    DoPropertyChanged("plcParameterList");
                    if (controller.plcParameterList.Count != 0)
                    {
                        plcParameterListBox.SelectedItem = controller.plcParameterList.ElementAt(0);
                        plcParameterListBox.ScrollIntoView(plcParameterListBox.Items[0]);
                    }
                    controller.isLocal = value;
                    DoPropertyChanged("isLocal");
                }
            }
        }
        
        public bool runAtStartup
        {
            get{ return controller.runAtStartUp; }
            set
            {
                if (value != controller.runAtStartUp)
                {
                    controller.runAtStartUp = value;
                    DoPropertyChanged("runAtStartup");
                }
            }
        }  

        public int port
        {
            get { return controller.port; }
            set
            {
                if (value != controller.port)
                {
                    controller.port = value;
                    DoPropertyChanged("port");
                }
            }
        }

        public string folderPathWebPages
        {
            get { return controller.folderPathWebPages; }
            set
            {
                if (value != controller.folderPathWebPages)
                {
                    controller.folderPathWebPages = value;
                    DoPropertyChanged("folderPathWebPages");
                }
            }
        }

        public ObservableCollection<PlcParameter> plcParameterList
        {
            get { return controller.plcParameterList; }
            set
            {
                if (value != controller.plcParameterList)
                {
                    controller.plcParameterList = value;
                    DoPropertyChanged("plcParameterList");
                }
            }
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("WoopsaAds", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
            }
            else
            {
                registryKey.DeleteValue("WoopsaAds",false);
            }
        }

        private void ButtonApply_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            controller.Restart();
            RegisterInStartup(controller.runAtStartUp);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            controller.LoadConfig();
            DoPropertyChanged("plcParameterList");
            DoPropertyChanged("isLocal");
        }

        private bool _realyCloseWindow = false;
        public void CloseWindow()
        {
            _realyCloseWindow = true;
            this.Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_realyCloseWindow)
            {
                e.Cancel = true;
                this.Hide();
                controller.LoadConfig();
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            controller.plcParameterList.Add(new PlcParameter("", ""));
            plcParameterListBox.SelectedItem = controller.plcParameterList.Last();
            plcParameterListBox.ScrollIntoView(plcParameterListBox.Items[plcParameterListBox.Items.Count - 1]);
            DoPropertyChanged("plcParameterList");
            plcParameterListBox.Focus();
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (plcParameterListBox.SelectedItem != null)
            {
                int index = controller.plcParameterList.IndexOf((PlcParameter)plcParameterListBox.SelectedItem);
                if (!(index == 0 && isLocal))
                {
                    controller.plcParameterList.Remove((PlcParameter)plcParameterListBox.SelectedItem);
                    int nbElement = controller.plcParameterList.Count;
                    if (nbElement != 0)
                    {
                        object selectedItem = controller.plcParameterList.ElementAt(
                            index >= nbElement ? nbElement - 1 : index);
                        plcParameterListBox.SelectedItem = selectedItem;
                        plcParameterListBox.ScrollIntoView(selectedItem);
                    }
                }
                else
                    System.Windows.MessageBox.Show("You can't delete the local ADS serveur! You must uncheck the check box.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                plcParameterListBox.Focus();
                DoPropertyChanged("plcParameterList");
            }
        }

        private void plcParameterListBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                ButtonDelete_Click(sender, new RoutedEventArgs());
            }
            else if (e.Key == Key.Insert)
            {
                ButtonAdd_Click(sender, new RoutedEventArgs());
            }
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            DialogResult retVal = dlg.ShowDialog();
            if(retVal == System.Windows.Forms.DialogResult.OK)
                folderPathWebPages = dlg.SelectedPath;
        }

        private enum ConfigeTab { Woopsa = 0, Ads, Advanced, About}
        private void ConfigWoopsa_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Windows.Controls.TabControl tab = sender as System.Windows.Controls.TabControl;
            switch((ConfigeTab)tab.SelectedIndex)
            {
                case ConfigeTab.Woopsa:
                    Port_TextBox.Focus();
                    break;
                case ConfigeTab.Ads:
                    local.Focus();
                    break;
                case ConfigeTab.Advanced:
                    Browse_Button.Focus();
                    break;
                case ConfigeTab.About:
                    About_TextBlock.Focus();
                    break;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
