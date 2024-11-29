using Answers.Dialogs;
using System.Windows;

namespace Answerable.Dialogs.Wpf
{
    public class UserDialog : IUserDialog
    {
        public async Task<bool> YesNoAsync(string message, CancellationToken ct)
        {
            var dialog = new YesNoDialog(message, ct);

            await using (ct.Register(() => dialog.Dispatcher.Invoke(() => dialog.Close())))
            {
                await dialog.Dispatcher.InvokeAsync(() => dialog.Show());
                try
                {
                    return await dialog.WaitForButtonPressAsync();
                }
                catch (TaskCanceledException)
                {
                    return false;
                }
            }
        }

        public async Task<bool> ContinueTimedOutYesNoAsync(string message, CancellationToken ct)
        {
            return await YesNoAsync(message, ct);
        }

        public bool YesNo(string message)
        {
            var tcs = new TaskCompletionSource<bool>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new YesNoDialog(message, CancellationToken.None);
                dialog.ShowDialog();
                tcs.SetResult(dialog.WaitForButtonPressAsync().Result);
            });

            return tcs.Task.Result;
        }

        public bool ContinueTimedOutYesNo(string message, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (ct.IsCancellationRequested)
            {
                return false;
            }

            using (ct.Register(() => tcs.TrySetCanceled()))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = new YesNoDialog(message, ct);

                    dialog.ShowDialog();
                    tcs.TrySetResult(dialog.WaitForButtonPressAsync().Result);
                });

                try
                {
                    return tcs.Task.Result;
                }
                catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
                {
                    return false;
                }
            }
        }

        public bool HasAsyncYesNo { get; }
        public bool HasAsyncTimeoutDialog { get; }
        public bool HasYesNo { get; }
        public bool HasTimeoutDialog { get; }
    }
}
