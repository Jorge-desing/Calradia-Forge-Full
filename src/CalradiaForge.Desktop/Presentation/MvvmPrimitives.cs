using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ToolkitRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand;
using ToolkitAsyncRelayCommand = CommunityToolkit.Mvvm.Input.AsyncRelayCommand;
using ToolkitParameterizedRelayCommand = CommunityToolkit.Mvvm.Input.RelayCommand<object>;

namespace CalradiaForge.Desktop.Presentation
{
    /// <summary>Compatibility surface over the official CommunityToolkit.Mvvm observable base.</summary>
    internal abstract class ObservableObject : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
            => SetProperty(ref field, value, name);

        protected void Raise([CallerMemberName] string name = null) =>
            OnPropertyChanged(name);
    }

    /// <summary>Thin parameter bridge retaining the existing desktop contracts while delegating execution to the toolkit.</summary>
    internal sealed class RelayCommand : ICommand
    {
        readonly ICommand inner;
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            inner = new ToolkitRelayCommand(execute, canExecute ?? (() => true));
        }
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            inner = new ToolkitParameterizedRelayCommand(execute, new Predicate<object>(canExecute ?? (_ => true)));
        }
        public event EventHandler CanExecuteChanged { add => inner.CanExecuteChanged += value; remove => inner.CanExecuteChanged -= value; }
        public bool CanExecute(object parameter) => inner.CanExecute(parameter);
        public void Execute(object parameter) => inner.Execute(parameter);
        public void NotifyCanExecuteChanged() => (inner as CommunityToolkit.Mvvm.Input.IRelayCommand)?.NotifyCanExecuteChanged();
    }

    internal sealed class AsyncRelayCommand : ICommand
    {
        readonly ToolkitAsyncRelayCommand inner;

        public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool> canExecute = null)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            inner = new ToolkitAsyncRelayCommand(execute, canExecute ?? (() => true));
            inner.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(IsRunning)) ExecutionStateChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public bool IsRunning => inner.IsRunning;
        public event EventHandler CanExecuteChanged { add => inner.CanExecuteChanged += value; remove => inner.CanExecuteChanged -= value; }
        public event EventHandler ExecutionStateChanged;
        public bool CanExecute(object parameter) => inner.CanExecute(parameter);
        public async void Execute(object parameter) => await ExecuteAsync().ConfigureAwait(true);

        public async Task ExecuteAsync(object parameter = null)
        {
            if (!CanExecute(parameter)) return;
            await inner.ExecuteAsync(null).ConfigureAwait(true);
        }

        public void Cancel() => inner.Cancel();
        public void NotifyCanExecuteChanged() => inner.NotifyCanExecuteChanged();
    }
}
