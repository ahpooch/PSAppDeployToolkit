using System;
using PSADT.PE;
using System.IO;
using System.Linq;
using System.Text;
using PSADT.PathEx;
using PSADT.PInvoke;
using PSADT.ConsoleEx;
using PSADT.WTSSession;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Collections.Specialized;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Manages the execution and monitoring of processes across different user sessions.
    /// </summary>
    public class StartProcess : IDisposable
    {
        private readonly ExecutionManager _processManager;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StartProcess"/> class.
        /// </summary>
        public StartProcess()
        {
            _processManager = new ExecutionManager();
        }

        /// <summary>
        /// Launches and monitors processes based on the provided options.
        /// </summary>
        /// <param name="options">The launch options for the processes.</param>
        /// <returns>An exit code indicating the result of the operation.</returns>
        public async Task<int> ExecuteAndMonitorAsync(LaunchOptions options)
        {
            try
            {
                options.FilePath = PathHelper.ResolveExecutableFullPath(options.FilePath) ?? throw new InvalidOperationException("Resolved file path is null or empty.");

                ConsoleHelper.DebugWrite($"Executing process [{options.FilePath}].", MessageType.Info);

                if (options.AllActiveUserSessions)
                {
                    ConsoleHelper.DebugWrite("Executing process in all active user sessions.", MessageType.Info);
                    await ExecuteInAllActiveSessionsAsync(options);
                }
                else if (options.PrimaryActiveUserSession)
                {
                    ConsoleHelper.DebugWrite("Executing process in primary active user session.", MessageType.Info);
                    await ExecuteInPrimaryActiveSessionAsync(options);
                }
                else if (options.SessionId.HasValue)
                {
                    ConsoleHelper.DebugWrite($"Executing process in specified session with id [{options.SessionId.Value}].", MessageType.Info);
                    await ExecuteProcessInSessionAsync(options);
                }
                else
                {
                    options.SessionId = SessionHelper.GetCurrentProcessSessionId();
                    options.UpdateUsername(@$"{Environment.UserDomainName}\{Environment.UserName}");
                    ConsoleHelper.DebugWrite($"Executing process in current session with id [{options.SessionId.Value}].", MessageType.Info);
                    await ExecuteProcessInCurrentSessionAsync(options);
                }

                if (options.Wait)
                {
                    return await WaitForProcessesAsync(options);
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Process execution failed: {ex.Message}", MessageType.Error, ex);
                return 60103; // ProcessEx execution failed
            }
            finally
            {
                _processManager.Clear();
            }
        }

        private async Task<int> WaitForProcessesAsync(LaunchOptions options)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(options.ConsoleTimeoutInSeconds);
            using var cts = new CancellationTokenSource(timeout);

            try
            {
                ExecutionResult exitInfo = await _processManager.WaitForAllProcessExitsAsync(options.WaitOption,
                                                                                             timeout,
                                                                                             cts.Token);

                if (exitInfo.HasTimedOut)
                {
                    ConsoleHelper.DebugWrite("Timeout reached. Stopping redirection monitors and terminating processes.", MessageType.Debug);
                    await _processManager.StopAllRedirectionMonitorsAsync(options.TerminateOnTimeout);
                }

                await _processManager.WaitForAllRedirectionMonitorsAsync();

                if (options.Debug && exitInfo.ExitedProcessInfo != null)
                {
                    foreach (ExecutionDetails process in exitInfo.ExitedProcessInfo)
                    {
                        ConsoleHelper.DebugWrite($"Process [{process.ProcessName}] with process id [{process.ProcessId}] started in session id [{process.SessionId}] as user [{process.Username}] returned with exit code [{process.ExitCode}].", MessageType.Debug);
                    }
                }

                if (exitInfo?.ExitedProcessInfo?.Any(p => options?.SuccessExitCodes?.Contains(p.ExitCode) == false) == true)
                {
                    return 60102; // At least one process exited with a non-success code
                }

                return 0;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error while waiting for processes: {ex.Message}", MessageType.Error, ex);
                return 60104; // Error during process wait
            }
        }

        /// <summary>
        /// Launches the process in all active user sessions.
        /// </summary>
        /// <param name="options">The launch options for the processes.</param>
        private async Task ExecuteInAllActiveSessionsAsync(LaunchOptions options)
        {
            List<SessionInfo>? sessions = SessionHelper.GetAllActiveUserSessions();
            if (sessions != null && sessions.Count != 0)
            {
                foreach (SessionInfo session in sessions)
                {
                    options.SessionId = session.SessionId;
                    try
                    {
                        options.UpdateUsername(SessionHelper.GetWtsUsernameById(session.SessionId));
                        await ExecuteProcessInSessionAsync(options);
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.DebugWrite($"Failed to execute process in session {session.SessionId}: {ex.Message}", MessageType.Error, ex);
                        // Decide whether to continue with other sessions or throw
                        // For now, we'll log the error and continue with other sessions
                    }
                }
            }
            else
            {
                ConsoleHelper.DebugWrite("No active user sessions found.", MessageType.Warning);
                throw new InvalidOperationException("No active user sessions found.");
            }
        }

        /// <summary>
        /// Launches the process in the primary active user session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        private async Task ExecuteInPrimaryActiveSessionAsync(LaunchOptions options)
        {
            SessionInfo? session = SessionHelper.GetPrimaryActiveUserSession();
            if (session != null)
            {
                options.SessionId = session.SessionId;
                try
                {
                    options.UpdateUsername(SessionHelper.GetWtsUsernameById(session.SessionId));
                    await ExecuteProcessInSessionAsync(options);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.DebugWrite($"Failed to execute process in primary active session: {ex.Message}.", MessageType.Error, ex);
                    throw;
                }
            }
            else
            {
                ConsoleHelper.DebugWrite("No primary active user session found.", MessageType.Warning);
                throw new InvalidOperationException("No primary active user session found.");
            }
        }

        /// <summary>
        /// Launches a process in a specific session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        /// <param name="sessionId">The ID of the session to launch the process in.</param>
        private async Task ExecuteProcessInSessionAsync(LaunchOptions options)
        {
            if (!options.SessionId.HasValue)
            {
                throw new ArgumentException("SessionId must be specified.", nameof(options));
            }

            try
            {
                ManagedProcess processInfo = _processManager.CreateNewProcessInfoObject(new SessionDetails(options.SessionId.Value, options.Username));
                ProcessStartInfo startInfo = CreateProcessStartInfo(options);

                processInfo.RedirectStandardOutput = startInfo.RedirectStandardOutput;
                processInfo.RedirectStandardError = startInfo.RedirectStandardError;
                processInfo.MergeStdErrAndStdOut = options.MergeStdErrAndStdOut;

                bool isGuiApplication = ExecutableType.IsGuiApplication(startInfo.FileName);
                processInfo.IsGuiApplication = isGuiApplication;

                processInfo.Process = await StartProcessInSessionAsync(
                    options.SessionId.Value,
                    startInfo,
                    options.CreateProcessCreationFlags(),
                    options.UseLinkedAdminToken,
                    options.InheritEnvironmentVariables ?? false
                );

                if (processInfo.Process == null)
                {
                    throw new InvalidOperationException($"Failed to start process in session [{options.SessionId.Value}].");
                }

                ConsoleHelper.DebugWrite($"Started process [{processInfo.Process.ProcessName}] with process id [{processInfo.Process.Id}] in session id [{options.SessionId.Value}] as user [{options.Username}].", MessageType.Info);

                ConfigureOutputRedirection(processInfo, options);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error executing process in session [{options.SessionId.Value}]: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Launches a process in the current session.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        private async Task ExecuteProcessInCurrentSessionAsync(LaunchOptions options)
        {
            try
            {
                if (!options.SessionId.HasValue)
                {
                    options.SessionId = SessionHelper.GetCurrentProcessSessionId();
                }

                if (string.IsNullOrEmpty(options.Username))
                {
                    options.UpdateUsername($@"{Environment.UserDomainName}\{Environment.UserName}");
                }

                ConsoleHelper.DebugWrite($"Executing process in current session with id [{options.SessionId.Value}].", MessageType.Info);

                ManagedProcess processInfo = _processManager.CreateNewProcessInfoObject(new SessionDetails(options.SessionId.Value, options.Username));
                ProcessStartInfo startInfo = CreateProcessStartInfo(options);

                bool isGuiApplication = ExecutableType.IsGuiApplication(startInfo.FileName);
                processInfo.IsGuiApplication = isGuiApplication;

                processInfo.Process = await StartProcessAsync(startInfo);

                if (processInfo.Process == null)
                {
                    throw new InvalidOperationException($"Failed to start process in current session with id [{options.SessionId.Value}].");
                }

                ConsoleHelper.DebugWrite($"Started process [{processInfo.Process.ProcessName}] with process id [{processInfo.Process.Id}] in session id [{options.SessionId.Value}] as user [{options.Username}].", MessageType.Info);

                ConfigureOutputRedirection(processInfo, options);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error executing process in current session: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a ProcessStartInfo object based on the provided options.
        /// </summary>
        /// <param name="options">The launch options for the process.</param>
        /// <returns>A ProcessStartInfo object configured with the provided options.</returns>
        private static ProcessStartInfo CreateProcessStartInfo(LaunchOptions options)
        {
            try
            {
                var isGuiApplication = ExecutableType.IsGuiApplication(options.FilePath);

                var startInfo = new ProcessStartInfo
                {
                    FileName = options.FilePath,
                    Arguments = string.Join(" ", options.ArgumentList),
                    WorkingDirectory = options.WorkingDirectory ?? Path.GetDirectoryName(Environment.CurrentDirectory),
                    UseShellExecute = false,
                    CreateNoWindow = options.HideWindow,
                    RedirectStandardOutput = options.RedirectOutput && !isGuiApplication,
                    RedirectStandardError = options.RedirectOutput && !options.MergeStdErrAndStdOut && !isGuiApplication
                };

                // Add additional environment variables
                foreach (var kvp in options.AdditionalEnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
                }

                if (options.FilePath.EndsWith("powershell.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var psArgs = new List<string>
                    {
                        "-NoLogo",
                        "-NoProfile",
                        "-NonInteractive",
                        $"-ExecutionPolicy {(options.BypassPsExecutionPolicy ? "Bypass" : options.PsExecutionPolicy)}",
                        "-Command"
                    };
                    psArgs.AddRange(options.ArgumentList);
                    startInfo.Arguments = string.Join(" ", psArgs);
                }

                ConsoleHelper.DebugWrite($"Created ProcessStartInfo: FileName={startInfo.FileName}, Arguments={startInfo.Arguments}", MessageType.Debug);

                return startInfo;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error creating ProcessStartInfo: {ex.Message}", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Sets up output redirection for the process.
        /// </summary>
        /// <param name="processInfo">The ManagedProcess containing the process to set up redirection for.</param>
        /// <param name="options">The launch options containing redirection settings.</param>
        private static void ConfigureOutputRedirection(ManagedProcess processInfo, LaunchOptions options)
        {
            if (!options.RedirectOutput || processInfo.Process == null)
                return;

            try
            {
                if (!processInfo.RedirectStandardOutput && !processInfo.RedirectStandardError)
                {
                    ConsoleHelper.DebugWrite("Process does not support output redirection. This is likely a GUI application.", MessageType.Info);
                    return;
                }

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                string baseFileName = $"S_{processInfo.SessionInfo.SessionId}_P_{processInfo.Process.Id}_{timestamp}";

                if (!string.IsNullOrEmpty(options.OutputDirectory))
                {
                    string stdoutFile = Path.Combine(options.OutputDirectory, $"{baseFileName}_stdout.txt");
                    string stderrFile = Path.Combine(options.OutputDirectory, $"{baseFileName}_stderr.txt");

                    if (processInfo.RedirectStandardOutput)
                    {
                        processInfo.StandardOutputRedirectionTask = RedirectToFileAsync(processInfo.Process.StandardOutput, stdoutFile);
                    }
                    if (processInfo.RedirectStandardError && !processInfo.MergeStdErrAndStdOut)
                    {
                        processInfo.StandardErrorRedirectionTask = RedirectToFileAsync(processInfo.Process.StandardError, stderrFile);
                    }

                    ConsoleHelper.DebugWrite($"Configured output redirection to files: stdout={stdoutFile}, stderr={stderrFile}", MessageType.Debug);
                }
                else
                {
                    if (processInfo.RedirectStandardOutput)
                    {
                        processInfo.StandardOutputRedirectionTask = RedirectToConsoleAsync(processInfo.Process.StandardOutput, Console.Out);
                    }
                    if (processInfo.RedirectStandardError && !processInfo.MergeStdErrAndStdOut)
                    {
                        processInfo.StandardErrorRedirectionTask = RedirectToConsoleAsync(processInfo.Process.StandardError, Console.Error);
                    }

                    ConsoleHelper.DebugWrite("Configured output redirection to console", MessageType.Debug);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error configuring output redirection: {ex.Message}", MessageType.Error, ex);
            }
        }

        private static async Task RedirectToFileAsync(StreamReader reader, string filePath)
        {
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(fileStream);
                await RedirectStreamAsync(reader, writer);
                ConsoleHelper.DebugWrite($"Completed redirection to file [{filePath}].", MessageType.Debug);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error redirecting to file [{filePath}]: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        private static Task RedirectToConsoleAsync(StreamReader reader, TextWriter writer)
        {
            try
            {
                return RedirectStreamAsync(reader, writer);
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Error redirecting to console: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        private static async Task RedirectStreamAsync(StreamReader reader, TextWriter writer)
        {
            char[] buffer = new char[4096];
            int bytesRead;

            try
            {
                while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await writer.WriteAsync(buffer, 0, bytesRead);
                    await writer.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to redirect stream: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
        }

        public static IDictionary<string, string> ConvertEnvironmentVariables(StringDictionary? environmentVariables)
        {
            if (environmentVariables == null)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return environmentVariables
                .Cast<DictionaryEntry>()
                .Where(entry => entry.Value != null)
                .ToDictionary(
                    entry => (string)entry.Key,
                    entry => (string)entry.Value!,
                    StringComparer.OrdinalIgnoreCase
                );
        }

        /// <summary>
        /// Starts a process in the specified session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to start the process in.</param>
        /// <param name="startInfo">The ProcessStartInfo for the process to start.</param>
        /// <param name="useLinkedAdminToken">If true, uses the linked admin token when available.</param>
        /// <returns>The started ProcessEx object.</returns>
        /// <summary>
        /// Starts a process in the specified session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to start the process in.</param>
        /// <param name="startInfo">The ProcessStartInfo for the process to start.</param>
        /// <param name="processCreationFlags">Flags to control the creation of the process.</param>
        /// <param name="useLinkedAdminToken">If true, uses the linked admin token when available.</param>
        /// <param name="inheritEnvironment">Specifies whether to inherit the parent's environment variables.</param>
        /// <returns>The started ProcessEx object.</returns>
        private static async Task<Process> StartProcessInSessionAsync(
            uint sessionId,
            ProcessStartInfo startInfo,
            CREATE_PROCESS processCreationFlags,
            bool useLinkedAdminToken,
            bool inheritEnvironment)
        {
            SafeAccessToken primaryToken = SafeAccessToken.Invalid;
            SafeAccessToken tokenToUse = SafeAccessToken.Invalid;

            try
            {
                if (!SessionHelper.QueryUserToken(sessionId, out SafeAccessToken impersonationToken))
                {
                    throw new InvalidOperationException($"Failed to obtain the access token for session id [{sessionId}].");
                }

                using (impersonationToken)
                {
                    if (!SessionHelper.DuplicateTokenAsPrimary(impersonationToken, out primaryToken))
                    {
                        throw new InvalidOperationException("Failed to duplicate token as a primary token.");
                    }
                }

                using (primaryToken)
                {
                    tokenToUse = primaryToken;
                    if (useLinkedAdminToken && SessionHelper.GetLinkedElevatedToken(primaryToken, out SafeAccessToken linkedAdminToken))
                    {
                        if (linkedAdminToken.IsInvalid)
                        {
                            throw new InvalidOperationException($"Failed to get the linked elevated token for specified session id [{sessionId}].");
                        }
                        tokenToUse = linkedAdminToken;
                    }

                    var startupInfo = new STARTUPINFO();
                    startupInfo.cb = Marshal.SizeOf(startupInfo);
                    startupInfo.lpDesktop = @"WinSta0\Default";

                    bool isGuiApplication = ExecutableType.IsGuiApplication(startInfo.FileName);

                    if (startInfo.RedirectStandardOutput || startInfo.RedirectStandardError)
                    {
                        startupInfo.dwFlags |= (uint)STARTF.STARTF_USESTDHANDLES;

                        SafeFileHandle standardOutput = CreatePipe();
                        SafeFileHandle standardError = CreatePipe();

                        startupInfo.hStdOutput = startInfo.RedirectStandardOutput ? standardOutput.DangerousGetHandle() : IntPtr.Zero;
                        startupInfo.hStdError = startInfo.RedirectStandardError ? standardError.DangerousGetHandle() : IntPtr.Zero;
                        startupInfo.hStdInput = IntPtr.Zero;
                    }

                    PROCESS_INFORMATION processInfo;

                    IDictionary<string, string> envVars = ConvertEnvironmentVariables(startInfo.EnvironmentVariables);

                    using (var environmentBlock = SessionHelper.CreateEnvironmentBlock(tokenToUse, envVars, inheritEnvironment))
                    {
                        ConsoleHelper.DebugWrite($"Attempting to start process with CreateProcessAsUser with: FileName [{startInfo.FileName}], Arguments [{string.Join(", ", startInfo.Arguments)}], WorkingDirectory [{startInfo.WorkingDirectory}].", MessageType.Debug);

                        if (!NativeMethods.CreateProcessAsUser(
                            tokenToUse,
                            startInfo.FileName,
                            new StringBuilder(startInfo.Arguments),
                            default,
                            default,
                            false,
                            processCreationFlags,
                            environmentBlock.DangerousGetHandle(),
                            startInfo.WorkingDirectory,
                            in startupInfo,
                            out processInfo))
                        {
                            int error = Marshal.GetLastWin32Error();
                            ConsoleHelper.DebugWrite($"'CreateProcessAsUser' failed with error code [{error}].", MessageType.Error);
                            throw new Win32Exception(error, $"'CreateProcessAsUser' failed to start the process [{startInfo.FileName}].");
                        }
                    }

                    Process? process = null;
                    try
                    {
                        process = Process.GetProcessById((int)processInfo.dwProcessId);

                        // Only wait for input idle if it's a GUI application
                        if (isGuiApplication)
                        {
                            await Task.Run(() =>
                            {
                                try
                                {
                                    process.WaitForInputIdle(5000);
                                }
                                catch (InvalidOperationException)
                                {
                                    // The process has exited or is a console application
                                    ConsoleHelper.DebugWrite("Process exited before becoming idle or is a console application.", MessageType.Warning);
                                }
                            });
                        }

                        return process;
                    }
                    finally
                    {
                        NativeMethods.CloseHandle(processInfo.hProcess);
                        NativeMethods.CloseHandle(processInfo.hThread);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to start process in session id [{sessionId}]: {ex.Message}.", MessageType.Error, ex);
                throw;
            }
            finally
            {
                if (!tokenToUse.IsInvalid || !tokenToUse.IsClosed)
                {
                    tokenToUse.Dispose();
                }
            }
        }

        private static SafeFileHandle CreatePipe()
        {
            SECURITY_ATTRIBUTES securityAttributes = new SECURITY_ATTRIBUTES();
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);
            securityAttributes.bInheritHandle = 1;
            securityAttributes.lpSecurityDescriptor = IntPtr.Zero;

            if (!NativeMethods.CreatePipe(out _, out SafeFileHandle writeHandle, securityAttributes, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create pipe.");
            }

            return writeHandle;
        }

        /// <summary>
        /// Starts a process asynchronously with the specified <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> containing the settings to start the process with.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the started <see cref="ProcessEx"/> object.</returns>
        /// <example>
        /// <code>
        /// var startInfo = new ProcessStartInfo
        /// {
        ///     FileName = "example.exe",
        ///     Arguments = "-someArgument",
        ///     RedirectStandardOutput = true,
        ///     RedirectStandardError = true,
        ///     UseShellExecute = false,
        ///     CreateNoWindow = true
        /// };
        /// 
        /// var process = await StartProcessAsync(startInfo);
        /// ConsoleHelper.DebugWrite($"ProcessEx started with ID {process.Id}");
        /// </code>
        /// </example>
        private static async Task<Process> StartProcessAsync(ProcessStartInfo startInfo)
        {
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            try
            {
                ConsoleHelper.DebugWrite($"Attempting to start process: FileName [{startInfo.FileName}], Arguments [{string.Join(", ", startInfo.Arguments)}].", MessageType.Info);

                if (!process.Start())
                {
                    throw new InvalidOperationException("Process.Start() returned false.");
                }

                bool isGuiApplication = ExecutableType.IsGuiApplication(startInfo.FileName);

                // Only wait for input idle if it's a GUI application
                if (isGuiApplication)
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            process.WaitForInputIdle(5000);
                        }
                        catch (InvalidOperationException)
                        {
                            // The process has exited or is a console application
                            ConsoleHelper.DebugWrite("Process exited before becoming idle or is a console application.", MessageType.Warning);
                        }
                    });
                }

                return process;
            }
            catch (Exception ex)
            {
                ConsoleHelper.DebugWrite($"Failed to start process: {ex.Message}", MessageType.Error, ex);
                throw;
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="StartProcess"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="StartProcess"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose() or from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _processManager.Dispose();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Destructor for <see cref="StartProcess"/>.
        /// </summary>
        ~StartProcess()
        {
            Dispose(false);
        }
    }
}