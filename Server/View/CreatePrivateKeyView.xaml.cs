using System.ComponentModel;
using System.Windows;

namespace Server.View
{
    /// <summary>
    /// Interaction logic for CreatePrivateKeyView.xaml
    /// </summary>
    public partial class CreatePrivateKeyView : Window
    {
        public CreatePrivateKeyView()
        {
            InitializeComponent();
            Closing += new System.ComponentModel.CancelEventHandler(ClosingWindow);
        }

        private void ClosingWindow(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
    }
}
