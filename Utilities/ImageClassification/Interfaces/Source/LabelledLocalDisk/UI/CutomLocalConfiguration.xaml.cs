using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces.Source.LabelledLocalDisk.Configuration;
using System;
using System.Windows.Controls;

namespace ImageClassifier.Interfaces.Source.LabelledLocalDisk.UI
{
    /// <summary>
    /// Interaction logic for CutomLocalConfiguration.xaml
    /// </summary>
    public partial class CutomLocalConfiguration : UserControl
    {
        #region Private Members
        private LocalSourceConfigurationUi ChildControl { get; set; }
        private LabelledLocalConfiguration Configuration { get; set; }
        #endregion

        public event OnConfigurationUpdatedHandler OnConfigurationSaved;
        public event OnUpdateSourceData OnSourceDataUpdated;

        public IDataSource Provider { get; private set; }

        public CutomLocalConfiguration(IDataSource source, LabelledLocalConfiguration configuration)
        {
            InitializeComponent();

            this.Provider = source;
            this.Configuration = configuration;

            this.ChildControl = new LocalSourceConfigurationUi(source, this.Configuration.LocalConfiguration);
            this.ChildControl.OnConfigurationSaved += ChildConfigurationSaved;
            this.ChildControl.OnSourceDataUpdated += ChildSourceUpdated;

            this.LocalSourceUiPanel.Children.Clear();
            this.LocalSourceUiPanel.Children.Add(this.ChildControl);

            int itemidx = 0;
            foreach (object entry in this.BatchSize.Items)
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
