using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Service;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
{
    public static class HistoryCommands
    {
        public static readonly DelegateCommand<IHistoryManager> UndoCommand =
            CommandManager.Register<IHistoryManager>(Undo);

        public static readonly DelegateCommand<IHistoryManager> RedoCommand =
            CommandManager.Register<IHistoryManager>(Redo);

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