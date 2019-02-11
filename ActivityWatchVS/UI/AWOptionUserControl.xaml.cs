using System;
using System.Diagnostics;
using System.Windows.Controls;

namespace ActivityWatchVS.UI
{
    /// <summary>
    /// Interaction logic for AWOptionUserControl.xaml
    /// </summary>
    public partial class AWOptionUserControl : UserControl
    {
        #region Constructors

        public AWOptionUserControl(AWOptionUIElementDialogPage model)
        {
            InitializeComponent();
            this.DataContext = model;
        }

        #endregion Constructors

        #region Methods

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            string url = e.Uri.OriginalString;

            if (url.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                Process.Start(url);
            }
            else if (url.StartsWith("shell", StringComparison.InvariantCultureIgnoreCase))
            {
                Process.Start("explorer.exe", url);
            }
            else
            {
                throw new NotImplementedException($"no method to open '{url}'.");
            }
        }

        #endregion Methods
    }
}