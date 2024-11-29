using System.Windows;

namespace Answerable.Dialogs.Wpf
{
    public partial class YesNoDialog : Window
    {
        private readonly YesNoDialogViewModel _viewModel;
        public YesNoDialog(string message, CancellationToken cancellationToken)
        {
            InitializeComponent();
            _viewModel = new YesNoDialogViewModel(message, cancellationToken);
            this.DataContext = _viewModel;

            _viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(object sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(Close);
            }
            else
            {
                Close();
            }
        }

        public Task<bool> WaitForButtonPressAsync()
        {

            return _viewModel.WaitForButtonPressAsync();
        }
    }
}
