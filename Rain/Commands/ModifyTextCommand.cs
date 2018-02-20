using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Rain.Core;
using Rain.Core.Model.DocumentGraph;
using Rain.Core.Model.Text;

using FontStretch = Rain.Core.Model.Text.FontStretch;
using FontStyle = Rain.Core.Model.Text.FontStyle;
using FontWeight = Rain.Core.Model.Text.FontWeight;

namespace Rain.Commands
{
    public class TextInfoChange
    {
        public float? Baseline { get; set; }
        public string FontFamily { get; set; }
        public float? FontSize { get; set; }
        public FontStretch? FontStretch { get; set; }
        public FontStyle? FontStyle { get; set; }
        public FontWeight? FontWeight { get; set; }
    }

    public sealed class ModifyTextCommand : IOperationCommand<ITextContainer>
    {
        private ITextInfo _oldStyle;

        public ModifyTextCommand(long id, ITextContainer target, TextInfoChange change)
        {
            Id = id;
            Change = change;
            Time = Utility.Time.Now;
            Targets = new[] {target};
        }

        public TextInfoChange Change { get; }

        #region IOperationCommand<ITextContainer> Members

        public void Do(IArtContext artView)
        {
            foreach (var target in Targets)
            {
                _oldStyle = target.TextStyle;

                if (Change.FontFamily != null)
                    target.TextStyle.FontFamily = Change.FontFamily;

                if (Change.FontSize != null)
                    target.TextStyle.FontSize = (float) Change.FontSize;

                if (Change.FontStretch != null)
                    target.TextStyle.FontStretch = (FontStretch) Change.FontStretch;

                if (Change.FontStyle != null)
                    target.TextStyle.FontStyle = (FontStyle)Change.FontStyle;

                if (Change.FontWeight != null)
                    target.TextStyle.FontWeight = (FontWeight)Change.FontWeight;
            }
        }

        public IOperationCommand Merge(IOperationCommand newCommand)
        {
            if (newCommand is ModifyTextCommand cmd)
            {
                return new ModifyTextCommand(newCommand.Id, cmd.Targets[0], 
                        new TextInfoChange
                        {
                            FontFamily = cmd.Change.FontFamily ?? Change.FontFamily,
                            FontSize = cmd.Change.FontSize ?? Change.FontSize,
                            FontStyle = cmd.Change.FontStyle ?? Change.FontStyle,
                            FontWeight = cmd.Change.FontWeight ?? Change.FontWeight,
                            FontStretch = cmd.Change.FontStretch ?? Change.FontStretch
                        });
            }

            return null;
        }

        public void Undo(IArtContext artView)
        {
            foreach (var target in Targets)
                target.TextStyle = _oldStyle;
        }

        public string Description => "Changed format of text";

        /// <inheritdoc />
        public long Id { get; }

        /// <inheritdoc />
        public ITextContainer[] Targets { get; }

        /// <inheritdoc />
        public long Time { get; }

        /// <inheritdoc />
        object[] IOperationCommand.Targets => Targets.ToArray<object>();

        #endregion
    }
}