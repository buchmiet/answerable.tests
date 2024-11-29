using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Answerable.Dialogs.Wpf
{
    public class YesNoDialogViewModel : ObservableObject
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private readonly CancellationToken _cancellationToken;
        public event EventHandler CloseRequested;

        public YesNoDialogViewModel(string message, CancellationToken cancellationToken)
        {
            Message = message;
            _cancellationToken = cancellationToken;
            _tcs = new TaskCompletionSource<bool>();

            YesCommand = new RelayCommand(OnYes);
            NoCommand = new RelayCommand(OnNo);

            if (_cancellationToken.CanBeCanceled)
            {
                _cancellationToken.Register(() =>
                {
                    _tcs.TrySetCanceled(_cancellationToken);
                });
            }
        }

        public string Message { get; }

        public IRelayCommand YesCommand { get; }
        public IRelayCommand NoCommand { get; }

        public Task<bool> WaitForButtonPressAsync()
        {
            return _tcs.Task;
        }

        private void OnYes()
        {
            _tcs.TrySetResult(true);
        }

        private void OnNo()
        {
            _tcs.TrySetResult(false);
        }
        private void OnCloseRequested()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
