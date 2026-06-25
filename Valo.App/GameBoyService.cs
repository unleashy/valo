using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Valo.App;

public sealed class GameBoyService(ILcd lcd)
{
    private GameBoy? _gameBoy;

    public void LoadCartridge(ReadOnlySpan<byte> cartridgeData)
    {
        _gameBoy = GameBoy.Create(Cartridge.FromBytes(cartridgeData), lcd);
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var msPerFrameTs = TimeSpan.FromMilliseconds(GameBoy.MsPerFrame);

        await Task.Run(() => {
            var sw = new Stopwatch();

            while (!(ct.IsCancellationRequested || _gameBoy == null)) {
                sw.Restart();

                _gameBoy.EmulateSingleFrame();

                Sleep.Precisely(msPerFrameTs - sw.Elapsed);
            }
        }, ct);
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static partial class Sleep
{
    public static void Precisely(TimeSpan span)
    {
        var timer = Timer.Value;
        if (timer == 0) {
            // fall back to standard sleep with no high resolution timer
            Thread.Sleep(span);
            return;
        }

        unsafe {
            var dueTime = new LARGE_INTEGER { QuadPart = -(span.Nanoseconds / 100) };
            if (SetWaitableTimerEx(timer, &dueTime, 0, 0, 0, 0, 0)) {
                _ = WaitForSingleObject(timer, INFINITE);
            }
        }
    }

    private static readonly ThreadLocal<nint> Timer =
        new(() =>
            // CREATE_WAITABLE_TIMER_HIGH_RESOLUTION requires Windows version 1803
            // Using build 17763 here (version 1809) because it's still supported unlike 1803
            OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763)
                ? CreateWaitableTimerExW(
                    0,
                    0,
                    CREATE_WAITABLE_TIMER_HIGH_RESOLUTION,
                    SYNCHRONIZE | TIMER_MODIFY_STATE
                )
                : 0
        );

    [LibraryImport("kernel32.dll")]
    private static partial nint CreateWaitableTimerExW(
        nint lpTimerAttributes,
        nint lpTimerName,
        uint dwFlags,
        ulong dwDesiredAccess
    );

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static unsafe partial bool SetWaitableTimerEx(
        nint hTimer,
        LARGE_INTEGER* lpDueTime,
        int lPeriod,
        nint lpCompletionRoutine,
        nint lpArgToCompletionRoutine,
        nint WakeContext,
        uint TolerableDelay
    );

    [LibraryImport("kernel32.dll")]
    private static partial uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct LARGE_INTEGER
    {
        [FieldOffset(0)]
        public long QuadPart;
    }

    private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002;
    private const ulong SYNCHRONIZE = 0x00100000L;
    private const ulong TIMER_MODIFY_STATE = 0x0002;
    private const uint INFINITE = 0xFFFFFFFF;
}
