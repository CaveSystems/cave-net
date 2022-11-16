using System;
using System.Collections.Generic;
using System.IO;
using Cave.IO;

namespace Cave.Mail.Imap
{
    static class ImapParser
    {
        public static string[] SplitAnswer(string answer)
        {
            var parts = new List<string>();
            var stack = new Stack<char>();
            var start = 0;
            for (var i = 0; i < answer.Length; i++)
            {
                var c = answer[i];
                switch (c)
                {
                    case '\'':
                    case '"':
                        if ((stack.Count > 0) && (stack.Peek() == c))
                        {
                            stack.Pop();
                        }
                        else
                        {
                            stack.Push(c);
                        }
                        break;

                    case ')': if (stack.Pop() != '(') { throw new FormatException(); } break;
                    case '}': if (stack.Pop() != '{') { throw new FormatException(); } break;
                    case ']': if (stack.Pop() != '[') { throw new FormatException(); } break;
                    case '>': if (stack.Pop() != '<') { throw new FormatException(); } break;

                    case '(':
                    case '{':
                    case '[':
                    case '<':
                        stack.Push(c);
                        break;
                    case ' ':
                        if (stack.Count == 0)
                        {
                            parts.Add(answer.Substring(start, i - start));
                            start = i + 1;
                            continue;
                        }
                        break;
                }
            }
            if (start < answer.Length)
            {
                parts.Add(answer.Substring(start));
            }
            return parts.ToArray();
        }

        public static ImapAnswer Parse(string id, Stream stream)
        {
            var answer = new ImapAnswer
            {
                ID = id
            };

            var buffer = new FifoStream();
            var current = new List<byte>(80);
            while (true)
            {
                var b = stream.ReadByte();
                if (b < 0)
                {
                    throw new EndOfStreamException();
                }

                current.Add((byte)b);
                if (b == '\n')
                {
                    var line = current.ToArray();
                    var str = ASCII.GetString(line);
                    if (str.StartsWith(answer.ID + " "))
                    {
                        answer.Result = str;
                        break;
                    }
                    buffer.AppendBuffer(line, 0, line.Length);
                    current.Clear();
                }
            }

            answer.Data = buffer.ToArray();
            return answer;
        }
    }
}
