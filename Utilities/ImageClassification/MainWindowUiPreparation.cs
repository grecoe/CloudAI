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

using ImageClassifier.UIUtils;
using System.Windows.Input;
using System.Collections.Generic;
using ImageClassifier.Interfaces.GenericUI;
using ImageClassifier.Interfaces;

namespace ImageClassifier
{
    public partial class MainWindow
    {
        private void InitializeUi(bool fullInitialization)
        {
            PopulateAnnotationsTabAnnotationsPanel();

            if (fullInitialization)
            {
                if (this.SelectedDataSource != null)
                {
                    // Set up the IImageControl 
                    this.ImagePanel.Children.Clear();
                    this.SelectedDataSource.ImageControl.ParentControl = this.ImagePanel;
                    this.ImagePanel.Children.Add(this.SelectedDataSource.ImageControl.Control);

                    // Set up the IContainerControl 
                    this.ContainerPanel.Children.Clear();
                    this.ContainerPanel.Children.Add(this.SelectedDataSource.ContainerControl.Control);

                    if(this.SelectedDataSource is IMultiImageDataSource)
                    {
                        this.IMultiImageControlGroupChanged(null);
                    }
                }
            }
        }

        /// <summary>
        /// Populate the panel with the selections for classifications and then hook all of the key bindings
        /// so it can be made with shortcuts.
        /// </summary>
        private void PopulateAnnotationsTabAnnotationsPanel()
        {
            // Clear current bindings
            this.InputBindings.Clear();

            // Set up all the boxes
            List<System.Windows.Controls.Primitives.ToggleButton> boxes = ClassificationCheckboxPanelHelper.PopulateSelectionPanel(
                this.SelectedDataSource,
                this.ClassificationTabSelectionPanel,
                this.ConfigurationContext.Classifications,
                this.ForceClassificationUpdate);

            // Now set up all of the key bindings 
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
                ToggleButtonCommand cmd = new ToggleButtonCommand(cb);
                cmd.ClassificationsChanged += this.ForceClassificationUpdate;
                this.PrepareInputBindings(cmd, keys[keyIdx++]);
            }

            if (this.SelectedDataSource != null && this.SelectedDataSource.ImageControl != null)
            {
                foreach (KeyBinding binding in this.SelectedDataSource.ImageControl.Bindings)
                {
                    this.InputBindings.Add(binding);
                }
            }
        }

        private void PrepareInputBindings(ICommand command, System.Windows.Input.Key key)
        {
            KeyBinding binding = new KeyBinding(command, key, ModifierKeys.Control);
            this.InputBindings.Add(binding);
        }
    }
}
