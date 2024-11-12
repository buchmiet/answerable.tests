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

namespace Answerable.Dialogs.Wpf.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public partial class Testowa : IAnswerable
        {
            public async Task<Answer> DoSomething()
            {
                var methodNames = typeof(Testowa)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName) // Pomijamy specjalne metody, np. konstruktory, gettery/settery itp.
                    .Select(m => m.Name);

                // Tworzenie stringa zawierającego nazwy metod, oddzielone przecinkami
                string allMethods = string.Join(", ", methodNames);

                // Przykład użycia allMethods, np. w wyniku
             //   var result = TryAsync(() => DoOtherThing());
                return Answer.Prepare($"Metody: {allMethods}");
            }

            public async Task<Answer> DoOtherThing()
            {
                return Answer.Prepare("do other thing").Error("failed");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            var x=new Testowa();
            var resp=x.DoSomething().Result;
        }
    }
}