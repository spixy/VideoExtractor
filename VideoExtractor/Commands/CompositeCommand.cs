using System;
using System.Windows.Input;
using JetBrains.Annotations;

namespace VideoExtractor.Commands
{
    public class CompositeCommand : ICommand
    {
        [NotNull]
        public ICommand FirstCommand { get; }

        [NotNull]
        public ICommand SecondCommand { get; }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public CompositeCommand(ICommand firstCommand, ICommand secondCommand)
        {
            FirstCommand = firstCommand ?? throw new ArgumentNullException(nameof(firstCommand));
            SecondCommand = secondCommand ?? throw new ArgumentNullException(nameof(secondCommand));
        }

        public bool CanExecute(object parameter)
        {
            return FirstCommand.CanExecute(parameter) && SecondCommand.CanExecute(parameter);
        }

        public void Execute(object parameter)
        {
            FirstCommand.Execute(parameter);
            SecondCommand.Execute(parameter);
        }
    }
}
