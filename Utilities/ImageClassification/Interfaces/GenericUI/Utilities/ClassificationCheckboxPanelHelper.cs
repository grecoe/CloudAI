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

namespace ImageClassifier.Interfaces.GenericUI.Utilities
{
    delegate void OnForceUpdateDelegate();

    /// <summary>
    /// Manages a stack panel that is populated with Toggle Buttons
    /// </summary>
    class ClassificationCheckboxPanelHelper
    {
        /// <summary>
        /// Marks each ToggleButton in a stack panel that's label matches the classification
        /// </summary>
        public static void MakeSelection(StackPanel panel, string classification)
        {
            foreach (UIElement ui in panel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    childBox.IsChecked = String.Compare(childBox.Content.ToString(), classification) == 0;
                }
            }
        }

        /// <summary>
        /// Marks all ToggleButtons as clicked in a stack panel where teh label matches any of the classifications
        /// of the SourceFile
        /// </summary>
        public static void MakeSelection(StackPanel panel, SourceFile image)
        {
            foreach (UIElement ui in panel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    childBox.IsChecked = image.Classifications.Contains(childBox.Content.ToString());
                }
            }
        }

        /// <summary>
        /// Returns a list of strings that represent the label value of a toggle button if it is clicekd. 
        /// </summary>
        public static List<String> CollectSelections(StackPanel panel)
        {
            List<String> annotations = new List<string>();

            foreach (UIElement ui in panel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    if (childBox.IsChecked.HasValue && childBox.IsChecked.Value)
                    {
                        annotations.Add(childBox.Content.ToString());
                    }
                }
            }

            return annotations;
        }

        /// <summary>
        /// Gets a sist of ALL of the labels to ToggleButtons in a stack panel.
        /// </summary>
        public static List<String> CollectAllOptions(StackPanel panel)
        {
            List<String> annotations = new List<string>();

            foreach (UIElement ui in panel.Children)
            {
                System.Windows.Controls.Primitives.ToggleButton childBox = null;
                if ((childBox = ui as System.Windows.Controls.Primitives.ToggleButton) != null)
                {
                    annotations.Add(childBox.Content.ToString());
                }
            }

            return annotations;
        }

        /// <summary>
        /// Sets up ToggleButtons in a StackPanel
        /// </summary>
        /// <param name="source">The source that will be using the panel.</param>
        /// <param name="panel">The StackPanel to update</param>
        /// <param name="currentClassifications">List of existing global classifications to add</param>
        /// <param name="forceUpdate">A delegate that can be used to attach to each ToggleButton when the value changes.</param>
        /// <returns>A list of toggle buttons that were created for the panel.</returns>
        public static List<System.Windows.Controls.Primitives.ToggleButton> PopulateSelectionPanel(
            IDataSource source, 
            StackPanel panel,
            IEnumerable<string> currentClassifications,
            OnForceUpdateDelegate forceUpdate)
        {
            // Multiclass by default
            bool multiClass = !(source != null && !source.MultiClass);

            // The classifications
            List<String> classifications = new List<string>(currentClassifications);
            if (source is IMultiImageDataSource)
            {
                classifications.AddRange((source as IMultiImageDataSource).GetContainerLabels());
            }

            // Set up the controls on the panel
            panel.Children.Clear();
            List<System.Windows.Controls.Primitives.ToggleButton> boxes = new List<System.Windows.Controls.Primitives.ToggleButton>();
            foreach (String annotation in classifications)
            {
                System.Windows.Controls.Primitives.ToggleButton buttonX = null;
                if (multiClass)
                {
                    buttonX = new System.Windows.Controls.CheckBox();
                }
                else
                {
                    buttonX = new System.Windows.Controls.RadioButton() { GroupName = "Classifications" };
                }

                // Set up click so we can force change 
                if (forceUpdate != null)
                {
                    buttonX.Click += (o, e) => forceUpdate();
                }
                buttonX.Content = annotation;
                buttonX.Margin = new Thickness(5, 10, 5, 10);

                panel.Children.Add(buttonX);
                boxes.Add(buttonX);
            }

            return boxes;
        }
    }
}
