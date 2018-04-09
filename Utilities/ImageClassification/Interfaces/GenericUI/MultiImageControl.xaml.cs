//
// Copyright  Microsoft Corporation ("Microsoft").
//
// Microsoft grants you the right to use this software in accordance with your subscription agreement, if any, to use software 
// provided for use with Microsoft Azure ("Subscription Agreement").  All software is licensed, not sold.  
// 
// If you do not have a Subscription Agreement, or at your option if you so choose, Microsoft grants you a nonexclusive, perpetual, 
// royalty-free right to use and modify this software solely for your internal business purposes in connection with Microsoft Azure 
// and other Microsoft products, including but not limited to, Microsoft R Open, Microsoft R Server, and Microsoft SQL Server.  
// 
// Unless otherwise stated in your Subscription Agreement, the following applies.  THIS SOFTWARE IS PROVIDED "AS IS" WITHOUT 
// WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL MICROSOFT OR ITS LICENSORS BE LIABLE 
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED 
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THE SAMPLE CODE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageClassifier.MainWindowUtilities;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for MultiImageControl.xaml
    /// </summary>
    public partial class MultiImageControl : UserControl, IMultiImageControl
    {
        #region Private Members
        private const int MAX_COLUMNS_TO_NINE = 3;
        private const int MAX_COLUMNS_MORE_THAN_NINE = 5;

        private IMultiImageDataSource MultiImageDataSource { get; set; }
        private List<MultiImageInstance> MultiImageInstanceList { get; set; }
        #endregion

        public MultiImageControl(IDataSource source)
        {
            InitializeComponent();

            this.CurrentSourceBatch = new List<CurrentItem>();
            this.DataSource = source;
            this.MultiImageDataSource = this.DataSource as IMultiImageDataSource;
            this.ButtonNext.Click += (o, e) => NextBatch();
            this.ButtonPrevious.Click += (o, e) => PreviousBatch();

            this.MultiImageInstanceList = new List<MultiImageInstance>();
            this.Classifications = new List<string>();
        }

        #region IMultiImageControl
        public event OnImageChanged ImageChanged;

        public List<String> Classifications { get; set; }

        public List<CurrentItem> CurrentSourceBatch { get; private set; }

        public UIElement ParentControl { get; set; }

        public CurrentItem CurrentSourceFile { get; private set; }

        public IDataSource DataSource { get; private set; }

        public UIElement Control { get { return this; } }

        public IEnumerable<KeyBinding> Bindings
        {
            get
            {
                List<KeyBinding> bindings = new List<KeyBinding>();
                bindings.Add(new KeyBinding(
                    new ImageChangeCommand(this.ButtonNext, this.NextBatch),
                    Key.OemPlus,
                    ModifierKeys.None));

                bindings.Add(new KeyBinding(
                    new ImageChangeCommand(this.ButtonPrevious, this.PreviousBatch),
                    Key.OemMinus,
                    ModifierKeys.None));

                return bindings;
            }
        }

        public void Clear()
        {
            this.MultiImageInstanceList.Clear();
            this.ImagePanel.Children.Clear();
        }

        public void FastForward()
        {
            if (this.DataSource.Sink != null &&
                this.DataSource.CurrentContainerCollectionCount > 0)
            {
                int imageIdx = 1;
                this.DataSource.JumpToSourceFile(imageIdx);
                foreach (String itemName in this.DataSource.CurrentContainerCollectionNames)
                {
                    if (!this.DataSource.Sink.ItemHasBeenScored(this.DataSource.CurrentContainer, itemName))
                    {
                        break;
                    }
                    imageIdx++;
                }
                this.DataSource.JumpToSourceFile(imageIdx);
            }

            this.ShowNext();
        }

        public void ResetGrid()
        {
            if(this.MultiImageInstanceList.Count > 0)
            {
                int jumpIdx = this.MultiImageDataSource.CurrentContainerIndex;

                jumpIdx = ((jumpIdx - this.MultiImageInstanceList.Count + 1) > 0)
                    ? jumpIdx - this.MultiImageInstanceList.Count
                    : 1;

                this.MultiImageDataSource.JumpToSourceFile(jumpIdx);
                this.NextBatch();
            }
        }

        public void ShowNext()
        {
            this.NextBatch();
        }

        public void UpdateClassifications(List<string> classifications)
        {
            // If there is only 1 classificaiton and it's the one of the current folder, then ignore it
            // otherwise we'll be updating them all the time.
            // If it's 0 make sure we aren't updating it to nothing.
            if(classifications.Count == 0 ||
                (classifications.Count == 1 && String.Compare(classifications[0], this.MultiImageDataSource.CurrentContainerAsClassification) == 0))
            {
                return;
            }

            // Otherwise, update EVERYTHING and save it.
            List<SourceFile> updateList = new List<SourceFile>();
            foreach(CurrentItem item in this.CurrentSourceBatch)
            {
                item.CurrentSource.Classifications.Clear();
                item.CurrentSource.Classifications.AddRange(classifications);

                updateList.Add(item.CurrentSource);
            }
            this.MultiImageDataSource.UpdateSourceBatch(updateList);


            // Now force all of the instances to update
            foreach (MultiImageInstance instance in this.MultiImageInstanceList)
            {
                instance.UpdateLabels();
            }
        }
        #endregion

        #region Navigation

        /// <summary>
        /// Used by next and previous to display a batch of files.
        /// </summary>
        private void DisplayImages(IEnumerable<SourceFile> files)
        {
            this.Clear();

            // Items are saved as they are modified, so just need to clear out 
            // what we already have and get the next batch.
            this.CurrentSourceBatch.Clear();
            foreach (SourceFile sFile in files)
            {
                this.CurrentSourceBatch.Add(new CurrentItem() { CurrentSource = sFile });
            }

            // Get the size of the parent, default it to 300
            int columnConfiguration = (this.CurrentSourceBatch.Count > 9) ? MultiImageControl.MAX_COLUMNS_MORE_THAN_NINE : MultiImageControl.MAX_COLUMNS_TO_NINE;
            int maxRows = this.CurrentSourceBatch.Count / columnConfiguration;
            int maxCols = columnConfiguration;

            // Single image case
            if(this.CurrentSourceBatch.Count == 1)
            {
                maxRows = maxCols = 1;
            }
            else if ( maxRows == 0)
            {
                maxRows = 1;
            }

            // Set up some defaults in case the UI isn't up yet
            double parentHeight = 400 / maxRows;
            double parentWidth = 600 / maxCols;
            FrameworkElement fe = this.Parent as FrameworkElement;
            if (fe != null)
            {
                parentHeight = fe.ActualHeight / maxRows;
                parentWidth = fe.ActualWidth / maxCols;
            }

            // Geneerate a grid
            Grid imageGrid = this.BuildGrid(maxRows, maxCols);

            int curRow = 0;
            int curCol = 0;
            foreach (CurrentItem item in this.CurrentSourceBatch)
            {
                MultiImageInstance instance = 
                    new MultiImageInstance(
                        this.MultiImageDataSource, 
                        item, 
                        parentHeight, 
                        parentWidth, 
                        this.Classifications);

                // Place it in the grid
                int thisRow = curRow % maxRows;
                int thisCol = curCol++ % maxCols;
                if ((curCol % maxCols) == 0)
                {
                    curRow++;
                }
                Grid.SetColumn(instance, thisCol);
                Grid.SetRow(instance, thisRow);

                // Add it to the grid
                imageGrid.Children.Add(instance);

                this.MultiImageInstanceList.Add(instance);
            }

            this.ImagePanel.Children.Add(imageGrid);

            SourceFile newFile = new SourceFile();
            newFile.Classifications.Add(this.MultiImageDataSource.CurrentContainerAsClassification);
            this.ImageChanged?.Invoke(newFile);

            // Update the navigation buttons
            this.ButtonNext.IsEnabled = this.MultiImageDataSource.CanMoveNext;
            this.ButtonPrevious.IsEnabled = this.MultiImageDataSource.CanMovePrevious;
        }

        /// <summary>
        /// Builds a WPF Grid control based on number of rows and columns
        /// </summary>
        private Grid BuildGrid(int rows, int columns)
        {
            Grid imageGrid = new Grid();

            for(int i=0;i<columns;i++)
            {
                imageGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < columns; i++)
            {
                imageGrid.RowDefinitions.Add(new RowDefinition());
            }
            return imageGrid;
        }

        /// <summary>
        /// Show the next batch of images
        /// </summary>
        private void NextBatch()
        {
            // Open wait window
            AcquireContentWindow acqWindow = this.LaunchAcquisitionWindow();

            this.MultiImageDataSource.ClearSourceFiles();

            if (this.MultiImageDataSource != null &&
                this.ContinueToImage(true))
            {
                this.DisplayImages(this.MultiImageDataSource.NextSourceGroup());
            }

            // Close wait window
            acqWindow.Close();
        }

        /// <summary>
        /// Show the previous batch of images.
        /// </summary>
        private void PreviousBatch()
        {
            // Open wait window
            AcquireContentWindow acqWindow = this.LaunchAcquisitionWindow();

            this.DataSource.ClearSourceFiles();
            if (this.MultiImageDataSource != null &&
                this.ContinueToImage(false))
            {
                this.DisplayImages(this.MultiImageDataSource.PreviousSourceGroup());
            }

            // Close wait window
            acqWindow.Close();
        }

        /// <summary>
        /// Create and launch the window to be used while collecting data from the source.
        /// </summary>
        private AcquireContentWindow LaunchAcquisitionWindow()
        {
            AcquireContentWindow contentWindow = new AcquireContentWindow();

            contentWindow.DisplayContent = String.Format("Acquiring next batch.....");

            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                contentWindow.Top = parentWindow.Top + (parentWindow.Height - contentWindow.Height) / 2;
                contentWindow.Left = parentWindow.Left + (parentWindow.Width - contentWindow.Width) / 2;
            }
            contentWindow.Show();

            return contentWindow;

        }

        /// <summary>
        /// Determines if there is anything to show. If not an instance of ImageControlNotificationDisplay is created
        /// and displayed instead.
        /// </summary>
        /// <param name="forward">Next = true, previous = false</param>
        /// <returns>True if the control should move to the next batch, false if an ImageControlNotificationDisplay was 
        /// created and displayed.</returns>
        private bool ContinueToImage(bool forward)
        {
            bool continueToImage = true;

            int totalImages = this.DataSource.CurrentContainerCollectionCount;
            ImageControlNotificationDisplay returnDisplay = null;

            if (totalImages == 0)
            {
                returnDisplay = new ImageControlNotificationDisplay(null, this.ParentControl as FrameworkElement);
            }
            else
            {
                if (forward &&
                    (this.DataSource == null || !this.DataSource.CanMoveNext))
                {
                    returnDisplay = new ImageControlNotificationDisplay("There are no more next images to display for this provider",
                        this.ParentControl as FrameworkElement);
                }

                if (!forward &&
                    (this.DataSource == null || !this.DataSource.CanMovePrevious))
                {
                    returnDisplay = new ImageControlNotificationDisplay("There are no more previous images to display for this provider",
                        this.ParentControl as FrameworkElement);
                }

            }

            if (returnDisplay != null)
            {
                this.ImagePanel.Children.Clear();
                this.ImagePanel.Children.Add(returnDisplay);
                this.ImageChanged?.Invoke(null);
                continueToImage = false;
            }

            return continueToImage;
        }
        #endregion
    }
}
