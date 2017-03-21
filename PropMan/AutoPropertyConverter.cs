using System;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections.Generic;

namespace OlegShilo.PropMan
{
    public class AutoPropertyConverter
    {
        private IVsTextManager txtMgr;

        public AutoPropertyConverter(IVsTextManager txtMgr)
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
            int currentLineNumber = startLineNumber;

            var refactor = new CSharpRefactor();

            int l = currentLineNumber;

            string originalCode = refactor.AgregateCodeBlock(() =>
                {
                    try
                    {
                        return snapshot.GetLineFromLineNumber(l++).GetText();
                    }
                    catch
                    {
                        return null;
                    }
                });
            currentLineNumber += originalCode.TrimEnd().Split('\n').Length;
            string replacementCode = "";

            CSharpRefactor.PropInfo info = refactor.ProbeAsProperty(originalCode);

            string fieldDeclarationToDelete = null;
            int newCaretLineOffset = 0;

            if (!info.IsValid)
            {
                return;
            }
            else
            {
                if (info.IsAuto)
                {
                    newCaretLineOffset = 2; //FullProperty has extra two lines (field declaration)
                    //on top of the property definition 
                    replacementCode = refactor.EmittFullProperty(info);
                }
                else if (!info.IsAuto)
                {
                    //look only above and only a primitive declarations without initialization
                    fieldDeclarationToDelete = info.AccessModifiers.Split(' ').LastOrDefault() + " " +
                                               char.ToLower(info.Name[0]) + info.Name.Substring(1) +
                                               ";";

                    //if (info.IsCompleteAndSimple)
                    replacementCode = refactor.EmittAutoProperty(info);
                    //else
                    //    WriteToOutput("Error: Only primitive get/set properties can be \"collapsed\".");
                }
            }

            if (replacementCode == "")
                return;

            //double initialStartPosition =  textView.Caret.Left;

            //replace existing property definition
            ITextEdit edit = snapshot.TextBuffer.CreateEdit();
            for (int i = startLineNumber; i < currentLineNumber; i++)
            {
                ITextSnapshotLine currentLine = snapshot.GetLineFromLineNumber(i);
                edit.Delete(currentLine.Start.Position, currentLine.LengthIncludingLineBreak);
            }


            var fieldLinesToDelete = new List<int>();
            var initialValue = "";

            //search for field in the above code
            for (int i = startLineNumber; i >= 0 && !fieldLinesToDelete.Any(); i--)
            {
                ITextSnapshotLine currentLine = snapshot.GetLineFromLineNumber(i);
                string lineText = currentLine.GetText();
                foreach (string pattern in info.FullPropFieldExpectatedPatterns)
                    if (lineText.Trim().ToLower().StartsWith(pattern))
                    {
                        fieldLinesToDelete.Add(i);

                        //int count = 0;
                        initialValue = lineText.Trim()
                                               .TrimEnd()
                                               .Substring(pattern.Length)
                                               .TrimStart();

                        if (initialValue.StartsWith("=") && initialValue.Contains(";"))
                            initialValue = " " + initialValue;
                        else
                            initialValue = "";

                        var nextLine = snapshot.GetLineFromLineNumber(i + 1);
                        var nextLineText = nextLine.GetText();
                        if (string.IsNullOrWhiteSpace(nextLineText))
                            fieldLinesToDelete.Add(i + 1);
                        break;
                    }
            }

            replacementCode = replacementCode.TrimEnd() + initialValue + Environment.NewLine;

            edit.Insert(snapshot.GetLineFromLineNumber(startLineNumber).Start.Position, replacementCode);
            edit.Apply();

            ITextSnapshotLine line = textView.TextSnapshot.GetLineFromLineNumber(startLineNumber + newCaretLineOffset);
            SnapshotPoint point = new SnapshotPoint(line.Snapshot, line.Start.Position + initialCaretXPosition);
            textView.Caret.MoveTo(point);


            if (fieldLinesToDelete.Any())
            {
                ITextEdit edit1 = snapshot.TextBuffer.CreateEdit();
                foreach (int i in fieldLinesToDelete)
                {
                    ITextSnapshotLine currentLine = snapshot.GetLineFromLineNumber(i);
                    edit1.Delete(currentLine.Start.Position, currentLine.LengthIncludingLineBreak);
                }
                edit1.Apply();
            }
        }

        private void WriteToOutput(string message)
        {
            IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            Guid generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane; // P.S. There's also the GUID_OutWindowDebugPane available.
            IVsOutputWindowPane generalPane;
            outWindow.GetPane(ref generalPaneGuid, out generalPane);

            if (generalPane != null)
            {
                generalPane.OutputString(message);
                generalPane.Activate(); // Brings this pane into view
            }
        }

        private IWpfTextView GetTextView()
        {
            return GetViewHost().TextView;
        }

        private IWpfTextViewHost GetViewHost()
        {
            object holder;
            Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
            GetUserData().GetData(ref guidViewHost, out holder);
            return (IWpfTextViewHost) holder;
        }

        private IVsUserData GetUserData()
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