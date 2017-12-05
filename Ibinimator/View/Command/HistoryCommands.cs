using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Core;
using Ibinimator.Renderer;
using Ibinimator.Renderer.Model;
using Ibinimator.ViewModel;

namespace Ibinimator.View.Command
{
    public static class HistoryCommands
    {
        public static readonly DelegateCommand<IArtContext> UndoCommand =
            CommandManager.Register<IArtContext>(Undo);

        public static readonly DelegateCommand<IArtContext> RedoCommand =
            CommandManager.Register<IArtContext>(Redo);

        private static void Redo(IArtContext artContext)
        {
            artContext.HistoryManager.Redo();
        }

        private static void Undo(IArtContext artContext)
        {
            artContext.HistoryManager.Undo();
        }
    }
}