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
using ImageClassifier.Interfaces.GlobalUtils;
using ImageClassifier.UIUtils;

namespace ImageClassifier.Interfaces.GenericUI
{
    /// <summary>
    /// Interaction logic for MultiImageControl.xaml
    /// </summary>
    public partial class MultiImageControl : UserControl, IMultiImageControl
    {
        public MultiImageControl(IDataSource source)
        {
            InitializeComponent();

            this.CurrentSourceBatch = new List<CurrentItem>();
            this.DataSource = source;
            this.ButtonNext.Click += (o, e) => NextBatch();
            this.ButtonPrevious.Click += (o, e) => PreviousBatch();
        }

        #region IMultiImageControl
        public event OnImageChanged ImageChanged;

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
                    Key.N,
                    ModifierKeys.Control));

                bindings.Add(new KeyBinding(
                    new ImageChangeCommand(this.ButtonPrevious, this.PreviousBatch),
                    Key.P,
                    ModifierKeys.Control));

                return bindings;
            }
        }


        public void Clear()
        {
            // TODO: Clean up whatever needs doing
            this.ImagePanel.Children.Clear();
        }

        public void FastForward()
        {
            // TODO: Does fast forward in this sense make sense?
            this.ShowNext();
        }

        public void ShowNext()
        {
            this.NextBatch();
        }

        public void UpdateClassifications(List<string> classifications)
        {
            // TODO: Does this mke sense for multi image?

            // Update the batch with the new ones, but have to make sure
            // that if they were manually changed, they stay that way :)
        }
        #endregion

        #region Navigation

        private void DisplayImages(IEnumerable<SourceFile> files)
        {
            // Move this delete off to the container changed 
            // and then wipe out any files that match teh pattern.
            // Could cause memory issues, but right now it's SUPER slow
            this.Clear();

            this.CurrentSourceBatch.Clear();
            foreach(SourceFile sFile in files)
            {
                this.CurrentSourceBatch.Add(new CurrentItem() { CurrentSource = sFile });
            }

            // Get the size of the parent
            double parentHeight = 300;
            double parentWidth = 300;
            int maxRows = this.CurrentSourceBatch.Count / 3;
            int maxCols = 3;

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
            foreach(CurrentItem item in this.CurrentSourceBatch)
            {
                MultiImageInstance instance = new MultiImageInstance(item, parentHeight, parentWidth);

                int thisRow = curRow % maxRows;
                int thisCol = curCol++ % maxCols;
                if((curCol % maxCols) == 0)
                {
                    curRow++;
                }

                Grid.SetColumn(instance, thisCol);
                Grid.SetRow(instance, thisRow);

                imageGrid.Children.Add(instance);
            }
            this.ImagePanel.Children.Add(imageGrid);



            // Update the navigation buttons
            this.ButtonNext.IsEnabled = this.DataSource.CanMoveNext;
            this.ButtonPrevious.IsEnabled = this.DataSource.CanMovePrevious;
        }

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

        
        private void NextBatch()
        {
            this.DataSource.ClearSourceFiles();
            if (this.DataSource != null &&
                this.DataSource.CanMoveNext &&
                this.DataSource is IMultiImageDataSource)
            {
                this.DisplayImages((this.DataSource as IMultiImageDataSource).NextSourceGroup());
            }
        }

        private void PreviousBatch()
        {
            this.DataSource.ClearSourceFiles();
            if (this.DataSource != null &&
                this.DataSource.CanMovePrevious &&
                this.DataSource is IMultiImageDataSource)
            {
                this.DisplayImages((this.DataSource as IMultiImageDataSource).PreviousSourceGroup());
            }
        }
        #endregion
    }
}
