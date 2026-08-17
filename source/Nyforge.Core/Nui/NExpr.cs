namespace Nyforge.Core.Nui;

/// <summary>
/// The deterministic NUI expression language (NUI-SCHEMA §7.2) — the C#
/// mirror of the Nyrqis reference floor (<c>ui/nexpr.py</c>) and the
/// Rust crate (<c>rust/nyui/src/nexpr.rs</c>). The same expression must
/// parse, validate, and evaluate identically in NyForge (design time),
/// the floor, and the shipped crate, so this parser is a behavioral
/// mirror: same grammar, same precedence, same error messages (byte
/// offsets included).
///
/// Grammar (lowest to highest precedence):
///   or      := and ('||' and)*
///   and     := compare ('&&' compare)*
///   compare := add (('=='|'!='|'<'|'<='|'>'|'>=') add)*
///   add     := mul (('+'|'-') mul)*
///   mul     := unary (('*'|'/'|'%') unary)*
///   unary   := ('!'|'-'|'+') unary | primary
///   primary := number | string | 'true' | 'false'
///            | 'state' ('.' ident)* | '(' or ')' | func '(' args ')'
///
/// Functions: <c>if(cond, a, b)</c>, <c>min(a, ...)</c>, <c>max(a, ...)</c>,
/// <c>contains(haystack, needle)</c>, <c>format(value, "{0}" | "{0:.2f}", ...)</c>.
/// </summary>
public static class NExpr
{
    // ------------------------------------------------------------------
    // Errors
    // ------------------------------------------------------------------

    /// <summary>An expression error; <see cref="Exception.Message"/> starts with <c>expr:</c>.</summary>
    public sealed class ExprException : Exception
    {
        public ExprException(string message) : base(message) { }
    }

    // ------------------------------------------------------------------
    // AST
    // ------------------------------------------------------------------

    internal abstract record Node;
    internal sealed record Num(double Value) : Node;
    internal sealed record Str(string Value) : Node;
    internal sealed record Bool(bool Value) : Node;
    internal sealed record StateRef(string Name) : Node;
    internal sealed record Func(string Name, IReadOnlyList<Node> Args) : Node;
    internal sealed record Not(Node Operand) : Node;
    internal sealed record Neg(Node Operand) : Node;
    internal sealed record Bin(string Op, Node Left, Node Right) : Node;

    // ------------------------------------------------------------------
    // Tokenizer
    // ------------------------------------------------------------------

    private enum TokKind { Num, Str, Ident, Op, Eof }

    private sealed record Token(TokKind Kind, string Text, int Pos);

    private sealed class Tokenizer
    {
        private readonly string _src;
        private int _index;

        public Tokenizer(string src)
        {
            _src = src;
            _index = 0;
        }

        public Token Next()
        {
            var n = _src.Length;
            while (_index < n && char.IsWhiteSpace(_src[_index]))
            {
                _index++;
            }
            if (_index >= n)
            {
                return new Token(TokKind.Eof, string.Empty, n);
            }
            var pos = _index;
            var c = _src[_index];

            if (char.IsAsciiDigit(c))
            {
                _index++;
                while (_index < n && char.IsAsciiDigit(_src[_index])) _index++;
                if (_index + 1 < n && _src[_index] == '.' && char.IsAsciiDigit(_src[_index + 1]))
                {
                    _index++;
                    while (_index < n && char.IsAsciiDigit(_src[_index])) _index++;
                }
                return new Token(TokKind.Num, _src[pos.._index], pos);
            }

            if (c == '"')
            {
                _index++;
                var text = new System.Text.StringBuilder();
                while (true)
                {
                    if (_index >= n)
                    {
                        throw new ExprException(SyntaxErr(pos, "\""));
                    }
                    var ch = _src[_index];
                    if (ch == '"')
                    {
                        _index++;
                        break;
                    }
                    if (ch == '\\')
                    {
                        _index++;
                        if (_index >= n) throw new ExprException(SyntaxErr(pos, "\""));
                        var esc = _src[_index];
                        text.Append(esc switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'r' => '\r',
                            '"' => '"',
                            '\\' => '\\',
                            '0' => '\0',
                            _ => esc,
                        });
                        _index++;
                    }
                    else
                    {
                        text.Append(ch);
                        _index++;
                    }
                }
                return new Token(TokKind.Str, text.ToString(), pos);
            }

            if (char.IsAsciiLetter(c) || c == '_')
            {
                _index++;
                while (_index < n && (char.IsAsciiLetterOrDigit(_src[_index]) || _src[_index] == '_'))
                {
                    _index++;
                }
                return new Token(TokKind.Ident, _src[pos.._index], pos);
            }

            foreach (var op in new[] { "==", "!=", "<=", ">=", "&&", "||", "(", ")", "{", "}", "[", "]", ",", "." })
            {
                if (_src.AsSpan(pos).StartsWith(op, StringComparison.Ordinal))
                {
                    _index += op.Length;
                    return new Token(TokKind.Op, op, pos);
                }
            }
            const string single = "!<>=+-*/%";
            if (single.Contains(c))
            {
                _index++;
                return new Token(TokKind.Op, c.ToString(), pos);
            }

            throw new ExprException(SyntaxErr(pos, _src[pos..(pos + 1)]));
        }

        private static string SyntaxErr(int pos, string text) =>
            $"expr: syntax error at {pos}: unexpected token '{text}'";
    }

    // ------------------------------------------------------------------
    // Parser (recursive descent)
    // ------------------------------------------------------------------

    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private int _index;

        public Parser(List<Token> tokens) => _tokens = tokens;

        internal Token Peek() => _tokens[_index];
        private Token Advance() => _tokens[_index++];

        private static string SyntaxErr(int pos, string text) =>
            $"expr: syntax error at {pos}: unexpected token '{text}'";

        public Node ParseOr()
        {
            var left = ParseAnd();
            while (Peek().Text == "||")
            {
                Advance();
                left = new Bin("||", left, ParseAnd());
            }
            return left;
        }

        private Node ParseAnd()
        {
            var left = ParseCompare();
            while (Peek().Text == "&&")
            {
                Advance();
                left = new Bin("&&", left, ParseCompare());
            }
            return left;
        }

        private Node ParseCompare()
        {
            var left = ParseAdd();
            while (Peek().Text is "==" or "!=" or "<" or "<=" or ">" or ">=")
            {
                var op = Advance().Text;
                left = new Bin(op, left, ParseAdd());
            }
            return left;
        }

        private Node ParseAdd()
        {
            var left = ParseMul();
            while (Peek().Text is "+" or "-")
            {
                var op = Advance().Text;
                left = new Bin(op, left, ParseMul());
            }
            return left;
        }

        private Node ParseMul()
        {
            var left = ParseUnary();
            while (Peek().Text is "*" or "/" or "%")
            {
                var op = Advance().Text;
                left = new Bin(op, left, ParseUnary());
            }
            return left;
        }

        private Node ParseUnary()
        {
            var text = Peek().Text;
            if (text == "!")
            {
                Advance();
                return new Not(ParseUnary());
            }
            if (text == "-")
            {
                Advance();
                return new Neg(ParseUnary());
            }
            if (text == "+")
            {
                Advance();
                return ParseUnary();
            }
            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            var tok = Advance();
            switch (tok.Kind)
            {
                case TokKind.Num:
                    return new Num(double.Parse(tok.Text, System.Globalization.CultureInfo.InvariantCulture));
                case TokKind.Str:
                    return new Str(tok.Text);
                case TokKind.Ident:
                    switch (tok.Text)
                    {
                        case "state":
                            var nxt = Peek();
                            if (nxt.Text == ".")
                            {
                                Advance();
                                var nameTok = Advance();
                                if (nameTok.Kind != TokKind.Ident || nameTok.Text == "state")
                                {
                                    throw new ExprException(SyntaxErr(nameTok.Pos, nameTok.Text));
                                }
                                var name = nameTok.Text;
                                while (Peek().Text == ".")
                                {
                                    Advance();
                                    var seg = Advance();
                                    if (seg.Kind != TokKind.Ident)
                                    {
                                        throw new ExprException(SyntaxErr(seg.Pos, seg.Text));
                                    }
                                    name += "." + seg.Text;
                                }
                                return new StateRef(name);
                            }
                            if (nxt.Text == "(")
                            {
                                throw new ExprException(SyntaxErr(nxt.Pos, "'state' is not a function"));
                            }
                            return new StateRef(string.Empty);
                        case "true":
                            return new Bool(true);
                        case "false":
                            return new Bool(false);
                        case "if" or "min" or "max" or "contains" or "format":
                            return ParseCall(tok.Text);
                        default:
                            throw new ExprException($"expr: unknown function '{tok.Text}'");
                    }
                case TokKind.Op when tok.Text == "(":
                    var node = ParseOr();
                    var closing = Advance();
                    if (closing.Text != ")")
                    {
                        throw new ExprException(SyntaxErr(closing.Pos, closing.Text));
                    }
                    return node;
                default:
                    throw new ExprException(SyntaxErr(tok.Pos, tok.Text));
            }
        }

        private Node ParseCall(string name)
        {
            var opening = Advance();
            if (opening.Text != "(")
            {
                throw new ExprException(SyntaxErr(opening.Pos, opening.Text));
            }
            var args = new List<Node>();
            if (Peek().Text == ")")
            {
                Advance();
                return new Func(name, args);
            }
            while (true)
            {
                args.Add(ParseOr());
                var sep = Advance();
                if (sep.Text == ")")
                {
                    break;
                }
                if (sep.Text != ",")
                {
                    throw new ExprException(SyntaxErr(sep.Pos, sep.Text));
                }
            }
            return new Func(name, args);
        }
    }

    // ------------------------------------------------------------------
    // Validation
    // ------------------------------------------------------------------

    // name -> (minArgs, maxArgs) — mirrored from ui/nexpr.py.
    private static readonly (string Name, int Min, int? Max)[] Functions =
    {
        ("if", 3, 3),
        ("min", 2, null),
        ("max", 2, null),
        ("contains", 2, 2),
        ("format", 2, null),
    };

    private static string ArityErr(string name, int min, int? max, int count)
    {
        var expected = max switch
        {
            null => $"at least {min}",
            { } m when m == min => min.ToString(),
            { } m => $"{min}-{m}",
        };
        return $"expr: function '{name}' expects {expected} argument(s), got {count}";
    }

    /// <summary>
    /// Structural validation beyond syntax: known functions with correct
    /// arity and (when <paramref name="knownStates"/> is given) only
    /// declared state references. Returns the first error message or
    /// <see langword="null"/>. Mirrors <c>ui/nexpr.py</c>'s
    /// <c>validate()</c>.
    /// </summary>
    internal static string? Validate(Node? node, IReadOnlySet<string>? knownStates)
    {
        if (node is null) return null;

        if (knownStates is not null)
        {
            var missing = FirstMissingState(node, knownStates);
            if (missing is not null)
            {
                return $"expr: unknown state 'state.{missing}'";
            }
        }

        return Walk(node, knownStates);

        static string? Walk(Node n, IReadOnlySet<string>? known)
        {
            switch (n)
            {
                case Func f:
                    var sig = Functions.FirstOrDefault(s => s.Name == f.Name);
                    if (sig.Name is null)
                    {
                        return $"expr: unknown function '{f.Name}'";
                    }
                    var count = f.Args.Count;
                    if (count < sig.Min || (sig.Max is { } max && count > max))
                    {
                        return ArityErr(sig.Name, sig.Min, sig.Max, count);
                    }
                    foreach (var arg in f.Args)
                    {
                        var problem = Walk(arg, known);
                        if (problem is not null) return problem;
                    }
                    return null;
                case Not not:
                    return Walk(not.Operand, known);
                case Neg neg:
                    return Walk(neg.Operand, known);
                case Bin bin:
                    return Walk(bin.Left, known) ?? Walk(bin.Right, known);
                default:
                    return null;
            }
        }
    }

    private static string? FirstMissingState(Node node, IReadOnlySet<string> states)
    {
        switch (node)
        {
            case StateRef { Name: { Length: > 0 } name } when !states.Contains(name):
                return name;
            case Func f:
                foreach (var arg in f.Args)
                {
                    var found = FirstMissingState(arg, states);
                    if (found is not null) return found;
                }
                return null;
            case Not not:
                return FirstMissingState(not.Operand, states);
            case Neg neg:
                return FirstMissingState(neg.Operand, states);
            case Bin bin:
                return FirstMissingState(bin.Left, states) ?? FirstMissingState(bin.Right, states);
            default:
                return null;
        }
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// <summary>
    /// Parse an expression. Throws <see cref="ExprException"/> on syntax
    /// errors; the message is byte-identical to the other implementations.
    /// </summary>
    internal static Node Parse(string text)
    {
        var tokenizer = new Tokenizer(text);
        var tokens = new List<Token>();
        while (true)
        {
            var tok = tokenizer.Next();
            tokens.Add(tok);
            if (tok.Kind == TokKind.Eof) break;
        }
        var parser = new Parser(tokens);
        var node = parser.ParseOr();
        if (parser.Peek().Kind != TokKind.Eof)
        {
            var tok = parser.Peek();
            throw new ExprException($"expr: syntax error at {tok.Pos}: unexpected token '{tok.Text}'");
        }
        return node;
    }

    /// <summary>
    /// Parse + structurally validate an expression against
    /// <paramref name="knownStates"/>. Returns the first error message
    /// or <see langword="null"/> when the expression is valid.
    /// </summary>
    public static string? TryValidate(string text, IReadOnlySet<string>? knownStates)
    {
        Node node;
        try
        {
            node = Parse(text);
        }
        catch (ExprException exc)
        {
            return exc.Message;
        }
        return Validate(node, knownStates);
    }

    /// <summary>
    /// Parse + evaluate an expression against <paramref name="states"/>
    /// (state name → value). Throws <see cref="ExprException"/> on
    /// syntax or evaluation errors (division by zero, type mismatches).
    /// </summary>
    public static object? Evaluate(string text, IReadOnlyDictionary<string, object?> states)
    {
        var node = Parse(text);
        return Eval(node, states);
    }

    private static object? Eval(Node node, IReadOnlyDictionary<string, object?> states)
    {
        return node switch
        {
            Num n => n.Value,
            Str s => s.Value,
            Bool b => b.Value,
            StateRef sr => states.TryGetValue(sr.Name, out var v) ? v : string.Empty,
            Not not => !Truthy(Eval(not.Operand, states)),
            Neg neg => -AsNumber(Eval(neg.Operand, states), "operand of '-'"),
            Bin bin => EvalBin(bin, states),
            Func f => EvalFunc(f, states),
            _ => throw new ExprException($"expr: internal: unknown node {node.GetType().Name}"),
        };
    }

    private static object? EvalBin(Bin bin, IReadOnlyDictionary<string, object?> states)
    {
        switch (bin.Op)
        {
            case "&&":
                return Truthy(Eval(bin.Left, states)) && Truthy(Eval(bin.Right, states));
            case "||":
                return Truthy(Eval(bin.Left, states)) || Truthy(Eval(bin.Right, states));
            case "==":
            case "!=":
                var equal = EqualsValue(Eval(bin.Left, states), Eval(bin.Right, states));
                return bin.Op == "==" ? equal : !equal;
            case "<":
            case "<=":
            case ">":
            case ">=":
                var a = CompareNumber(Eval(bin.Left, states), bin.Op);
                var b = CompareNumber(Eval(bin.Right, states), bin.Op);
                return bin.Op switch
                {
                    "<" => a < b,
                    "<=" => a <= b,
                    ">" => a > b,
                    _ => a >= b,
                };
            case "+":
                var l = Eval(bin.Left, states);
                var r = Eval(bin.Right, states);
                if (l is string || r is string)
                {
                    return string.Concat(l, r);
                }
                return AsNumber(l, "operand of '+'") + AsNumber(r, "operand of '+'");
            case "-":
                return AsNumber(Eval(bin.Left, states), "operand of '-'") -
                       AsNumber(Eval(bin.Right, states), "operand of '-'");
            case "*":
                return AsNumber(Eval(bin.Left, states), "operand of '*'") *
                       AsNumber(Eval(bin.Right, states), "operand of '*'");
            case "/":
                var den = AsNumber(Eval(bin.Right, states), "operand of '/'");
                if (den == 0) throw new ExprException("expr: division by zero");
                return AsNumber(Eval(bin.Left, states), "operand of '/'") / den;
            case "%":
                var mod = AsNumber(Eval(bin.Right, states), "operand of '%'");
                if (mod == 0) throw new ExprException("expr: modulo by zero");
                return AsNumber(Eval(bin.Left, states), "operand of '%'") % mod;
            default:
                throw new ExprException($"expr: internal: unknown operator '{bin.Op}'");
        }
    }

    private static object? EvalFunc(Func f, IReadOnlyDictionary<string, object?> states)
    {
        var args = f.Args.Select(a => Eval(a, states)).ToArray();
        switch (f.Name)
        {
            case "if":
                return Truthy(args[0]) ? args[1] : args[2];
            case "min":
                return args.Select(v => AsNumber(v, $"argument of '{f.Name}'")).Min();
            case "max":
                return args.Select(v => AsNumber(v, $"argument of '{f.Name}'")).Max();
            case "contains":
                var haystack = args[0];
                var needle = args[1];
                if (haystack is string h && needle is string nd)
                {
                    return h.Contains(nd, StringComparison.Ordinal);
                }
                if (haystack is System.Collections.IEnumerable list && needle is not null)
                {
                    foreach (var item in list)
                    {
                        if (EqualsValue(item, needle)) return true;
                    }
                    return false;
                }
                throw new ExprException("expr: contains: first argument must be a string or list");
            case "format":
                var value = args[0];
                if (args[1] is not string fmt)
                {
                    throw new ExprException("expr: format: format string must be a string");
                }
                var spec = ParseFormatSpec(fmt);
                if (args.Length == 2)
                {
                    return FormatValue(value, spec);
                }
                var parts = new List<string> { fmt };
                for (var i = 2; i < args.Length; i++)
                {
                    parts.Add(Convert.ToString(args[i], System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
                }
                return string.Concat(parts);
            default:
                throw new ExprException($"expr: unknown function '{f.Name}'");
        }
    }

    private static bool Truthy(object? value) => value switch
    {
        null => false,
        bool b => b,
        double d => d != 0,
        float f => f != 0,
        int i => i != 0,
        long l => l != 0,
        decimal m => m != 0,
        string s => s.Length > 0,
        System.Collections.IEnumerable e => e.Cast<object?>().Any(),
        _ => true,
    };

    private static double AsNumber(object? value, string what)
    {
        switch (value)
        {
            case bool:
                throw new ExprException($"expr: {what} must be a number, got boolean");
            case double d:
                return d;
            case float f:
                return f;
            case int i:
                return i;
            case long l:
                return l;
            case decimal m:
                return (double)m;
            default:
                throw new ExprException($"expr: {what} must be a number, got '{value}'");
        }
    }

    private static double CompareNumber(object? value, string op)
    {
        switch (value)
        {
            case bool:
                throw new ExprException($"expr: cannot compare boolean with '{op}'");
            case double d:
                return d;
            case float f:
                return f;
            case int i:
                return i;
            case long l:
                return l;
            case decimal m:
                return (double)m;
            default:
                throw new ExprException($"expr: cannot compare '{value}' with '{op}'");
        }
    }

    private static bool EqualsValue(object? left, object? right)
    {
        if (left is bool || right is bool)
        {
            return Equals(left, right);
        }
        if (IsNumber(left) && IsNumber(right))
        {
            return AsNumber(left, "comparison") == AsNumber(right, "comparison");
        }
        if (IsNumber(left) || IsNumber(right))
        {
            try
            {
                return Convert.ToDouble(left, System.Globalization.CultureInfo.InvariantCulture) ==
                       Convert.ToDouble(right, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return Equals(left, right);
    }

    private static bool IsNumber(object? value) =>
        value is double or float or int or long or decimal;

    private static string FormatValue(object? value, string spec)
    {
        if (spec.Length == 0)
        {
            if (value is double d && d == Math.Floor(d))
            {
                return ((long)d).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        }
        if (IsNumber(value))
        {
            try
            {
                // Translate the common Python-style specs so the same
                // format string behaves identically in .NET: ".2f" is
                // Python fixed-point -> .NET standard "F2", ".3e" is
                // Python exponent -> .NET standard "E3".
                var dotnetSpec = TranslateNumericSpec(spec);
                return AsNumber(value, "format").ToString(dotnetSpec, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                throw new ExprException($"expr: format: invalid numeric spec '{{{spec}}}'");
            }
        }
        throw new ExprException($"expr: format: non-numeric value with numeric spec '{{{spec}}}'");
    }

    private static string TranslateNumericSpec(string spec)
    {
        // ".Nf" -> "FN", ".Ne" -> "EN" — identical output to Python's
        // fixed-point and exponent format types.
        if (spec.Length >= 3 && spec[0] == '.' &&
            spec[1..^1].All(char.IsAsciiDigit))
        {
            var precision = spec[1..^1];
            var last = spec[^1];
            if (last == 'f') return "F" + precision;
            if (last == 'e') return "E" + precision;
        }
        return spec;
    }

    private static string ParseFormatSpec(string fmt)
    {
        var body = fmt.Trim();
        if (body.Length < 3 || !body.StartsWith('{') || !body.EndsWith('}'))
        {
            throw new ExprException("expr: format: format string must be like '{0}' or '{0:.2f}'");
        }
        var inner = body[1..^1];
        if (inner.Length == 0 || !char.IsAsciiDigit(inner[0]))
        {
            throw new ExprException("expr: format: format string must be like '{0}' or '{0:.2f}'");
        }
        var colon = inner.IndexOf(':');
        if (colon >= 0)
        {
            var index = inner[..colon];
            if (index.Length == 0 || index.Any(ch => !char.IsAsciiDigit(ch)))
            {
                throw new ExprException("expr: format: format string must be like '{0}' or '{0:.2f}'");
            }
            return inner[(colon + 1)..];
        }
        return string.Empty;
    }
}
