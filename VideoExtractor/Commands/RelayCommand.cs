using System;
using System.Windows.Input;
using JetBrains.Annotations;

namespace VideoExtractor.Commands
{
    public class RelayCommand : ICommand
    {
        [NotNull]
        private readonly Predicate<object> _canExecute;

        [NotNull]
        private readonly Action<object> _execute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public RelayCommand(Action<object> execute) : this(execute, obj => true)
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public CompositeCommand CreateBeforeCommand(ICommand beforeCommand) => new CompositeCommand(beforeCommand, this);

        public CompositeCommand CreateNextCommand(ICommand nextCommand) => new CompositeCommand(this, nextCommand);
    }
}
