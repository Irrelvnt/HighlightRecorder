using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace HighlightRecorder
{
    public class Recorder
    {
        private Process ffmpegProcess;
        private string tempFolder;
        private System.Timers.Timer cleanupTimer;
        private readonly object chunkLock = new object();
        public bool IsRecording { get; private set; } = false;

        public string FfmpegPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "bin", "ffmpeg.exe");

        public Action<string> Logger { get; set; }

        public Recorder()
        {
            tempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempChunks");
            Directory.CreateDirectory(tempFolder);

            cleanupTimer = new System.Timers.Timer(10000); // every 10s
            cleanupTimer.Elapsed += CleanupOldChunks;
        }

        private void Log(string message)
        {
            Logger?.Invoke(message);
            Console.WriteLine(message);
        }

        public Size GetPrimaryResolution()
        {
            return Screen.PrimaryScreen.Bounds.Size;
        }

        public bool IsAv1NvencSupported()
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = FfmpegPath;
                process.StartInfo.Arguments = "-encoders";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                bool supported = output.Contains("av1_nvenc");
                Log(supported ? "AV1 NVENC encoder is supported." : "AV1 NVENC encoder is NOT supported on this system.");
                return supported;
            }
            catch (Exception ex)
            {
                Log("Error checking AV1 NVENC support: " + ex.Message);
                return false;
            }
        }

        public void Start()
        {
            try
            {
                if (!File.Exists(FfmpegPath))
                    throw new FileNotFoundException("FFmpeg not found at: " + FfmpegPath);

                if (!IsAv1NvencSupported())
                    throw new Exception("AV1 NVENC encoding is not supported on this system.");

                var res = GetPrimaryResolution();
                Log($"Primary monitor resolution detected: {res.Width}x{res.Height}");

                IsRecording = true;
                StartChunkRecording();
                cleanupTimer.Start();
            }
            catch (Exception ex)
            {
                Log("Error starting recording: " + ex.Message);
                throw;
            }
        }

        private void StartChunkRecording()
        {
            try
            {
                string outputPath = Path.Combine(tempFolder, $"chunk_{DateTime.Now:yyyyMMdd_HHmmss}.mkv");

                ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = FfmpegPath;
                int screenIndex = Screen.AllScreens.ToList().FindIndex(s => s.Primary);
                ffmpegProcess.StartInfo.Arguments =
                $"-filter_complex \"ddagrab=output_idx={screenIndex}:framerate=60,hwdownload,format=bgra\" " +
                $"-f dshow -i audio=\"Stereo Mix (Realtek(R) Audio)\" " +
                $"-c:v libx264 -crf 18 " +
                $"-c:a aac -b:a 192k " +
                $"-t 10 " +
                $"\"{outputPath}\"";


                ffmpegProcess.StartInfo.RedirectStandardError = true;
                ffmpegProcess.StartInfo.RedirectStandardOutput = true;
                ffmpegProcess.StartInfo.UseShellExecute = false;
                ffmpegProcess.EnableRaisingEvents = true;
                ffmpegProcess.StartInfo.CreateNoWindow = true;
                ffmpegProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                ffmpegProcess.OutputDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };
                ffmpegProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) Log(e.Data); };

                ffmpegProcess.Exited += (s, e) =>
                {
                    if (ffmpegProcess.ExitCode != 0)
                        Log($"FFmpeg exited with code {ffmpegProcess.ExitCode}");

                    if (IsRecording)
                        StartChunkRecording(); // restart next chunk
                };

                ffmpegProcess.Start();
                ffmpegProcess.BeginOutputReadLine();
                ffmpegProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                Log("Error starting chunk recording: " + ex.Message);
            }
        }

        private string GetDefaultAudioDevice()
        {
            // You may need to change this string depending on your system
            // Common values:
            // "virtual-audio-capturer"
            // "Stereo Mix (Realtek(R) Audio)"
            return "virtual-audio-capturer";
        }

        private void CleanupOldChunks(object sender, ElapsedEventArgs e)
        {
            lock (chunkLock)
            {
                try
                {
                    var files = new DirectoryInfo(tempFolder).GetFiles("chunk_*.mkv")
                                                             .OrderBy(f => f.CreationTime)
                                                             .ToArray();

                    while (files.Length > 30) // keep ~5 minutes
                    {
                        files[0].Delete();
                        files = new DirectoryInfo(tempFolder).GetFiles("chunk_*.mkv")
                                                             .OrderBy(f => f.CreationTime)
                                                             .ToArray();
                    }
                }
                catch (Exception ex)
                {
                    Log("Error cleaning old chunks: " + ex.Message);
                }
            }
        }

        public string StopAndSave()
        {
            try
            {
                IsRecording = false;
                cleanupTimer.Stop();

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    ffmpegProcess.Kill();
                    ffmpegProcess.WaitForExit();
                }

                // Wait a short moment to ensure last chunk is flushed
                Thread.Sleep(300);

                string finalFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                                $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mkv");

                string[] chunkFiles;
                lock (chunkLock)
                {
                    chunkFiles = Directory.GetFiles(tempFolder, "chunk_*.mkv")
                                          .Where(f => new FileInfo(f).Length > 1000)
                                          .OrderBy(f => f)
                                          .ToArray();
                }

                if (chunkFiles.Length == 0)
                {
                    Log("No chunks found to save. Last chunk may not have flushed yet.");
                    return null;
                }

                string fileList = Path.Combine(tempFolder, "file_list.txt");
                File.WriteAllLines(fileList, Array.ConvertAll(chunkFiles, f => $"file '{f.Replace("'", "'\\''")}'"));

                var mergeProcess = new Process();
                mergeProcess.StartInfo.FileName = FfmpegPath;
                mergeProcess.StartInfo.Arguments = $"-f concat -safe 0 -i \"{fileList}\" -c copy \"{finalFile}\"";
                mergeProcess.StartInfo.UseShellExecute = false;
                mergeProcess.StartInfo.CreateNoWindow = true;
                mergeProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                mergeProcess.Start();
                mergeProcess.WaitForExit();

                if (!File.Exists(finalFile))
                    Log("FFmpeg failed to create final file.");

                Directory.Delete(tempFolder, true); // cleanup
                Log($"Saved recording to {finalFile}");
                return finalFile;
            }
            catch (Exception ex)
            {
                Log("Error saving recording: " + ex.Message);
                return null;
            }
        }

        public void Cancel()
        {
            try
            {
                IsRecording = false;
                cleanupTimer.Stop();

                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    try
                    {
                        ffmpegProcess.StandardInput?.WriteLine("q");
                    }
                    catch { }

                    ffmpegProcess.WaitForExit(2000);
                }

                if (Directory.Exists(tempFolder))
                    Directory.Delete(tempFolder, true);

                Log("Recording cancelled and chunks deleted.");
            }
            catch (Exception ex)
            {
                Log("Error cancelling recording: " + ex.Message);
            }
        }
    }
}
