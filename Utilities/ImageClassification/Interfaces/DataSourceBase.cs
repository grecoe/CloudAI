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

using ImageClassifier.Interfaces.GlobalUtils.Configuration;
using System;
using System.Collections.Generic;

namespace ImageClassifier.Interfaces
{
    /// <summary>
    /// Base class for any class that implements IDataSource. Provides base implementations of core 
    /// members and abstract implementations of anything a source must provide.
    /// </summary>
    /// <typeparam name="T">Class that identifies the configuration type used by the source.</typeparam>
    /// <typeparam name="I">Class that identifies what is kept in the internal image list</typeparam>
    abstract class DataSourceBase<T,I>: ConfigurationBase<T>, IDataSource where T:class, new() where I: class
    {
        #region Common Collection Information
        /// <summary>
        /// Index into CurrentImageList
        /// </summary>
        protected int CurrentImage { get; set; }
        /// <summary>
        /// List of files from the currently selected directory
        /// </summary>
        protected List<I> CurrentImageList { get; set; }
        #endregion

        public DataSourceBase(String name)
            :base(name)
        {
            this.CurrentImageList = new List<I>();
        }

        #region IDataSource
        public string Name { get; protected set; }

        public DataSourceType SourceType { get; protected set; }

        public bool DeleteSourceFilesWhenComplete { get; protected set; }

        public bool MultiClass { get; protected set; }

        public IConfigurationControl ConfigurationControl { get; protected set; }

        public IContainerControl ContainerControl { get; protected set; }

        public IImageControl ImageControl { get; protected set; }

        public IDataSink Sink { get; set; }

        public string CurrentContainer { get; protected set; }

        #region Abstracts
        public abstract IEnumerable<string> Containers { get; }

        public abstract int CurrentContainerIndex { get; }

        public abstract int CurrentContainerCollectionCount { get; }

        public abstract IEnumerable<string> CurrentContainerCollectionNames { get; }

        public abstract bool CanMovePrevious { get; }

        public abstract bool CanMoveNext { get; }

        public abstract void ClearSourceFiles();

        public abstract bool JumpToSourceFile(int idx);

        public abstract void SetContainer(string container);

        public abstract void UpdateSourceFile(SourceFile file);

        #endregion

        #endregion
    }
}
