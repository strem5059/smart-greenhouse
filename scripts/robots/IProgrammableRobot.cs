using System;
using System.Collections.Generic;

namespace SmartGreenhouse.Robots;

public sealed record RobotSetting(string Label, double Min, double Max, double Step, Func<double> Read, Action<double> Write, string Suffix = "");

public interface IProgrammableRobot
{
	string ProgramTitle { get; }
	string DefaultProgram { get; }
	string? SavedProgram { get; }
	RobotInterpreter Interpreter { get; }
	string InfoText { get; }
	string StateName { get; }
	string TaskName { get; }
	bool IsPaused { get; }
	IReadOnlyList<RobotSetting> Settings { get; }
	bool LoadProgram(string source);
	void Run();
	void StopProgram();
	void TogglePause();
}
