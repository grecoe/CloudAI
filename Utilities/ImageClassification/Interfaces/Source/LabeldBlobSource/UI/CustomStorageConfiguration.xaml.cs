using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.Source.LabeldBlobSource.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ImageClassifier.Interfaces.Source.LabeldBlobSource.UI
{
    /// <summary>
    /// Interaction logic for CustomStorageConfiguration.xaml
    /// </summary>
    public partial class CustomStorageConfiguration : UserControl
    {
        #region Private Members
        private AzureStorageConfigurationUi ChildControl { get; set; }
        private LabelledBlobSourceConfiguration Configuration { get; set; }
        #endregion

        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;

        public IDataSource Provider { get; private set; }

        public CustomStorageConfiguration(IDataSource source, LabelledBlobSourceConfiguration config)
        {
            InitializeComponent();

            this.Provider = source;
            this.Configuration = config;

            this.ChildControl = new AzureStorageConfigurationUi(source, config.StorageConfiguration);
            this.ChildControl.OnConfigurationSaved += ChildConfigurationSaved;
            this.ChildControl.OnSourceDataUpdated += ChildSourceUpdated;

            this.StorageConfigUiPanel.Children.Clear();
            this.StorageConfigUiPanel.Children.Add(this.ChildControl);

            int itemidx = 0;
            foreach(object entry in this.BatchSize.Items)
            {
                if (entry is ComboBoxItem)
                {
                    if (String.Compare((entry as ComboBoxItem).Content.ToString(), this.Configuration.BatchSize.ToString()) == 0)
                    {
                        this.BatchSize.SelectedIndex = itemidx;
                        break;
                    }
                }
                itemidx++;
            }
        }

        private void ChildSourceUpdated(IDataSource source)
        {
            this.OnSourceDataUpdated?.Invoke(source);
        }

        private void ChildConfigurationSaved(IDataSource source)
        {
            this.Configuration.BatchSize = int.Parse((this.BatchSize.SelectedItem as ComboBoxItem).Content.ToString());
            this.OnConfigurationSaved?.Invoke(source);
        }
    }
}
