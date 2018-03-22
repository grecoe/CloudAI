using ImageClassifier.UIUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageClassifier.Interfaces
{
    interface IMultiImageControl : IImageControl
    {
        List<CurrentItem> CurrentSourceBatch { get; }

    }
}
