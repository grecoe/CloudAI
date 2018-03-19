using ImageClassifier.Interfaces;
using System;
using System.Collections.Generic;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        #region Data source provider callbacks
        /// <summary>
        /// Called by a data source configuration UI to notify that the 
        /// settings have changed.
        /// </summary>
        /// <param name="sender">The provider that made the change</param>
        private void IDataSourceOnConfigurationUdpated(IDataSource sender)
        {
            // Something changed, make sure we get the annotations saved out
            this.ConfigurationContext.Classifications =
                new List<string>(this.ConfigurationTabTextAnnotationTags.Text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            this.ConfigurationContext.Save();

            this.PopulateAnnotationsTabAnnotationsPanel();

            if (sender == this.SelectedDataSource)
            {
                InitializeUi(false);
            }
        }

        /// <summary>
        /// Called by a data source configuration UI to notify that the 
        /// data for the source has changed.
        /// </summary>
        /// <param name="sender">The provider that made the change</param>
        private void IDataSourceOnSourceDataUpdated(IDataSource sender)
        {
            if (sender == this.SelectedDataSource)
            {
                InitializeUi(true);
            }
        }
        #endregion

    }
}
