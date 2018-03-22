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
using System.Linq;
using System.Collections.Generic;

namespace ImageClassifier.Interfaces.GlobalUtils
{
    class FileUtils
    {
        /// <summary>
        /// Checks for the existence of a directory, creates it if neccesary
        /// </summary>
        /// <param name="path">Direcotry path on disk</param>
        public static void EnsureDirectoryExists(String path)
        {
            if(!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// List all the files that have the provided extensions.
        /// </summary>
        /// <param name="directory">Directory to search</param>
        /// <param name="extensions">A list of file extesions with or without the * character</param>
        /// <returns>List of files with the extensions in the directory</returns>
        public static IEnumerable<string> ListFile(string directory, IEnumerable<string> extensionPattern)
        {
            List<string> returnValue = new List<string>();
            if (System.IO.Directory.Exists(directory))
            {
                foreach (string extension in extensionPattern)
                {
                    String usableExtension = FileUtils.GetUsableExtension(extension);
                    foreach (String file in System.IO.Directory.GetFiles(directory, usableExtension ))
                    {
                        returnValue.Add(file);
                    }
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Delete all files that match the pattern in the provided directory.
        /// </summary>
        public static void DeleteFiles(string directory, IEnumerable<string> extensionPattern)
        {
            if (System.IO.Directory.Exists(directory))
            {
                List<string> files = new List<string>();

                if (extensionPattern != null && extensionPattern.Count() > 0 )
                {
                    foreach (string extension in extensionPattern)
                    {
                        String usableExtension = FileUtils.GetUsableExtension(extension);
                        files.AddRange(System.IO.Directory.GetFiles(directory, usableExtension));
                    }
                }
                else
                {
                    files.AddRange(System.IO.Directory.GetFiles(directory));
                }

                foreach(String file in files)
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        public static System.IO.MemoryStream GetFileStream(string fileLocation)
        {
            System.IO.MemoryStream returnStream = null;
            using (System.IO.FileStream stream = new System.IO.FileStream(fileLocation, System.IO.FileMode.Open))
            {
                returnStream = new System.IO.MemoryStream();
                stream.CopyTo(returnStream);
            }

            returnStream.Position = 0;
            return returnStream;
        }

        private static String GetUsableExtension(string extension)
        {
            String usableExtension = extension;
            if (usableExtension.StartsWith("."))
            {
                usableExtension = String.Format("*{0}", usableExtension);
            }
            return usableExtension;
        }
    }
}
