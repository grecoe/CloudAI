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

namespace ImageClassifier.Interfaces.GlobalUtils
{
    class GenericCsvLogger
    {
        public string Directory { get; private set; }
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public string[] Header { get; private set; }

        private object FileLock = new object();

        public GenericCsvLogger(string directory, string file, string[] header)
        {
            this.Directory = directory;
            this.FileName = file;
            this.Header = header;

            if (!String.IsNullOrEmpty(this.Directory))
            {
                FileUtils.EnsureDirectoryExists(this.Directory);
            }

            this.FilePath = System.IO.Path.Combine(this.Directory, this.FileName);
        }

        public void Record(string[] content)
        {
            this.Record(String.Join(",", content));
        }

        public void Record(string content)
        {
            lock (FileLock)
            {
                if (!String.IsNullOrEmpty(content))
                {
                    bool exists = System.IO.File.Exists(this.FilePath);
                    using (System.IO.StreamWriter writer = new System.IO.StreamWriter(this.FilePath, true))
                    {
                        if (!exists)
                        {
                            writer.WriteLine(String.Join(",", this.Header));
                        }

                        writer.WriteLine(content);
                    }
                }
            }
        }

        public IEnumerable<string[]> GetEntries()
        {
            List<string[]> entries = new List<string[]>();
            lock(FileLock)
            {
                if(System.IO.File.Exists(this.FilePath))
                {
                    string[] content = System.IO.File.ReadAllLines(this.FilePath);
                    List<String> temp = new List<string>(content);
                    temp.RemoveAt(0);

                    foreach(String entry in temp)
                    {
                        entries.Add(entry.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }
            return entries;
        }

        public void ClearLog()
        {
            lock (FileLock)
            {
                if (System.IO.File.Exists(this.FilePath))
                {
                    System.IO.File.Delete(this.FilePath);
                }
            }
        }
    }
}
