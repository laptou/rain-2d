using Ibinimator.ViewModel;

namespace Ibinimator.Service.Commands
{
    public static class HistoryCommands
    {
        public static readonly DelegateCommand<IHistoryManager> UndoCommand =
            new DelegateCommand<IHistoryManager>(Undo, null);

        public static readonly DelegateCommand<IHistoryManager> RedoCommand =
            new DelegateCommand<IHistoryManager>(Redo, null);

        private static void Redo(IHistoryManager historyManager)
        {
            historyManager.Redo();
        }

        private static void Undo(IHistoryManager historyManager)
        {
            historyManager.Undo();
        }
    }
}