using UnityEngine;

public interface ILogManager
{
	void Initialize(TargetEnvironment targetEnvironment);
	void LogDebug(string message);
	void LogDebugError(string message);
}

public class LogManager : ILogManager
{
	private TargetEnvironment _targetEnvironment;

	public void Initialize(TargetEnvironment targetEnvironment)
	{
		_targetEnvironment = targetEnvironment;
	}

	public void LogDebug(string message)
	{
		if (TargetEnvironment.Test != _targetEnvironment)
		{
			return;
		}

		Debug.Log("db " + message);
	}

	public void LogDebugError(string message)
	{
		if (TargetEnvironment.Test != _targetEnvironment)
		{
			return;
		}

		Debug.LogError("db " + message);
	}
}

public enum TargetEnvironment
{
	Unknown,
	Test,
	Beta,
	Prod
}