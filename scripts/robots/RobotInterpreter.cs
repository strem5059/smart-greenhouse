using System;
using System.Collections.Generic;
using System.Globalization;

namespace SmartGreenhouse.Robots;

public interface IRobotHardware
{
    bool IsAction(string name);
    bool TryCall(string name, IReadOnlyList<double> args, out double result);
    void BeginAction(string action);
    bool IsBusy { get; }
    string? ActionError { get; }
    void CancelAction();
}

public sealed class RobotException : Exception
{
    public int Line { get; }

    public RobotException(int line, string message) : base(message)
    {
        Line = line;
    }
}

internal enum TokenKind { Identifier, Number, String, Symbol, End }

internal readonly record struct Token(TokenKind Kind, string Text, int Line, double Number = 0);

internal static class Lexer
{
    public static List<Token> Tokenize(string source)
    {
        var tokens = new List<Token>();
        int line = 1;
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            if (c == '\n')
            {
                line++;
                i++;
            }
            else if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (c == '/' && i + 1 < source.Length && source[i + 1] == '/')
            {
                while (i < source.Length && source[i] != '\n')
                {
                    i++;
                }
            }
            else if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new Token(TokenKind.Identifier, source[start..i], line));
            }
            else if (char.IsDigit(c))
            {
                int start = i;
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.'))
                {
                    i++;
                }

                string text = source[start..i];
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    throw new RobotException(line, $"Número no válido: {text}");
                }

                tokens.Add(new Token(TokenKind.Number, text, line, value));
            }
            else if (c == '"')
            {
                int start = ++i;
                while (i < source.Length && source[i] != '"' && source[i] != '\n')
                {
                    i++;
                }

                if (i >= source.Length || source[i] != '"')
                {
                    throw new RobotException(line, "Cadena sin cerrar");
                }

                tokens.Add(new Token(TokenKind.String, source[start..i], line));
                i++;
            }
            else
            {
                string two = i + 1 < source.Length ? source.Substring(i, 2) : string.Empty;
                if (two is "<=" or ">=" or "==" or "!=" or "&&" or "||")
                {
                    tokens.Add(new Token(TokenKind.Symbol, two, line));
                    i += 2;
                }
                else if ("(){};,.<>=+-*/!".IndexOf(c) >= 0)
                {
                    tokens.Add(new Token(TokenKind.Symbol, c.ToString(), line));
                    i++;
                }
                else
                {
                    throw new RobotException(line, $"Carácter no admitido: '{c}'");
                }
            }
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, line));
        return tokens;
    }
}

internal abstract record Expr(int Line);
internal sealed record NumExpr(int Line, double Value) : Expr(Line);
internal sealed record VarExpr(int Line, string Name) : Expr(Line);
internal sealed record StrExpr(int Line, string Value) : Expr(Line);
internal sealed record CallExpr(int Line, string Name, List<Expr> Args) : Expr(Line);
internal sealed record BinExpr(int Line, string Op, Expr Left, Expr Right) : Expr(Line);
internal sealed record UnaryExpr(int Line, string Op, Expr Operand) : Expr(Line);

internal abstract record Stmt(int Line);
internal sealed record DeclStmt(int Line, string Name, Expr? Value) : Stmt(Line);
internal sealed record AssignStmt(int Line, string Name, Expr Value) : Stmt(Line);
internal sealed record CallStmt(int Line, CallExpr Call) : Stmt(Line);
internal sealed record IfStmt(int Line, Expr Condition, List<Stmt> Then, List<Stmt> Else) : Stmt(Line);
internal sealed record WhileStmt(int Line, Expr Condition, List<Stmt> Body) : Stmt(Line);

internal sealed record ProgramAst(List<Stmt> Globals, List<Stmt> Setup, List<Stmt> Loop);

internal sealed class Parser
{
    private static readonly HashSet<string> TypeNames = new() { "float", "int", "bool", "double" };
    private readonly List<Token> _tokens;
    private int _pos;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    private Token Peek => _tokens[_pos];

    private bool IsSymbol(string text) => Peek.Kind == TokenKind.Symbol && Peek.Text == text;

    private bool IsWord(string text) => Peek.Kind == TokenKind.Identifier && Peek.Text == text;

    private void Expect(string text)
    {
        if (!IsSymbol(text))
        {
            throw new RobotException(Peek.Line, $"Se esperaba '{text}' cerca de '{Peek.Text}'");
        }

        _pos++;
    }

    private string ExpectIdentifier()
    {
        if (Peek.Kind != TokenKind.Identifier)
        {
            throw new RobotException(Peek.Line, $"Se esperaba un identificador cerca de '{Peek.Text}'");
        }

        return _tokens[_pos++].Text;
    }

    public ProgramAst ParseProgram()
    {
        var globals = new List<Stmt>();
        List<Stmt>? setup = null;
        List<Stmt>? loop = null;
        while (Peek.Kind != TokenKind.End)
        {
            if (IsWord("void"))
            {
                _pos++;
                int line = Peek.Line;
                string name = ExpectIdentifier();
                Expect("(");
                Expect(")");
                List<Stmt> body = ParseBlock();
                if (name == "setup" && setup == null)
                {
                    setup = body;
                }
                else if (name == "loop" && loop == null)
                {
                    loop = body;
                }
                else
                {
                    throw new RobotException(line, $"Función no admitida o repetida: {name}()");
                }
            }
            else
            {
                globals.Add(ParseStatement());
            }
        }

        if (loop == null)
        {
            throw new RobotException(1, "Falta la función void loop()");
        }

        return new ProgramAst(globals, setup ?? new List<Stmt>(), loop);
    }

    private List<Stmt> ParseBlock()
    {
        Expect("{");
        var block = new List<Stmt>();
        while (!IsSymbol("}"))
        {
            if (Peek.Kind == TokenKind.End)
            {
                throw new RobotException(Peek.Line, "Falta '}'");
            }

            block.Add(ParseStatement());
        }

        _pos++;
        return block;
    }

    private List<Stmt> ParseBody() => IsSymbol("{") ? ParseBlock() : new List<Stmt> { ParseStatement() };

    private Stmt ParseStatement()
    {
        int line = Peek.Line;
        if (Peek.Kind == TokenKind.Identifier && TypeNames.Contains(Peek.Text))
        {
            _pos++;
            string name = ExpectIdentifier();
            Expr? value = null;
            if (IsSymbol("="))
            {
                _pos++;
                value = ParseExpr();
            }

            Expect(";");
            return new DeclStmt(line, name, value);
        }

        if (IsWord("while"))
        {
            _pos++;
            Expect("(");
            Expr condition = ParseExpr();
            Expect(")");
            return new WhileStmt(line, condition, ParseBody());
        }

        if (IsWord("if"))
        {
            _pos++;
            Expect("(");
            Expr condition = ParseExpr();
            Expect(")");
            List<Stmt> then = ParseBody();
            List<Stmt> otherwise = new();
            if (IsWord("else"))
            {
                _pos++;
                otherwise = ParseBody();
            }

            return new IfStmt(line, condition, then, otherwise);
        }

        if (Peek.Kind == TokenKind.Identifier && _tokens[_pos + 1].Kind == TokenKind.Symbol && _tokens[_pos + 1].Text == "=")
        {
            string name = _tokens[_pos++].Text;
            _pos++;
            Expr value = ParseExpr();
            Expect(";");
            return new AssignStmt(line, name, value);
        }

        if (Peek.Kind == TokenKind.Identifier)
        {
            CallExpr call = ParseCall();
            Expect(";");
            return new CallStmt(line, call);
        }

        throw new RobotException(line, $"Instrucción no válida cerca de '{Peek.Text}'");
    }

    private CallExpr ParseCall()
    {
        int line = Peek.Line;
        string name = ExpectIdentifier();
        if (IsSymbol("."))
        {
            _pos++;
            name += "." + ExpectIdentifier();
        }

        Expect("(");
        var args = new List<Expr>();
        if (!IsSymbol(")"))
        {
            while (true)
            {
                if (Peek.Kind == TokenKind.String)
                {
                    args.Add(new StrExpr(Peek.Line, _tokens[_pos++].Text));
                }
                else
                {
                    args.Add(ParseExpr());
                }

                if (!IsSymbol(","))
                {
                    break;
                }

                _pos++;
            }
        }

        Expect(")");
        return new CallExpr(line, name, args);
    }

    private Expr ParseExpr() => ParseBinary(0);

    private static readonly string[][] Levels =
    {
        new[] { "||" },
        new[] { "&&" },
        new[] { "==", "!=" },
        new[] { "<", ">", "<=", ">=" },
        new[] { "+", "-" },
        new[] { "*", "/" },
    };

    private Expr ParseBinary(int level)
    {
        if (level == Levels.Length)
        {
            return ParseUnary();
        }

        Expr left = ParseBinary(level + 1);
        while (Peek.Kind == TokenKind.Symbol && Array.IndexOf(Levels[level], Peek.Text) >= 0)
        {
            Token op = _tokens[_pos++];
            left = new BinExpr(op.Line, op.Text, left, ParseBinary(level + 1));
        }

        return left;
    }

    private Expr ParseUnary()
    {
        if (IsSymbol("!") || IsSymbol("-"))
        {
            Token op = _tokens[_pos++];
            return new UnaryExpr(op.Line, op.Text, ParseUnary());
        }

        return ParsePrimary();
    }

    private Expr ParsePrimary()
    {
        Token token = Peek;
        if (token.Kind == TokenKind.Number)
        {
            _pos++;
            return new NumExpr(token.Line, token.Number);
        }

        if (token.Kind == TokenKind.Identifier)
        {
            if (token.Text == "true" || token.Text == "false")
            {
                _pos++;
                return new NumExpr(token.Line, token.Text == "true" ? 1 : 0);
            }

            if (_tokens[_pos + 1].Kind == TokenKind.Symbol && (_tokens[_pos + 1].Text == "(" || _tokens[_pos + 1].Text == "."))
            {
                return ParseCall();
            }

            _pos++;
            return new VarExpr(token.Line, token.Text);
        }

        if (IsSymbol("("))
        {
            _pos++;
            Expr inner = ParseExpr();
            Expect(")");
            return inner;
        }

        throw new RobotException(token.Line, $"Expresión no válida cerca de '{token.Text}'");
    }
}

public sealed class RobotInterpreter
{
    private const int MaxStepsPerUpdate = 500;
    private const double LoopPause = 0.05;
    private const double ActionPause = 0.05;

    private readonly IRobotHardware _hardware;
    private readonly Action<string> _console;
    private readonly Dictionary<string, double> _vars = new();
    private ProgramAst? _program;
    private IEnumerator<double>? _run;
    private double _wait;

    public RobotInterpreter(IRobotHardware hardware, Action<string> console)
    {
        _hardware = hardware;
        _console = console;
    }

    public bool IsLoaded => _program != null;
    public bool IsRunning { get; private set; }
    public bool IsWaiting => IsRunning && _wait > 0;
    public string? LastError { get; private set; }
    public int ErrorLine { get; private set; }
    public int CurrentLine { get; private set; }

    private bool IsAction(string name) => _hardware.IsAction(name);

    public bool Load(string source)
    {
        try
        {
            _program = new Parser(Lexer.Tokenize(source)).ParseProgram();
            LastError = null;
            ErrorLine = 0;
            return true;
        }
        catch (RobotException error)
        {
            _program = null;
            LastError = error.Message;
            ErrorLine = error.Line;
            _console($"Error (línea {error.Line}): {error.Message}");
            return false;
        }
    }

    public void Start()
    {
        if (_program == null)
        {
            LastError = "Valida el programa antes de ejecutarlo.";
            ErrorLine = 0;
            return;
        }

        _vars.Clear();
        _wait = 0;
        LastError = null;
        IsRunning = true;
        _run = Execute(_program).GetEnumerator();
        _console("--- Programa iniciado ---");
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        Halt();
        _console("--- Programa detenido ---");
    }

    public void Update(double delta)
    {
        if (!IsRunning || _run == null)
        {
            return;
        }

        _wait -= delta;
        int steps = 0;
        try
        {
            while (_wait <= 0 && IsRunning)
            {
                if (++steps > MaxStepsPerUpdate)
                {
                    _wait = 0;
                    break;
                }

                if (!_run.MoveNext())
                {
                    Halt();
                    _console("--- Programa terminado ---");
                    break;
                }

                _wait = _run.Current;
            }
        }
        catch (RobotException error)
        {
            LastError = error.Message;
            ErrorLine = error.Line;
            _console($"Error (línea {error.Line}): {error.Message}");
            Halt();
        }
    }

    private void Halt()
    {
        IsRunning = false;
        _wait = 0;
        _run?.Dispose();
        _run = null;
        _hardware.CancelAction();
    }

    private IEnumerable<double> Execute(ProgramAst program)
    {
        foreach (double wait in RunBlock(program.Globals))
        {
            yield return wait;
        }

        foreach (double wait in RunBlock(program.Setup))
        {
            yield return wait;
        }

        while (true)
        {
            foreach (double wait in RunBlock(program.Loop))
            {
                yield return wait;
            }

            yield return LoopPause;
        }
    }

    private IEnumerable<double> RunBlock(List<Stmt> block)
    {
        foreach (Stmt statement in block)
        {
            CurrentLine = statement.Line;
            foreach (double wait in RunStatement(statement))
            {
                yield return wait;
            }
        }
    }

    private IEnumerable<double> RunStatement(Stmt statement)
    {
        switch (statement)
        {
            case DeclStmt declaration:
                _vars[declaration.Name] = declaration.Value == null ? 0 : Evaluate(declaration.Value);
                break;
            case AssignStmt assignment:
                if (!_vars.ContainsKey(assignment.Name))
                {
                    throw new RobotException(assignment.Line, $"Variable no declarada: {assignment.Name}");
                }

                _vars[assignment.Name] = Evaluate(assignment.Value);
                break;
            case IfStmt condition:
                List<Stmt> branch = Evaluate(condition.Condition) != 0 ? condition.Then : condition.Else;
                foreach (double wait in RunBlock(branch))
                {
                    yield return wait;
                }

                break;
            case WhileStmt loop:
                while (Evaluate(loop.Condition) != 0)
                {
                    foreach (double wait in RunBlock(loop.Body))
                    {
                        yield return wait;
                    }

                    yield return LoopPause;
                }

                break;
            case CallStmt call when call.Call.Name == "delay":
                if (call.Call.Args.Count != 1)
                {
                    throw new RobotException(call.Line, "delay() necesita un argumento en milisegundos");
                }

                yield return Math.Max(0, Evaluate(call.Call.Args[0])) / 1000.0;
                break;
            case CallStmt action when IsAction(action.Call.Name):
                _hardware.BeginAction(action.Call.Name);
                while (_hardware.IsBusy)
                {
                    yield return ActionPause;
                }

                if (_hardware.ActionError is { } error)
                {
                    throw new RobotException(action.Line, error);
                }

                break;
            case CallStmt call:
                Invoke(call.Call);
                break;
        }
    }

    private double Evaluate(Expr expression)
    {
        switch (expression)
        {
            case NumExpr number:
                return number.Value;
            case VarExpr variable when variable.Name == "Fertilizante":
                return 1;
            case VarExpr variable:
                if (!_vars.TryGetValue(variable.Name, out double value))
                {
                    throw new RobotException(variable.Line, $"Variable no declarada: {variable.Name}");
                }

                return value;
            case StrExpr text:
                throw new RobotException(text.Line, "Las cadenas solo pueden usarse en Serial");
            case CallExpr call:
                return Invoke(call);
            case UnaryExpr unary:
                double operand = Evaluate(unary.Operand);
                return unary.Op == "!" ? (operand == 0 ? 1 : 0) : -operand;
            case BinExpr binary:
                return Apply(binary, Evaluate(binary.Left), Evaluate(binary.Right));
            default:
                throw new RobotException(expression.Line, "Expresión no soportada");
        }
    }

    private static double Apply(BinExpr binary, double left, double right)
    {
        return binary.Op switch
        {
            "+" => left + right,
            "-" => left - right,
            "*" => left * right,
            "/" => right == 0 ? throw new RobotException(binary.Line, "División entre cero") : left / right,
            "<" => left < right ? 1 : 0,
            ">" => left > right ? 1 : 0,
            "<=" => left <= right ? 1 : 0,
            ">=" => left >= right ? 1 : 0,
            "==" => left == right ? 1 : 0,
            "!=" => left != right ? 1 : 0,
            "&&" => left != 0 && right != 0 ? 1 : 0,
            "||" => left != 0 || right != 0 ? 1 : 0,
            _ => throw new RobotException(binary.Line, $"Operador no admitido: {binary.Op}")
        };
    }

    private double Invoke(CallExpr call)
    {
        switch (call.Name)
        {
            case "recolectar":
            case "replantar":
            case "moverABaul":
                throw new RobotException(call.Line, $"{call.Name}() solo puede usarse como instrucción");
            case "Serial.begin":
                return 0;
            case "Serial.println":
            case "Serial.print":
                _console(FormatArguments(call));
                return 0;
            case "delay":
                throw new RobotException(call.Line, "delay() solo puede usarse como instrucción independiente");
            default:
            {
                var values = new List<double>();
                foreach (var arg in call.Args)
                {
                    values.Add(Evaluate(arg));
                }

                if (_hardware.TryCall(call.Name, values, out double result))
                {
                    return result;
                }

                throw new RobotException(call.Line, $"Función no admitida en este robot: {call.Name}()");
            }
        }
    }

    private static void RequireArguments(CallExpr call, int count)
    {
        if (call.Args.Count != count)
        {
            throw new RobotException(call.Line, $"{call.Name}() requiere {count} argumento(s)");
        }
    }

    private string FormatArguments(CallExpr call)
    {
        var parts = new List<string>();
        foreach (Expr argument in call.Args)
        {
            if (argument is StrExpr text)
            {
                parts.Add(text.Value);
            }
            else
            {
                double value = Evaluate(argument);
                parts.Add(Math.Abs(value - Math.Round(value)) < 1e-9
                    ? value.ToString("0", CultureInfo.InvariantCulture)
                    : value.ToString("0.0", CultureInfo.InvariantCulture));
            }
        }

        return string.Join(" ", parts);
    }
}
