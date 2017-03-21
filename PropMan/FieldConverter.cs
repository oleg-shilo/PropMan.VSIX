using System;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace OlegShilo.PropMan
{
    public class FieldConverter
    {
        IVsTextManager txtMgr;

        public FieldConverter(IVsTextManager txtMgr)
        {
            this.txtMgr = txtMgr;
        }

        public void Execute()
        {
            IWpfTextView textView = GetTextView();

            ITextSnapshot snapshot = textView.TextSnapshot;

            if (snapshot != snapshot.TextBuffer.CurrentSnapshot)
                return;

            if (!textView.Selection.IsEmpty)
                return;

            int caretGlobalPos = textView.Caret.Position.BufferPosition.Position;
            int caretLineGlobalStartPos = textView.GetTextViewLineContainingBufferPosition(textView.Caret.Position.BufferPosition).Start.Position;
            int initialCaretXPosition = caretGlobalPos - caretLineGlobalStartPos;

            int startLineNumber = snapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition);

            string lineText = snapshot.GetLineFromLineNumber(startLineNumber).GetText();

            var refactor = new CSharpRefactor();

            string replacementCode = "";

            CSharpRefactor.FldInfo info = refactor.ProbeAsField(lineText);

            if (!info.IsValid)
            {
                return;
            }
            else
            {
                replacementCode = refactor.EmittFullProperty(info);
            }

            if (replacementCode == "")
                return;

            //double initialStartPosition =  textView.Caret.Left;

            //replace existing property definition
            ITextEdit edit = snapshot.TextBuffer.CreateEdit();
            ITextSnapshotLine currentLine = snapshot.GetLineFromLineNumber(startLineNumber);
            edit.Delete(currentLine.Start.Position, currentLine.LengthIncludingLineBreak);
            edit.Insert(snapshot.GetLineFromLineNumber(startLineNumber).Start.Position, replacementCode);
            edit.Apply();

            int caretLineOffset = 2; //shift caret to the property definition
            ITextSnapshotLine line = textView.TextSnapshot.GetLineFromLineNumber(startLineNumber + caretLineOffset);

            SnapshotPoint point = new SnapshotPoint(line.Snapshot, line.Start.Position + initialCaretXPosition);
            textView.Caret.MoveTo(point);
        }

        IWpfTextView GetTextView()
        {
            return GetViewHost().TextView;
        }

        IWpfTextViewHost GetViewHost()
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost)holder;
        }

        IVsUserData GetUserData()
        {
            int mustHaveFocus = 1;//means true
            IVsTextView currentTextView;
            txtMgr.GetActiveView(mustHaveFocus, null, out currentTextView);

            if (currentTextView is IVsUserData)
                return currentTextView as IVsUserData;
            else
                throw new ApplicationException("No text view is currently open");
        }
    }
}