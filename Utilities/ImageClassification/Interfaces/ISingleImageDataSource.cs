using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces
{
    public interface ISingleImageDataSource : IDataSource
    {
        /// <summary>
        /// Request the next item in the current container
        /// collection.
        /// </summary>
        /// <returns>SourceFile indicating information for the next item</returns>
        SourceFile NextSourceFile();

        /// <summary>
        /// Request the previous item in the current container
        /// collection.
        /// </summary>
        /// <returns>SourceFile indicating information for the previous item</returns>
        SourceFile PreviousSourceFile();

    }
}
