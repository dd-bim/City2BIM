using System;
using System.Windows.Input;

namespace IFCGeoRefCheckerGUI
{
    public class DelegateCommand : ICommand
    {
        readonly Action<object>? execute;
        readonly Predicate<object>? canExecute;

        public DelegateCommand(Action<object>? execute, Predicate<object>? canExecute)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public DelegateCommand(Action<object> excute) : this(excute, null) { }

        public event EventHandler? CanExecuteChanged;

        public void RaiseCanExecuteChanged() => this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public bool CanExecute(object? parameter) => this.canExecute?.Invoke(parameter!) ?? true;

        public void Execute(object? parameter)
        {
            this.execute?.Invoke(parameter!);
        }
    }
}
