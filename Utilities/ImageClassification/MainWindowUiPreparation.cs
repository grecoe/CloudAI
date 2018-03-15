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
using System.Windows;
using ImageClassifier.UIUtils;
using System.Windows.Input;
using System.Collections.Generic;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        private void InitializeUi(bool fullInitialization)
        {
            PopulateAnnotationsTabAnnotationsPanel();
            this.ClassificationTabImageSizeData.Text = String.Empty;

            if (fullInitialization)
            {
                this.ClassificationTabImagePanel.Children.Clear();
                this.ClassificationTabImageLabel.Text = String.Empty;
                this.ClassificationTabContainerCombo.Items.Clear();
                this.ConfigurationTabSinkLabel.Text = String.Empty;

                if (this.SelectedDataSource != null)
                {
                    if (this.SelectedDataSource != null)
                    {
                        this.ConfigurationTabSinkLabel.Text = String.Format("Sink Provider: {0}", this.SelectedDataSource.Sink.Name);
                    }

                    foreach (String file in this.SelectedDataSource.Containers)
                    {
                        this.ClassificationTabContainerCombo.Items.Add(new ContainerComboItem(this.SelectedDataSource.SourceType, file));
                    }

                    this.ClassificationTabContainerCombo.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Checks to see if this is a multi-class scenario and uses either CheckBox or RadioButton to enforce it.
        /// </summary>
        private void PopulateAnnotationsTabAnnotationsPanel()
        {
            this.InputBindings.Clear();

            bool multiClass = true;
            // Multiclass available by default
            if(this.SelectedDataSource != null && !this.SelectedDataSource.MultiClass)
            {
                multiClass = false;
            }

            this.ClassificationTabSelectionPanel.Children.Clear();
            List<System.Windows.Controls.Primitives.ToggleButton> boxes = new List<System.Windows.Controls.Primitives.ToggleButton>();
            foreach (String annotation in this.ConfigurationContext.Classifications)
            {
                System.Windows.Controls.Primitives.ToggleButton buttonX = null;
                if(multiClass)
                {
                    buttonX = new System.Windows.Controls.CheckBox();
                }
                else
                {
                    buttonX = new System.Windows.Controls.RadioButton() { GroupName = "Classifications" };
                }

                buttonX.Content = annotation;
                buttonX.Margin = new Thickness(5, 10, 5, 10);

                this.ClassificationTabSelectionPanel.Children.Add(buttonX);
                boxes.Add(buttonX);
            }

            this.PrepareAllInputBindings(boxes);
        }

        private void PrepareAllInputBindings(List<System.Windows.Controls.Primitives.ToggleButton> boxes)
        {
            this.InputBindings.Clear();

            int keyIdx = 0;
            System.Windows.Input.Key[] keys = new Key[]
            {
                System.Windows.Input.Key.NumPad1,
                System.Windows.Input.Key.NumPad2,
                System.Windows.Input.Key.NumPad3,
                System.Windows.Input.Key.NumPad4,
                System.Windows.Input.Key.NumPad5,
                System.Windows.Input.Key.NumPad6,
                System.Windows.Input.Key.NumPad7,
                System.Windows.Input.Key.NumPad8,
                System.Windows.Input.Key.NumPad9
            };

            foreach(System.Windows.Controls.Primitives.ToggleButton cb in boxes)
            {
                this.PrepareInputBindings(new ToggleButtonCommand(cb), keys[keyIdx++]);
            }

            this.PrepareInputBindings(new ImageChangeCommand(this.ClassificationTabNavigationButtonNext, this.ClassificationTabNextImage), Key.N);
            this.PrepareInputBindings(new ImageChangeCommand(this.ClassificationTabNavigationButtonPrevious, this.ClassificationTabPreviousImage), Key.P);
        }

        private void PrepareInputBindings(ICommand command, System.Windows.Input.Key key)
        {
            KeyBinding binding = new KeyBinding(command, key, ModifierKeys.Control);
            this.InputBindings.Add(binding);
        }
    }
}
