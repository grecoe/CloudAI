using System.Collections.Generic;

namespace ImageClassifier.Interfaces
{
    public delegate void OnContainerLabelsAcquired(IEnumerable<string> containerLabels);

    interface IMultiImageDataSource : IDataSource
    {
        /// <summary>
        /// Notifiy parent controls that new labels have been acquired from the containers.
        /// </summary>
        event OnContainerLabelsAcquired OnLabelsAcquired;

        /// <summary>
        /// Collect the container labels
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetContainerLabels();

        /// <summary>
        /// Request the next item in the current container
        /// collection.
        /// </summary>
        /// <returns>SourceFile indicating information for the next item</returns>
        IEnumerable<SourceFile> NextSourceGroup();

        /// <summary>
        /// Request the previous item in the current container
        /// collection.
        /// </summary>
        /// <returns>SourceFile indicating information for the previous item</returns>
        IEnumerable<SourceFile> PreviousSourceGroup();

    }
}
