using ImGuiNET;
using Newtonsoft.Json;
using NLayer;
using Raylib_cs;
using rlImGui_cs;
using System.Numerics;
using System.Text.Json;
using static ImGuiNET.ImGui;
using static Raylib_cs.Raylib;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;

internal class MusicPlayer() : Viewport("Music Player")
{

	//private const string Url = "https://lofi.stream.laut.fm/lofi";
	//private const string Url = "https://nightride.fm/stream/nightride.mp3";
	private const string Url = "https://listen.moe/fallback";

	private AudioStream? _audioStream;
	private MpegFile? _mpegFile;
	private HttpClient? _httpClient;
	private Stream? _networkStream;

	private bool _isPlaying, _isConnecting;
	private float _volume = 0.2f;

	private RenderTexture2D _rt;
	private bool _rtInit;

	private const int FftSize = 1024;
	private const int VisualBins = 48;

	private readonly float[] _fft = new float[VisualBins], _samples = new float[FftSize];

	private int _sampleIdx;
	private readonly Lock _lock = new();
	private float _hueOffset;

	private CancellationTokenSource? _cts;
	private string _currentTitle = "Waiting for data...";
	private string _currentArtist = "";

	private async Task StartMetadataLoop(CancellationToken token)
	{
		while (!token.IsCancellationRequested)
		{
			try
			{
				using var client = new ClientWebSocket();
				var uri = new Uri("wss://listen.moe/gateway_v2");
				await client.ConnectAsync(uri, token);

				// Simple heartbeat task to keep connection alive
				_ = Task.Run(async () =>
				{
					try
					{
						while (client.State == WebSocketState.Open && !token.IsCancellationRequested)
						{
							await Task.Delay(30000, token);
							if (client.State == WebSocketState.Open)
							{
								var heartbeat = Encoding.UTF8.GetBytes("{\"op\": 2}");
								await client.SendAsync(new ArraySegment<byte>(heartbeat), WebSocketMessageType.Text, true, token);
							}
						}
					}
					catch { /**/ }
				}, token);

				var buffer = new byte[4096];
				var messageBuffer = new List<byte>();

				while (client.State == WebSocketState.Open && !token.IsCancellationRequested)
				{
					WebSocketReceiveResult result;
					do
					{
						result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), token);
						if (result.MessageType == WebSocketMessageType.Close) break;
						messageBuffer.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
					} while (!result.EndOfMessage);

					if (result.MessageType == WebSocketMessageType.Close) break;

					var msg = Encoding.UTF8.GetString(messageBuffer.ToArray());
					messageBuffer.Clear();

					using var doc = JsonDocument.Parse(msg);
					var root = doc.RootElement;

					// op 1 is current song info
					if (root.TryGetProperty("op", out var op) && op.GetInt32() == 1)
					{
						var data = root.GetProperty("d");
						if (data.TryGetProperty("song", out var song))
						{
							_currentTitle = song.GetProperty("title").GetString() ?? "Unknown";
							if (song.TryGetProperty("artists", out var artists) && artists.GetArrayLength() > 0)
							{
								_currentArtist = artists[0].GetProperty("name").GetString() ?? "";
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[MusicPlayer] Metadata WebSocket Error: {ex.Message}");
				await Task.Delay(5000, token);
			}
		}
	}

	private static string GetPath()
	{

		PathUtil.ValidateFile(
			"Layouts/MusicPlayer.json",
			out var path,
			"""
            {
                "Volume": 0.05,
                "IsPlaying": true
            }
            """
		);

		return path;
	}

	public void Load()
	{

		var path = GetPath();

		if (!File.Exists(path)) return;

		try
		{

			var settings = JsonConvert.DeserializeObject<MusicPlayerSettings>(File.ReadAllText(path));
			_volume = settings.Volume;
			if (settings.IsPlaying) Play();

		}
		catch
		{
			/**/
		}
	}

	public void Save()
	{

		var path = GetPath();
		var dir = Path.GetDirectoryName(path);
		if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
		var settings = new MusicPlayerSettings { Volume = _volume, IsPlaying = _isPlaying };
		File.WriteAllText(path, JsonConvert.SerializeObject(settings, Formatting.Indented));
	}

	private struct MusicPlayerSettings
	{
		public float Volume;
		public bool IsPlaying;
	}

	private void Play()
	{

		if (_isPlaying || _isConnecting) return;

		_isConnecting = true;
		_cts = new CancellationTokenSource();

		var token = _cts.Token;

		Task.Run(
			async () => {

				try
				{
					_ = StartMetadataLoop(token);

					if (!IsAudioDeviceReady()) InitAudioDevice();

					// Standard buffer size for Linux stability, increased for background resilience
					const int streamBufferSize = 8192;
					SetAudioStreamBufferSizeDefault(streamBufferSize);

					_httpClient = new HttpClient();
					// Browser-like User-Agent
					_httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

					_networkStream = await _httpClient.GetStreamAsync(Url, token);

					// Wrap in a simple seekable buffer
					var streamingBuffer = new StreamingMemoryBuffer(_networkStream);
					_mpegFile = new MpegFile(streamingBuffer);

					var channels = _mpegFile.Channels;
					var sampleRate = _mpegFile.SampleRate;

					_audioStream = LoadAudioStream((uint)sampleRate, 32, (uint)channels);
					SetAudioStreamVolume(_audioStream.Value, _volume);

					// Buffer to decode into
					var decodeBuffer = new float[streamBufferSize * channels];

					// PRE-ROLL: Ensure we have enough data initially
					while (streamingBuffer.Length < 128 * 1024 && !token.IsCancellationRequested) await Task.Delay(100, token);

					PlayAudioStream(_audioStream.Value);
					_isPlaying = true;
					_isConnecting = false;

					// This decouples audio from the GUI frame rate, eliminating stuttering.
					while (!token.IsCancellationRequested && _isPlaying)
					{

						if (IsAudioStreamProcessed(_audioStream.Value))
						{

							var read = _mpegFile.ReadSamples(decodeBuffer, 0, decodeBuffer.Length);

							if (read > 0)
							{

								unsafe
								{

									fixed (float* p = decodeBuffer) UpdateAudioStream(_audioStream.Value, p, read / channels);
								}

								// Update visualization samples
								lock (_lock)
								{

									for (var i = 0; i < read && _sampleIdx < FftSize; i += channels) _samples[_sampleIdx++] = decodeBuffer[i];
								}
							}
						}

						// Tight loop for audio pumping, but yield to OS
						await Task.Delay(5, token);
					}
				}
				catch (Exception ex)
				{

					Console.WriteLine($"[MusicPlayer] Error: {ex.Message}");
					Cleanup();
				}
				finally
				{
					_isConnecting = false;
				}

			},
			token
		);
	}

	private void Stop() => Cleanup();

	private void Cleanup()
	{
		_isPlaying = false;
		_cts?.Cancel();
		_cts = null;

		CleanupAudio();
	}

	private void CleanupAudio()
	{
		if (_audioStream.HasValue)
		{
			StopAudioStream(_audioStream.Value);
			UnloadAudioStream(_audioStream.Value);
			_audioStream = null;
		}

		_mpegFile?.Dispose();
		_mpegFile = null;
		_networkStream?.Dispose();
		_networkStream = null;
		_httpClient?.Dispose();
		_httpClient = null;
	}

	protected override void OnDraw()
	{

		Spacing();

		if (_isPlaying)
		{
			PushFont(Fonts.ImJapaneseFont);
			TextUnformatted(_currentTitle);
			if (!string.IsNullOrEmpty(_currentArtist))
			{
				SameLine();
				TextDisabled($"- {_currentArtist}");
			}
			PopFont();
		}
		else
		{
			TextDisabled("Listen.moe Fallback Stream");
		}

		BeginGroup();

		if (_isConnecting)
		{

			const float radius = 10;
			var pos = GetCursorScreenPos() + new Vector2(15, 12);
			GetWindowDrawList().PathArcTo(pos, radius, (float)Raylib.GetTime() * 5.0f, (float)Raylib.GetTime() * 5.0f + 4.5f, 10);
			GetWindowDrawList().PathStroke(GetColorU32(ImGuiCol.Text), ImDrawFlags.None, 2.0f);
			Dummy(new Vector2(30, 24));

		}
		else
		{

			PushFont(Fonts.ImFontAwesomeNormal);
			if (!_isPlaying && Button(Icons.FaPlay, new Vector2(30, 24))) Play();
			if (_isPlaying && Button(Icons.FaStop, new Vector2(30, 24))) Stop();
			PopFont();
		}

		SameLine();
		SetNextItemWidth(GetContentRegionAvail().X - 10);

		if (SliderFloat("##Vol", ref _volume, 0, 1, "VOL %.2f"))
		{

			_volume = MathF.Round(_volume / 0.05f) * 0.05f;
			if (_audioStream.HasValue) SetAudioStreamVolume(_audioStream.Value, _volume);
		}

		EndGroup();

		// Main thread only handles visualization
		var winSize = GetContentRegionAvail();

		if (winSize.X < 5 || winSize.Y < 5) return;

		if (!_rtInit || _rt.Texture.Width != (int)winSize.X || _rt.Texture.Height != (int)winSize.Y)
		{

			if (_rtInit) UnloadRenderTexture(_rt);
			_rt = LoadRenderTexture(Math.Max(1, (int)winSize.X), Math.Max(1, (int)winSize.Y));
			_rtInit = true;
		}

		UpdateFft();

		BeginTextureMode(_rt);
		ClearBackground(new Color(15, 15, 18, 255));
		DrawVisualizer((int)winSize.X, (int)winSize.Y);
		EndTextureMode();

		rlImGui.ImageRenderTexture(_rt);
	}

	private void UpdateFft()
	{

		const int step = FftSize / VisualBins;

		float[] snap;

		lock (_lock)
		{
			snap = (float[])_samples.Clone();
			_sampleIdx = 0;
		}

		var dt = GetIO().DeltaTime;

		for (var i = 0; i < VisualBins; i++)
		{
			var sum = 0f;
			for (var j = 0; j < step; j++) sum += Math.Abs(snap[Math.Clamp(i * step + j, 0, FftSize - 1)]);
			var val = sum / step;
			_fft[i] = Raymath.Lerp(_fft[i], val, dt * (val > _fft[i] ? 20.0f : 10.0f));
		}

		_hueOffset += (10.0f + _fft.Sum() * 0.5f) * dt;
	}

	private void DrawVisualizer(int w, int h)
	{

		const float padding = 1.5f;

		var barWidth = (float)w / VisualBins;

		for (var i = 0; i < VisualBins; i++)
		{

			var val = Math.Clamp(_fft[i] * h * 2.5f, 2, h * 0.9f);
			var colorTop = ColorFromHSV(((float)i / VisualBins * 45.0f + _hueOffset) % 360, 0.6f, 0.9f);
			var colorBottom = ColorFromHSV(((float)i / VisualBins * 45.0f + _hueOffset + 25) % 360, 0.7f, 0.4f);
			var rect = new Rectangle(i * barWidth + padding, h - val, barWidth - padding * 2, val);
			DrawRectangleGradientV((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height, colorTop, colorBottom);
			DrawRectangle((int)rect.X, (int)rect.Y, (int)rect.Width, 1, Fade(Color.White, 0.3f));
		}
	}

	// A thread-safe seekable buffer that fills from a network stream.
	private class StreamingMemoryBuffer : Stream
	{

		private readonly MemoryStream _memory = new();
		private readonly Stream _source;

		private long _position;
		private readonly Lock _sync = new();
		public bool IsEnded { get; private set; }

		public StreamingMemoryBuffer(Stream source)
		{

			_source = source;

			Task.Run(async () => {

				var buffer = new byte[32768];

				try
				{

					while (true)
					{

						var read = await _source.ReadAsync(buffer, 0, buffer.Length);

						if (read <= 0) break;

						lock (_sync)
						{

							var oldPos = _memory.Position;
							_memory.Position = _memory.Length;
							_memory.Write(buffer, 0, read);
							_memory.Position = oldPos;
						}
					}

				}
				catch
				{
					/**/
				}
				finally
				{
					IsEnded = true;
				}
			}
			);
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;

		public override long Length
		{
			get
			{
				lock (_sync) return _memory.Length;
			}
		}

		public override long Position
		{
			get => _position;
			set => Seek(value, SeekOrigin.Begin);
		}

		public override void Flush() { }

		public override int Read(byte[] buffer, int offset, int count)
		{

			// Block if we haven't downloaded enough yet, but don't wait forever if the source is dead
			while (_position + count > Length && !IsEnded)
			{

				Thread.Sleep(1);

				if (count == 0 || IsEnded) break;
			}

			lock (_sync)
			{

				_memory.Position = _position;
				var read = _memory.Read(buffer, offset, count);
				_position += read;

				return read;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{

			lock (_sync)
			{

				_position = origin switch
				{

					SeekOrigin.Begin => offset,
					SeekOrigin.Current => _position + offset,
					SeekOrigin.End => _memory.Length + offset,
					_ => _position
				};

				return _position;
			}
		}

		public override void SetLength(long value) { }
		public override void Write(byte[] buffer, int offset, int count) { }

		protected override void Dispose(bool disposing)
		{

			if (disposing)
			{

				lock (_sync) _memory.Dispose();
				_source.Dispose();
			}

			base.Dispose(disposing);
		}
	}
}