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

namespace ImageClassifier.Interfaces
{
    /// <summary>
    /// Scored is used to pass data back and forth about a particular 
    /// item/image and it's classifications. 
    /// </summary>
    public class ScoredItem
    {
        /// <summary>
        /// Container the item was in
        /// </summary>
        public string Container { get; set; }
        /// <summary>
        /// Item identifier, could be a name, path or URL
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Applied classifications to the object (if any)
        /// </summary>
        public List<string> Classifications { get; set; }
    }

    public interface IDataSink
    {
        /// <summary>
        /// Display name of this sink
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Find an item based on it's location
        /// </summary>
        /// <param name="container">Container the item came from</param>
        /// <param name="name">Name of the item</param>
        /// <returns></returns>
        ScoredItem Find(string container, string name);
        /// <summary>
        /// Determine if an item has already been scored
        /// </summary>
        /// <param name="container">Container the item came from</param>
        /// <param name="name">Name of the item</param>
        /// <returns>True if in catalog, false otherwise</returns>
        bool ItemHasBeenScored(string container, string name);
        /// <summary>
        /// Add a catalog entry for the given item. Causes the catalog
        /// to be persisted.
        /// </summary>
        /// <param name="item">Item to record</param>
        void Record(ScoredItem item);
        /// <summary>
        /// Purges the catalog data. Use on a refresh where the new catalog items
        /// may not match teh existing catalog data.
        /// </summary>
        void Purge();
    }
}
