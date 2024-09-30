using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cave.Collections;

namespace Cave.Mail.Imap;

/// <summary>Provides a class for imap message sequence numbers.</summary>
public sealed class ImapNumberSequence : IEnumerable<int>
{
    #region Private Fields

    static readonly char[] NumbersSeparator = [' ', ','];

    readonly int[] numbers;

    #endregion Private Fields

    #region Private Methods

    IEnumerator<int> GetDefaultEnumerator()
    {
        IEnumerable<int> GetRange() => new Counter(FirstNumber, (LastNumber - FirstNumber) + 1);
        var values = IsEmpty ? Empty : IsRange ? GetRange() : numbers;
        return values.GetEnumerator();
    }

    #endregion Private Methods

    #region Public Fields

    /// <summary>Gets the empty number sequence.</summary>
    public static readonly int[] Empty = [];

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Creates a new empty <see cref="ImapNumberSequence"/>.</summary>
    public ImapNumberSequence()
    {
        numbers = Empty;
        IsRange = false;
    }

    /// <summary>Creates a new <see cref="ImapNumberSequence"/> with the given message numbers.</summary>
    /// <param name="numbers"></param>
    public ImapNumberSequence(int[] numbers)
    {
        IsRange = false;
        this.numbers = numbers;
    }

    /// <summary>Creates a new <see cref="ImapNumberSequence"/> with the given message number range.</summary>
    /// <param name="first"></param>
    /// <param name="last"></param>
    public ImapNumberSequence(int first, int last)
    {
        IsRange = true;
        numbers = [first, last];
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Obtains the number of items in the list.</summary>
    public int Count
    {
        get
        {
            if (IsRange) { return (LastNumber - FirstNumber) + 1; }
            return numbers.Length;
        }
    }

    /// <summary>Provides the first number of the message list.</summary>
    public int FirstNumber
    {
        get
        {
            if (IsEmpty) { return -1; }
            return numbers[0];
        }
    }

    /// <summary>Returns true if the list is empty.</summary>
    public bool IsEmpty => numbers.Length == 0;

    /// <summary>Returns true if the list does not contain single message numbers but a whole range of message numbers.</summary>
    public bool IsRange { get; }

    /// <summary>Provides the last number of the message list.</summary>
    public int LastNumber
    {
        get
        {
            if (IsEmpty) { return -1; }
            return numbers[^1];
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Creates a <see cref="ImapNumberSequence"/> from the given message number list.</summary>
    /// <param name="numbers"></param>
    /// <returns></returns>
    public static ImapNumberSequence CreateList(params int[] numbers) => new(numbers);

    /// <summary>Creates a <see cref="ImapNumberSequence"/> from the given message number range.</summary>
    /// <param name="firstNumber"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public static ImapNumberSequence CreateRange(int firstNumber, int count) => new(firstNumber, (count + firstNumber) - 1);

    /// <summary>
    /// Creates a <see cref="ImapNumberSequence"/> from the given string. The string is created by <see cref="ToString()"/> or received by <see cref="ImapClient.Search(ImapSearch)"/>.
    /// </summary>
    /// <param name="numbers"></param>
    /// <returns></returns>
    public static ImapNumberSequence FromString(string numbers)
    {
        numbers = numbers.UnboxBrackets(false);
        var result = new ImapNumberSequence();
        var list = new List<int>();
        foreach (var str in numbers.Split(NumbersSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (str.IndexOf(':') > -1)
            {
                var parts = str.Split(':');
                result += new ImapNumberSequence(Convert.ToInt32(parts[0]), Convert.ToInt32(parts[1]));
                continue;
            }
            list.Add(Convert.ToInt32(str));
        }
        return result + new ImapNumberSequence([.. list]);
    }

    /// <summary>Adds to <see cref="ImapNumberSequence"/> s.</summary>
    /// <param name="seq1"></param>
    /// <param name="seq2"></param>
    /// <returns></returns>
    public static ImapNumberSequence operator +(ImapNumberSequence seq1, ImapNumberSequence seq2)
    {
        var resultList = new List<int>(seq1.Count + seq2.Count);

        if (seq1.IsRange)
        {
            for (var i = seq1.FirstNumber; i <= seq1.LastNumber; i++)
            {
                resultList.Add(i);
            }
        }
        else
        {
            resultList.AddRange(seq1.numbers);
        }
        if (seq2.IsRange)
        {
            for (var i = seq2.FirstNumber; i <= seq2.LastNumber; i++)
            {
                if (resultList.Contains(i))
                {
                    continue;
                }

                resultList.Add(i);
            }
        }
        else
        {
            foreach (var number in seq2.numbers)
            {
                if (resultList.Contains(number))
                {
                    continue;
                }

                resultList.Add(number);
            }
        }
        return new([.. resultList]);
    }

    /// <inheritdoc/>
    public IEnumerator<int> GetEnumerator() => GetDefaultEnumerator();

    /// <summary>Sorts the sequence numbers.</summary>
    public void Sort() => Array.Sort(numbers);

    /// <summary>Obtains the string representing the <see cref="ImapNumberSequence"/>.</summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (IsRange)
        {
            return FirstNumber + ":" + LastNumber;
        }
        var strBuilder = new StringBuilder();
        strBuilder.Append(numbers[0]);
        for (var i = 1; i < numbers.Length; i++)
        {
            strBuilder.Append(',');
            strBuilder.Append(numbers[i]);
        }
        return strBuilder.ToString();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetDefaultEnumerator();

    #endregion Public Methods
}
