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

namespace Answerable.Dialogs.Wpf.Test;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
///
///
///


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

public partial class MainWindow : Window
{
  


    public MainWindow()
    {
        InitializeComponent();
         var x=new Testowa(new AnswerService());
         var resp=x.DoSomething().Result;
    }
}