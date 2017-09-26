using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ibinimator.Model;
using Ibinimator.View.Control;

namespace Ibinimator.Service.Commands
{
    public sealed class RemoveTextCommand : LayerCommandBase<ITextLayer>
    {
        public RemoveTextCommand(long id, ITextLayer target, string text, int index)
            : base(id, new[] {target})
        {
            Text = text;
            Index = index;
        }

        public override string Description => $@"Removed text ""{Text}""";

        public int Index { get; }

        public string Text { get; }

        public override void Do(ArtView artView)
        {
            foreach (var target in Targets)
                target.Remove(Index, Text.Length);
        }

        public override void Undo(ArtView artView)
        {
            foreach (var target in Targets)
                target.Insert(Index, Text);
        }
    }
}