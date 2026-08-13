using System.Buffers.Binary;

namespace GincanaHud.App.Services;

/// <summary>Campanas sintéticas + vibración al capturar un POI.</summary>
public sealed class CaptureFxService(IPlayerSettings settings) : ICaptureFxService
{
	private static readonly object Gate = new();
	private byte[]? _wavBytes;
	private bool _warming;
	private long _lastProximityHapticMs;

	public void PlayCaptureSuccess()
	{
		if (!settings.SoundEnabled)
			return;

		try
		{
			HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
		}
		catch
		{
			/* dispositivo sin háptica */
		}

		_ = PlayChimeAsync();
	}

	public void TickProximity(double distanceMeters)
	{
		if (!settings.SoundEnabled)
			return;

		// 15 m → ~900 ms; 0 m → ~180 ms
		var t = Math.Clamp(distanceMeters / 15.0, 0, 1);
		var intervalMs = (long)(180 + t * 720);
		var now = Environment.TickCount64;
		if (now - _lastProximityHapticMs < intervalMs)
			return;
		_lastProximityHapticMs = now;

		try
		{
			HapticFeedback.Default.Perform(HapticFeedbackType.Click);
		}
		catch
		{
			/* ignore */
		}
	}

	private async Task PlayChimeAsync()
	{
		try
		{
			var wav = await EnsureWavAsync().ConfigureAwait(false);
			var cache = Path.Combine(FileSystem.CacheDirectory, "capture_chime.wav");
			if (!File.Exists(cache) || new FileInfo(cache).Length != wav.Length)
				await File.WriteAllBytesAsync(cache, wav).ConfigureAwait(false);

#if ANDROID
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				var player = new Android.Media.MediaPlayer();
				player.SetDataSource(cache);
				player.Prepare();
				player.Completion += (_, _) =>
				{
					player.Release();
					player.Dispose();
				};
				player.Error += (_, _) =>
				{
					player.Release();
					player.Dispose();
				};
				player.Start();
			});
#else
			await Task.CompletedTask;
#endif
		}
		catch
		{
			/* el feedback visual sigue valiendo si el audio falla */
		}
	}

	private Task<byte[]> EnsureWavAsync()
	{
		lock (Gate)
		{
			if (_wavBytes is not null)
				return Task.FromResult(_wavBytes);
			if (_warming)
				return Task.Run(() =>
				{
					while (_wavBytes is null)
						Thread.Sleep(10);
					return _wavBytes;
				});
			_warming = true;
		}

		return Task.Run(() =>
		{
			var bytes = BuildChimeWav();
			lock (Gate)
			{
				_wavBytes = bytes;
				_warming = false;
			}
			return bytes;
		});
	}

	/// <summary>Tres golpes de campana (parciales + decay) en WAV PCM 16-bit mono.</summary>
	internal static byte[] BuildChimeWav()
	{
		const int sampleRate = 22050;
		const double durationSec = 1.35;
		var n = (int)(sampleRate * durationSec);
		var pcm = new short[n];

		ReadOnlySpan<(double Start, double[] Freqs, double Amp)> strikes =
		[
			(0.00, [880.0, 1760.0, 2640.0], 1.0),
			(0.18, [1174.7, 2349.3], 0.85),
			(0.38, [1318.5, 2637.0, 3955.0], 0.7),
		];

		for (var i = 0; i < n; i++)
		{
			var t = i / (double)sampleRate;
			var v = 0.0;
			foreach (var (start, freqs, amp) in strikes)
			{
				if (t < start)
					continue;
				var local = t - start;
				var env = Math.Exp(-3.2 * local) * (1.0 - Math.Exp(-80 * local));
				for (var h = 0; h < freqs.Length; h++)
				{
					var harm = 1.0 / (h + 1);
					v += amp * harm * env * Math.Sin(2 * Math.PI * freqs[h] * local);
				}
			}

			v = Math.Tanh(v * 0.55) * 0.85;
			pcm[i] = (short)Math.Clamp((int)Math.Round(v * 32767), short.MinValue, short.MaxValue);
		}

		var dataBytes = n * 2;
		var wav = new byte[44 + dataBytes];
		var span = wav.AsSpan();
		"RIFF"u8.CopyTo(span);
		BinaryPrimitives.WriteInt32LittleEndian(span[4..], 36 + dataBytes);
		"WAVE"u8.CopyTo(span[8..]);
		"fmt "u8.CopyTo(span[12..]);
		BinaryPrimitives.WriteInt32LittleEndian(span[16..], 16);
		BinaryPrimitives.WriteInt16LittleEndian(span[20..], 1);
		BinaryPrimitives.WriteInt16LittleEndian(span[22..], 1);
		BinaryPrimitives.WriteInt32LittleEndian(span[24..], sampleRate);
		BinaryPrimitives.WriteInt32LittleEndian(span[28..], sampleRate * 2);
		BinaryPrimitives.WriteInt16LittleEndian(span[32..], 2);
		BinaryPrimitives.WriteInt16LittleEndian(span[34..], 16);
		"data"u8.CopyTo(span[36..]);
		BinaryPrimitives.WriteInt32LittleEndian(span[40..], dataBytes);
		Buffer.BlockCopy(pcm, 0, wav, 44, dataBytes);
		return wav;
	}
}
