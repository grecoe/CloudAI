using ImageClassifier.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        /// <summary>
        /// Called from teh ISingleImageControl control
        /// </summary>
        /// <param name="file"></param>
        private void ISingleImageControlFileChanged(SourceFile file)
        {
            if (this.SelectedDataSource != null)
            {
                this.ClassificationPanelMakeSelections(file);

                this.StatusBarLocationStatus.Text =
                    String.Format("Viewing {0} of {1} ",
                    this.SelectedDataSource.CurrentContainerIndex + 1,
                    this.SelectedDataSource.CurrentContainerCollectionCount);
            }
        }
    }
}
