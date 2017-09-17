using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.ViewModel;
using static Ibinimator.Service.CommandManager;

namespace Ibinimator.Service.Commands
{
    public static class HistoryCommands
    {
        public static readonly DelegateCommand<IHistoryManager> UndoCommand =
            Instance.Register<IHistoryManager>(Undo);

        public static readonly DelegateCommand<IHistoryManager> RedoCommand =
            Instance.Register<IHistoryManager>(Redo);

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