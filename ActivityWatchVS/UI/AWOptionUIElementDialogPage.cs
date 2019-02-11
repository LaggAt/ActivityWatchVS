using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ActivityWatchVS.UI
{
    public class AWOptionUIElementDialogPage : UIElementDialogPage, INotifyPropertyChanged
    {
        #region Fields

        private string _activityWatchBaseURL;
        private bool _isEnabled = true;
        private AWOptionUserControl _optionDialog;

        #endregion Fields

        #region Constructors

        public AWOptionUIElementDialogPage()
        {
            _optionDialog = new AWOptionUserControl(this);
        }

        #endregion Constructors

        #region Properties

        protected override UIElement Child
        {
            get
            {
                return _optionDialog;
            }
        }

        #endregion Properties

        #region Setting Properties

        /// <summary>
        /// Override configuration from config
        /// </summary>
        public string ActivityWatchBaseURL { get => _activityWatchBaseURL; set { _activityWatchBaseURL = value; onPropertyChanged(); } }

        /// <summary>
        /// This enables/disables sending events
        /// </summary>
        public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; onPropertyChanged(); } }

        #endregion Setting Properties

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion Events

        #region Methods

        private void onPropertyChanged([CallerMemberNameAttribute] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion Methods
    }
}