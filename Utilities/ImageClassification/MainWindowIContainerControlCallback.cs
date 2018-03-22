using ImageClassifier.Interfaces;
using ImageClassifier.Interfaces.GenericUI;
using System;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        private void IContainerControlContainerChanged(IContainerControl source, object container)
        {
            if(this.SelectedDataSource == null || this.SelectedDataSource.ImageControl == null)
            {
                return; 
            }

            this.SelectedDataSource.ImageControl.Clear();

            // Get the container label, whatever it is
            string containerLabel = String.Empty;
            if (container is ContainerComboItem)
            {
                containerLabel = (container as ContainerComboItem).ToString();
            }

            if (!String.IsNullOrEmpty(containerLabel))
            {
                this.StatusBarCollection.Text = containerLabel;
            }

            // Catch whatever we need for the current image.
            this.ForceClassificationUpdate();

            // Move to the first un-tagged item
            this.SelectedDataSource.ImageControl.FastForward();
        }
    }
}
