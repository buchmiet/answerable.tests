﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Answerable.Dialogs.Wpf.Test
{
    /// <summary>
    /// Interaction logic for YesNoDialog.xaml
    /// </summary>
    ///
    ///
    public class YesNoDialogViewModel : ObservableObject
    {
        private readonly TaskCompletionSource<bool> _tcs;
        private readonly CancellationToken _cancellationToken;

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
    }

    public partial class YesNoDialog : Window
    {
        public YesNoDialog(string message, CancellationToken cancellationToken)
        {
            InitializeComponent();
            var viewModel = new YesNoDialogViewModel(message, cancellationToken);
            this.DataContext = viewModel;

            // Zamknięcie okna po zakończeniu zadania
            viewModel.WaitForButtonPressAsync().ContinueWith(_ =>
            {
                if (!Dispatcher.CheckAccess())
                {
                    Dispatcher.Invoke(Close);
                }
                else
                {
                    Close();
                }
            });
        }

        public Task<bool> WaitForButtonPressAsync()
        {
            var viewModel = (YesNoDialogViewModel)this.DataContext;
            return viewModel.WaitForButtonPressAsync();
        }
    }
}
