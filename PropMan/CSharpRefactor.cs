using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OlegShilo.PropMan
{
    public class CSharpRefactor
    {
        public class PropInfo
        {
            public bool IsValid;
            public bool IsCompleteAndSimple;
            public bool IsAuto;

            public string RootIndent = "    ";

            public string Name = "";
            public string AccessModifiers = "";

            public string GetterAccessModifiers = "";
            public string SetterAccessModifiers = "";

            public string GetterBody = "";
            public string SetterBody = "";

            public string InitialValue = "";

            public bool HasGetter;
            public bool HasSetter;

            public List<string> FullPropFieldExpectatedPatterns = new List<string>();
        }

        public class FldInfo
        {
            public bool IsValid;

            public string RootIndent = "    ";

            public string Name = "";
            public string AccessModifiersAndType = "";
            public string Intitializer = "";
        }

        //an ugly state machine implementation
        public PropInfo ProbeAsProperty(string code)
        {
            PropInfo retval = new PropInfo();

            int indentLenth = code.Length - code.TrimStart().Length;
            retval.RootIndent = code.Substring(0, indentLenth);

            string[] tokens = ToSingleLine(code.Replace(";", " ;")).Split(' ');
            //string[] tokens = ToSingleLine(code).Split(' ');
            bool propStarted = false;

            int setStartedPos = -1;
            int getStartedPos = -1;
            int latstBlockEndPos = -1;

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];

                if (token == "}" || token == "{")
                {
                    if (setStartedPos == -1 && getStartedPos == -1)
                        latstBlockEndPos = i;
                }

                if (!propStarted && token == "{")
                {
                    propStarted = true;

                    if (i == 0)
                        break;

                    retval.Name = tokens[i - 1];
                    retval.AccessModifiers = tokens.ConcatItems(0, i - 2, " ").Trim();
                }
                else
                {
                    if (propStarted)
                    {
                        if (setStartedPos == -1 && (token == "set" || token == "set;"))
                        {
                            if (token == "set" && i >= tokens.Length - 1 - 1) //proper property definition should have at least one token more
                                break;

                            if (tokens[i + 1] == ";" || token == "set;") //aouto property
                            {
                                retval.HasSetter = true;
                                setStartedPos = -1;
                                retval.SetterAccessModifiers = tokens.ConcatItems(latstBlockEndPos + 1, i - 1, " ").Trim();

                                if (token == "set;")
                                    latstBlockEndPos = i;
                                else
                                    latstBlockEndPos = i + 1;

                                continue;
                            }

                            if (tokens[i + 1] != "{") //does not follow property definition layout
                                break;

                            if (i >= tokens.Length - 1 - 2) //full property definition should have at least two tokens more
                                break;

                            retval.SetterAccessModifiers = tokens.ConcatItems(latstBlockEndPos + 1, i - 1, " ").Trim();
                            setStartedPos = i + 2;
                        }
                        else if (setStartedPos != -1)
                        {
                            if (i == tokens.Length - 1) //the last token
                                break;

                            if (tokens[i + 1] == "}") //does not follow property definition layout
                            {
                                retval.HasSetter = true;
                                retval.SetterBody = tokens.ConcatItems(setStartedPos, i, " ").Replace(" ;", ";").Trim();

                                latstBlockEndPos = i + 1;
                                i++;

                                setStartedPos = -1;
                            }
                        }

                        if (getStartedPos == -1 && (token == "get" || token == "get;"))
                        {
                            if (token == "get" && i >= tokens.Length - 1 - 1) //proper property definition should have at least one token more
                                break;

                            if (tokens[i + 1] == ";" || token == "get;") //aouto property
                            {
                                retval.HasGetter = true;
                                retval.GetterAccessModifiers = tokens.ConcatItems(latstBlockEndPos + 1, i - 1, " ").Trim();
                                getStartedPos = -1;
                                if (token == "get;")
                                    latstBlockEndPos = i;
                                else
                                    latstBlockEndPos = i + 1;
                                continue;
                            }

                            if (tokens[i + 1] != "{") //does not follow property definition layout
                                break;

                            if (i >= tokens.Length - 1 - 2) //full property definition should have at least two tokens more
                                break;

                            retval.GetterAccessModifiers = tokens.ConcatItems(latstBlockEndPos + 1, i - 1, " ").Trim();
                            getStartedPos = i + 2;
                        }
                        else if (getStartedPos != -1)
                        {
                            if (i == tokens.Length - 1) //the last token
                                break;

                            if (tokens[i + 1] == "}") //does not follow property definition layout
                            {
                                retval.GetterBody = tokens.ConcatItems(getStartedPos, i, " ").Replace(" ;", ";").Trim();
                                retval.HasGetter = true;

                                latstBlockEndPos = i + 1;
                                i++;

                                getStartedPos = -1;
                            }
                        }
                    }
                }
            }

            if (retval.HasGetter && retval.HasSetter)
            {
                if (retval.GetterBody != "" && retval.SetterBody != "")
                {
                    retval.IsValid = true;
                    retval.IsAuto = false;
                    retval.IsCompleteAndSimple = true;

                    if (!retval.GetterBody.IsSingleLine() || !retval.GetterBody.Trim().StartsWith("return "))
                        retval.IsCompleteAndSimple = false;

                    if (!retval.SetterBody.IsSingleLine() || !retval.SetterBody.Trim().EndsWith("value;"))
                        retval.IsCompleteAndSimple = false;
                }
                else if (retval.GetterBody == "" && retval.SetterBody == "")
                {
                    retval.IsValid = true;
                    retval.IsAuto = true;
                    retval.IsCompleteAndSimple = true;

                    //public int Count { get; set; } = 0;
                    retval.InitialValue = code.TrimEnd().Split(new[] { '}' }, 2).LastOrDefault();
                }
                else
                {
                    retval.IsValid = false;
                }
            }
            else if (!retval.HasGetter && !retval.HasSetter)
            {
                retval.IsValid = false;
            }
            else
            {
                if (retval.HasGetter && retval.GetterBody != "")
                {
                    retval.IsValid = true;
                    retval.IsAuto = false;
                    retval.IsCompleteAndSimple = false;
                }
                else if (retval.HasGetter && retval.GetterBody != "")
                {
                    retval.IsValid = true;
                    retval.IsAuto = false;
                    retval.IsCompleteAndSimple = false;
                }
                else
                {
                    retval.IsValid = false;
                }
            }

            if (retval.IsValid && !retval.IsAuto)
            {
                var possibleAxxessors = new[] { "private ", "" };
                var possibleNamePrefixes = new[] { "", "_", "m_" };
                foreach (var accessor in possibleAxxessors)
                    foreach (var prefix in possibleNamePrefixes)
                    {
                        var pattern = retval.AccessModifiers.Replace("public ", accessor)
                                                            .Replace("internal ", accessor)
                                                            .Replace("protected ", accessor)
                                                            + " " + prefix + retval.Name;

                        retval.FullPropFieldExpectatedPatterns.Add(pattern.ToLower());
                    }
            }

            return retval;
        }

        public FldInfo ProbeAsField(string text)
        {
            string code = text.Replace(";", " ;");

            FldInfo retval = new FldInfo();

            if (code.IsSingleLine())
            {
                int indentLenth = code.Length - code.TrimStart().Length;
                retval.RootIndent = code.Substring(0, indentLenth);

                string[] tokens = ToSingleLine(code).Split(' '); //ToSingleLine also removes any extra spaces
                if (tokens.Last(0) == ";")
                {
                    // AccessModifiersAndType Name[=initializer];
                    retval.IsValid = true;
                    string[] declaration = tokens.TrimEnd(1).ToArray();

                    if (tokens.Any(x => x == "="))
                    {
                        declaration = tokens.TakeWhile(x => x != "=").ToArray();
                    }
                    retval.Name = declaration.Last();
                    retval.AccessModifiersAndType = string.Join(" ", declaration.TrimEnd(1)).TrimEnd();

                    var initExpression = tokens.Skip(declaration.Length);
                    if (initExpression.Any())
                        retval.Intitializer = string.Join(" ", initExpression.TrimEnd(1).ToArray());
                }
            }
            return retval;
        }

        public string EmittFullProperty(FldInfo fldInfo)
        {
            var propInfo = new PropInfo()
            {
                Name = fldInfo.Name[0].ToString().ToUpper() + fldInfo.Name.Substring(1),
                AccessModifiers = "public " + fldInfo.AccessModifiersAndType.Replace("private ", ""),
                InitialValue = fldInfo.Intitializer,
                RootIndent = fldInfo.RootIndent,
                HasGetter = true,
                HasSetter = true
            };

            return EmittFullProperty(propInfo);
        }

        static public string defaultFullPropTemplate =
@"${type} ${fieldName};

public ${type} ${propName}
{
    get
    {
        return ${fieldName};
    }

    set
    {
        ${fieldName} = value;
    }
}";

        static string templateFile =
                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "PropMan.VSIX",
                        "PropMan.template");

        static public string FullPropTemplate
        {
            set
            {
                try
                {
                    if (!Directory.Exists(Path.GetDirectoryName(templateFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(templateFile));

                    File.WriteAllText(templateFile, value);
                }
                catch
                {
                }
            }

            get
            {
                try
                {
                    if (File.Exists(templateFile))
                        return File.ReadAllText(templateFile);
                }
                catch
                {
                }
                return defaultFullPropTemplate;
            }
        }

        public string EmittFullProperty(PropInfo info)
        {
            if (FullPropTemplate != null)
            {
                return EmittFullPropertyTemplated(FullPropTemplate, info);
            }
            else
            {
                return EmittFullPropertyHardcoded(info);
            }
        }

        //when collapsing the full property the "field" line is not removed

        public string EmittFullPropertyTemplated(string template, PropInfo info)
        {
            var code = new StringBuilder();

            Action<int, string> AppendLine = (indent, line) =>
            {
                code.Append(info.RootIndent);

                for (int i = 0; i < indent; i++)
                    code.Append("    ");
                code.AppendLine(line);
            };

            string fieldName = info.Name[0].ToString().ToLower() + info.Name.Substring(1);

            var lines = template.Replace("\r\n", "\n").Split('\n').ToArray();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("${type}") && line.Contains("${fieldName};")) //backing field
                    lines[i] = line.Replace("${fieldName};", "${fieldName}${initValue};");
            }

            template = string.Join(Environment.NewLine, lines);

            string retval = template.Replace("${type}", info.AccessModifiers.Split(" \t".ToCharArray()).Last(0))
                                    .Replace("${fieldName}", fieldName)
                                    .Replace("${initValue}", info.InitialValue)
                                    .Replace("${propName}", info.Name)
                                    .Replace("${fieldName}", fieldName)
                                    .Replace(";;", ";"); //initValue may contain ';' as well as the template so remove duplicates

            foreach (string line in retval.Split(new string[] { "\r\n" }, StringSplitOptions.None))
                AppendLine(0, line);

            return code.ToString();
        }

        public string EmittFullPropertyHardcoded(PropInfo info)
        {
            var code = new StringBuilder();

            string fieldName = info.Name[0].ToString().ToLower() + info.Name.Substring(1);

            Action<int, string> AppendLine = (indent, line) =>
                {
                    code.Append(info.RootIndent);

                    for (int i = 0; i < indent; i++)
                        code.Append("    ");
                    code.AppendLine(line);
                };

            AppendLine(0, info.AccessModifiers.Replace("public ", "")
                                              .Replace("internal ", "")
                                              .Replace("protected ", "")
                                               + " " + fieldName + ";");
            code.AppendLine();
            AppendLine(0, info.AccessModifiers + " " + info.Name);
            AppendLine(0, "{");
            if (info.HasGetter)
            {
                AppendLine(1, info.GetterAccessModifiers == "" ? "get" : info.GetterAccessModifiers + " get");
                AppendLine(1, "{");
                AppendLine(2, "return " + fieldName + ";");
                AppendLine(1, "}");
            }
            if (info.HasSetter)
            {
                AppendLine(1, info.SetterAccessModifiers == "" ? "set" : info.SetterAccessModifiers + " set");
                AppendLine(1, "{");
                AppendLine(2, fieldName + " = value;");
                AppendLine(1, "}");
            }
            AppendLine(0, "}");

            return code.ToString();
        }

        public string EmittAutoProperty(PropInfo info)
        {
            var code = new StringBuilder();

            Action<string> Append = (token) =>
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        code.Append(token);
                        code.Append(" ");
                    }
                };

            code.Append(info.RootIndent);
            Append(info.AccessModifiers.Replace("private", "public")
                                       .Replace("internal", "public")
                                       .Replace("protected", "public"));

            Append(info.Name);
            Append("{");
            Append(info.GetterAccessModifiers);
            Append("get;");
            Append(info.SetterAccessModifiers);
            Append("set;");
            Append("}");
            code.AppendLine();

            return code.ToString();
        }

        public string AgregateCodeBlock(Func<string> getLineDelegate)
        {
            var retval = new StringBuilder();

            int openBracketsCount = 0;
            string line = getLineDelegate();
            bool bracketsDetected = false;
            do
            {
                foreach (char c in (line ?? "").ToCharArray())
                    if (c == '{')
                    {
                        bracketsDetected = true;
                        openBracketsCount++;
                    }
                    else if (c == '}')
                    {
                        bracketsDetected = true;
                        openBracketsCount--;
                    }

                retval.AppendLine(line);
                line = getLineDelegate();
            }
            while (line != null && (!bracketsDetected || openBracketsCount != 0));

            return retval.ToString();
        }

        string ToSingleLine(string code)
        {
            var retval = new StringBuilder();

            char prevAggregatedChar = ' '; //it is an equivalent of TrimStart()
            foreach (char c in code.ToCharArray())
            {
                char charToAnalyse = c;
                if (charToAnalyse == '\n' || charToAnalyse == '\r' || charToAnalyse == '\t')
                    charToAnalyse = ' ';

                if (charToAnalyse == ' ' && prevAggregatedChar == ' ')
                    continue;

                retval.Append(charToAnalyse);
                prevAggregatedChar = charToAnalyse;
            }

            return retval.ToString();
        }
    }

    static class Extensions
    {
        static public T Last<T>(this T[] collection, int index) where T : class
        {
            return collection[collection.Length - index - 1];
        }

        static public IEnumerable<T> TrimEnd<T>(this IEnumerable<T> collection, int count) where T : class
        {
            return collection.Take(collection.Count() - count).ToArray();
        }

        static public string ConcatItems(this string[] collection, int startIndex, int endIndex, string newDelimiter)
        {
            var retval = new StringBuilder();

            for (int i = startIndex; i <= endIndex; i++)
            {
                retval.Append(collection[i]);
                retval.Append(newDelimiter);
            }
            return retval.ToString();
        }

        static public bool IsSingleLine(this string text)
        {
            return text.IndexOfAny("\n\r".ToCharArray()) == -1;
        }
    }
}