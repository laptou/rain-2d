using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ibinimator.Core;
using Ibinimator.Core.Model.DocumentGraph;

namespace Ibinimator.Commands
{
    public sealed class InsertTextCommand : LayerCommandBase<ITextLayer>
    {
        public InsertTextCommand(long id, ITextLayer target, string text, int index) : base(
            id,
            new[] {target})
        {
            Text = text;
            Index = index;
        }

        public override string Description => $@"Inserted text ""{Text}""";

        public int Index { get; }

        public string Text { get; }

        public override void Do(IArtContext artView)
        {
            foreach (var target in Targets)
                target.InsertText(Index, Text);
        }

        public override IOperationCommand Merge(IOperationCommand newCommand)
        {
            throw new InvalidOperationException("This operation is cannot be merged.");
        }

        public override void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
                target.RemoveText(Index, Text.Length);
        }
    }
}