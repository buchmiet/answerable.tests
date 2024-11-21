using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Answers;
using Answers.Dialogs;

namespace Answerable.Dialogs.Wpf.Test;

//public partial class YesNoDialog : Window
//{
//    private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
//    public bool Result { get; private set; }

//    public YesNoDialog(string message)
//    {
//        InitializeComponent();
//        MessageTextBlock.Text = message;

//        YesButton.Click += OnYesButtonClicked;
//        NoButton.Click += OnNoButtonClicked;
//        this.Closed += OnDialogClosed;
//    }

//    public Task<bool> WaitForButtonPressAsync()
//    {
//        return _tcs.Task;
//    }

//    private void OnYesButtonClicked(object sender, RoutedEventArgs e)
//    {
//        Result = true;
//        _tcs.TrySetResult(true);
//        this.Close();
//    }

//    private void OnNoButtonClicked(object sender, RoutedEventArgs e)
//    {
//        Result = false;
//        _tcs.TrySetResult(false);
//        this.Close();
//    }

//    private void OnDialogClosed(object sender, EventArgs e)
//    {
//        if (!_tcs.Task.IsCompleted)
//        {
//            _tcs.TrySetCanceled();
//        }
//    }
//}


public partial class Testowa : IAnswerable
{
    public async Task<Answer> DoSomething()
    {
        var sb = new StringBuilder();

        // Pobranie wszystkich metod (publicznych, prywatnych, statycznych, itp.)
        var methodNames = typeof(Testowa)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName) // Pomijamy specjalne metody, np. konstruktory, gettery/settery itp.
            .Select(m => m.Name);
        sb.AppendLine("Methods: " + string.Join(", ", methodNames));

        // Pobranie wszystkich właściwości (publicznych, prywatnych, statycznych, itp.)
        var propertyNames = typeof(Testowa)
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Select(p => p.Name);
        sb.AppendLine("Properties: " + string.Join(", ", propertyNames));

        // Pobranie wszystkich pól (publicznych, prywatnych, statycznych, itp.)
        var fieldNames = typeof(Testowa)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Select(f => f.Name);
        sb.AppendLine("Fields: " + string.Join(", ", fieldNames));

        // Przykładowe wyświetlenie wyników
        string allMembers = sb.ToString();

        // Przykład użycia allMethods, np. w wyniku
           var result = TryAsync(() => DoOtherThing(),new CancellationToken());
        return Answer.Prepare($"Metody: {allMembers}");
    }

    public async Task<Answer> DoOtherThing()
    {
        return Answer.Prepare("do other thing").Error("failed");
    }
}

public class UserDialog : IUserDialog
{
    public async Task<bool> YesNoAsync(string message, CancellationToken ct)
    {
        var dialog = new YesNoDialog(message);

        using (ct.Register(() => dialog.Dispatcher.Invoke(() => dialog.Close())))
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
        // Możesz dostosować komunikat lub zachowanie, jeśli to konieczne
        return await YesNoAsync(message, ct);
    }

    public bool YesNo(string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new YesNoDialog(message);
            dialog.ShowDialog();
            tcs.SetResult(dialog.Result);
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
                var dialog = new YesNoDialog(message);

                ct.Register(() => dialog.Dispatcher.Invoke(() => dialog.Close()));

                dialog.ShowDialog();
                tcs.TrySetResult(dialog.Result);
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
}


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
         var x=new Testowa(new AnswerService());
         var resp=x.DoSomething().Result;
    }
}