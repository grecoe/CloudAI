using ImageClassifier.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Configuration
{
    // IMultiImageDataSource

    class ApplicationContext
    {
        #region Private Properties
        private IDataSource CurrentDataSource { get; set; }
        #endregion

        #region Public Properties
        /// <summary>
        /// Configuration settings from Classification.json
        /// </summary>
        public ApplicationConfiguration AppConfiguration { get; set; }

        /// <summary>
        /// The current data source provider
        /// </summary>
        public IDataSource SelectedDataSource
        {
            get { return this.CurrentDataSource; }
            set {
                this.CurrentDataSource = value;
                this.IsMultiImageDataSource = this.CurrentDataSource is IMultiImageDataSource;
            }
        }

        /// <summary>
        /// A list of all known data sources
        /// </summary>
        public List<IDataSource> DataSources { get; set; }

        /// <summary>
        /// Flag used to let it known if this is construction phase or not
        /// </summary>
        public bool IsMultiImageDataSource{ get; private set; }

        /// <summary>
        /// Flag used to let it known if this is construction phase or not
        /// </summary>
        public bool ConstructorCompleted { get; set; }
        #endregion

        public ApplicationContext()
        {
            this.AppConfiguration = ApplicationConfiguration.LoadConfiguration();
        }
    }
}
