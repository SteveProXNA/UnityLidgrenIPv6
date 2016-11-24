using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace GameServer
{
	public static class NativeMethods
	{
		[DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
		internal static extern Int32 GetCurrentWin32ThreadId();
	}

	public static class LogManager
	{
		private static readonly log4net.ILog OUTPUT = log4net.LogManager.GetLogger(typeof(LogManager));
		private static Int32 threadId;

		public static void Initialize()
		{
			log4net.Config.XmlConfigurator.Configure();
			threadId = NativeMethods.GetCurrentWin32ThreadId();
		}

		public static void Info(String message)
		{
			Int32 nextThreadId = NativeMethods.GetCurrentWin32ThreadId();
			String prefix = threadId == nextThreadId ? "MAIN" : "work";

			String value = String.Format("[{0}][{1}] {2}", prefix, nextThreadId.ToString(CultureInfo.InvariantCulture).PadLeft(5, ' '), message);
			OUTPUT.Info(value);
		}
	}
}