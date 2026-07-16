using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.AspNetCore;

namespace CapabilityCentricSample.Hosting.API.Controllers.Diagnostics;

/// <summary>
/// Experimental upload/download endpoints for observing FileStorage streaming behaviour
/// (large binaries / video playback in the browser, Visual Studio Memory Usage).
/// </summary>
[ApiController]
[Route("api/diagnostics/file-storage")]
public sealed class FileStorageDiagnosticsController(
    IFileStorage fileStorage,
    ILogger<FileStorageDiagnosticsController> logger) : ControllerBase
{
    /// <summary>Fixed key used by the diagnostics upload/download pair.</summary>
    public const string DemoKey = "diagnostics_stream_demo_file";

    /// <summary>220 MiB — headroom above a 200 MiB test file.</summary>
    public const long MaxDemoBytes = 220L * 1024 * 1024;

    private static readonly FilePutConstraints LargeMediaConstraints =
        FilePutConstraints.For(
            "application/octet-stream",
            "video/mp4",
            "video/webm") with
        { MaxUploadBytes = MaxDemoBytes };

    /// <summary>
    /// Simple HTML page: upload a video, then play it via the streaming download endpoint.
    /// Open <c>/api/diagnostics/file-storage/watch</c> in the browser.
    /// </summary>
    [HttpGet("watch")]
    [Produces("text/html")]
    public ContentResult Watch()
    {
        const string html =
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <title>FileStorage stream demo</title>
              <link rel="preconnect" href="https://fonts.googleapis.com" />
              <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
              <link href="https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;1,9..40,400&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet" />
              <style>
                :root {
                  --bg: #0f1419;
                  --surface: #1a222c;
                  --surface-2: #243040;
                  --border: #334155;
                  --text: #e8eef4;
                  --muted: #94a3b8;
                  --accent: #3d9a7a;
                  --accent-hover: #4db892;
                  --danger: #e07a6a;
                  --radius: 12px;
                  --shadow: 0 18px 50px rgba(0, 0, 0, 0.35);
                }
                * { box-sizing: border-box; }
                body {
                  margin: 0;
                  min-height: 100vh;
                  font-family: "DM Sans", system-ui, sans-serif;
                  color: var(--text);
                  background:
                    radial-gradient(1200px 600px at 10% -10%, #1c3a32 0%, transparent 55%),
                    radial-gradient(900px 500px at 100% 0%, #1a2838 0%, transparent 50%),
                    var(--bg);
                }
                main {
                  width: min(920px, calc(100% - 2rem));
                  margin: 0 auto;
                  padding: 2.5rem 0 3rem;
                }
                header { margin-bottom: 1.75rem; }
                .eyebrow {
                  margin: 0 0 0.5rem;
                  font-size: 0.75rem;
                  font-weight: 600;
                  letter-spacing: 0.08em;
                  text-transform: uppercase;
                  color: var(--accent);
                }
                h1 {
                  margin: 0 0 0.6rem;
                  font-size: clamp(1.6rem, 3vw, 2.1rem);
                  font-weight: 700;
                  letter-spacing: -0.02em;
                }
                .lede {
                  margin: 0;
                  max-width: 42rem;
                  color: var(--muted);
                  line-height: 1.55;
                }
                .panel {
                  background: color-mix(in srgb, var(--surface) 92%, transparent);
                  border: 1px solid var(--border);
                  border-radius: calc(var(--radius) + 4px);
                  box-shadow: var(--shadow);
                  overflow: hidden;
                  backdrop-filter: blur(8px);
                }
                .player-wrap {
                  position: relative;
                  background: #05080c;
                  aspect-ratio: 16 / 9;
                }
                video {
                  display: block;
                  width: 100%;
                  height: 100%;
                  object-fit: contain;
                  background: #05080c;
                }
                .controls {
                  display: grid;
                  gap: 1rem;
                  padding: 1.25rem 1.35rem 1.4rem;
                  border-top: 1px solid var(--border);
                  background: var(--surface);
                }
                .file-row {
                  display: flex;
                  flex-wrap: wrap;
                  gap: 0.75rem;
                  align-items: center;
                }
                .file-picker {
                  flex: 1 1 220px;
                  position: relative;
                }
                .file-picker input {
                  position: absolute;
                  inset: 0;
                  opacity: 0;
                  cursor: pointer;
                }
                .file-label {
                  display: flex;
                  align-items: center;
                  gap: 0.65rem;
                  min-height: 2.75rem;
                  padding: 0.65rem 0.9rem;
                  border: 1px dashed var(--border);
                  border-radius: var(--radius);
                  background: var(--surface-2);
                  color: var(--muted);
                  font-size: 0.92rem;
                }
                .file-label strong { color: var(--text); font-weight: 600; }
                .actions { display: flex; flex-wrap: wrap; gap: 0.6rem; }
                button {
                  appearance: none;
                  border: 0;
                  border-radius: 999px;
                  padding: 0.7rem 1.15rem;
                  font: inherit;
                  font-weight: 600;
                  font-size: 0.92rem;
                  cursor: pointer;
                  transition: background 0.15s ease, transform 0.15s ease;
                }
                button:active { transform: translateY(1px); }
                .btn-primary { background: var(--accent); color: #04140f; }
                .btn-primary:hover { background: var(--accent-hover); }
                .btn-primary:disabled { opacity: 0.55; cursor: not-allowed; }
                .btn-ghost {
                  background: transparent;
                  color: var(--text);
                  border: 1px solid var(--border);
                }
                .btn-ghost:hover { background: var(--surface-2); }
                #status {
                  margin: 0;
                  padding: 0.85rem 1rem;
                  border-radius: var(--radius);
                  background: #0c1218;
                  border: 1px solid var(--border);
                  color: var(--muted);
                  font-family: "JetBrains Mono", ui-monospace, monospace;
                  font-size: 0.78rem;
                  line-height: 1.5;
                  white-space: pre-wrap;
                  min-height: 3.2rem;
                }
                #status[data-tone="ok"] { color: #9fd9c2; border-color: #2f6b56; }
                #status[data-tone="error"] { color: #f0b4ab; border-color: #8a453c; }
                .hint {
                  margin: 1rem 0 0;
                  color: var(--muted);
                  font-size: 0.85rem;
                }
                code {
                  font-family: "JetBrains Mono", ui-monospace, monospace;
                  font-size: 0.84em;
                  color: #c5d4e4;
                }
              </style>
            </head>
            <body>
              <main>
                <header>
                  <p class="eyebrow">FileStorage diagnostics</p>
                  <h1>Stream demo player</h1>
                  <p class="lede">
                    Upload a video to a fixed storage key, then play it back through a ranged stream.
                    Useful for checking memory while serving large media.
                  </p>
                </header>

                <section class="panel" aria-label="Video stream demo">
                  <div class="player-wrap">
                    <video id="player" controls playsinline></video>
                  </div>
                  <div class="controls">
                    <div class="file-row">
                      <div class="file-picker">
                        <div class="file-label" id="fileLabel">Choose a video… <strong>MP4 / WebM</strong></div>
                        <input id="file" type="file" accept="video/mp4,video/webm,video/*" />
                      </div>
                      <div class="actions">
                        <button id="upload" class="btn-primary" type="button">Upload</button>
                        <button id="reload" class="btn-ghost" type="button">Reload player</button>
                      </div>
                    </div>
                    <p id="status" data-tone="idle">Ready. Select a file, upload, then play.</p>
                  </div>
                </section>

                <p class="hint">
                  Endpoints: <code>POST …/upload</code> · <code>GET …/download</code> · key
                  <code>diagnostics_stream_demo_file</code>
                </p>
              </main>

              <script>
                const player = document.getElementById('player');
                const status = document.getElementById('status');
                const fileInput = document.getElementById('file');
                const fileLabel = document.getElementById('fileLabel');
                const uploadBtn = document.getElementById('upload');
                const downloadUrl = new URL('download', window.location.href);

                function setStatus(message, tone) {
                  status.textContent = message;
                  status.dataset.tone = tone || 'idle';
                }

                function formatBytes(bytes) {
                  if (bytes < 1024) return bytes + ' B';
                  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
                  return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
                }

                function loadPlayer() {
                  player.src = downloadUrl.pathname + '?t=' + Date.now();
                  player.load();
                }

                fileInput.addEventListener('change', () => {
                  const file = fileInput.files && fileInput.files[0];
                  if (!file) {
                    fileLabel.innerHTML = 'Choose a video… <strong>MP4 / WebM</strong>';
                    return;
                  }
                  fileLabel.innerHTML = '<strong>' + file.name + '</strong> · ' + formatBytes(file.size);
                });

                document.getElementById('reload').onclick = () => {
                  loadPlayer();
                  setStatus('Player reloaded from download stream.', 'idle');
                };

                uploadBtn.onclick = async () => {
                  const file = fileInput.files && fileInput.files[0];
                  if (!file) {
                    setStatus('Choose a video file first.', 'error');
                    return;
                  }

                  uploadBtn.disabled = true;
                  setStatus('Uploading ' + file.name + ' (' + formatBytes(file.size) + ')…', 'idle');

                  const form = new FormData();
                  form.append('file', file, file.name);

                  try {
                    const response = await fetch(new URL('upload', window.location.href), {
                      method: 'POST',
                      body: form
                    });
                    const text = await response.text();
                    if (!response.ok) {
                      setStatus('Upload failed (' + response.status + ')\n' + text, 'error');
                      return;
                    }
                    setStatus('Upload complete.\n' + text, 'ok');
                    loadPlayer();
                  } catch (err) {
                    setStatus('Network error: ' + err, 'error');
                  } finally {
                    uploadBtn.disabled = false;
                  }
                };
              </script>
            </body>
            </html>
            """;

        return Content(html, "text/html; charset=utf-8");
    }

    /// <summary>
    /// Uploads a large binary / video into FileStorage at a fixed key.
    /// Watch the log: bytes-read should advance gradually if the put path is streaming.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDemoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDemoBytes)]
    [ProducesResponseType(typeof(FileStorageDiagnosticsUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var upload = file.ToFileUploadContent();

        if (upload.ContentLength > MaxDemoBytes)
            return BadRequest($"File exceeds the diagnostics limit of {MaxDemoBytes} bytes.");

        var contentType = ResolveContentType(upload.FileName, upload.ContentType);

        await using var progress = new ProgressLoggingStream(
            upload.Content,
            logger,
            upload.ContentLength,
            leaveOpen: true);

        var put = await fileStorage.PutAsync(
            FileStoragePutHelper.CreateRequest(
                progress,
                contentType,
                LargeMediaConstraints,
                PutMode.Upsert,
                key: DemoKey,
                contentLength: upload.ContentLength),
            cancellationToken);

        return Ok(new FileStorageDiagnosticsUploadResponse(
            put.File.Key,
            put.File.Length ?? upload.ContentLength,
            put.File.ContentType,
            progress.BytesRead));
    }

    /// <summary>
    /// Streams the diagnostics object for inline browser playback (no Content-Disposition attachment).
    /// Range requests are enabled so video seeking works.
    /// </summary>
    [HttpGet("download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Download(CancellationToken cancellationToken)
        => fileStorage.OpenReadActionResultAsync(DemoKey, cancellationToken: cancellationToken);

    private static string ResolveContentType(string? fileName, string contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType)
            && !string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        return Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant() switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/mp4",
            _ => string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
        };
    }
}

public sealed record FileStorageDiagnosticsUploadResponse(
    string Key,
    long Length,
    string? ContentType,
    long BytesReadFromUploadStream);

/// <summary>Wraps a read stream and logs cumulative bytes every 10 MiB.</summary>
internal sealed class ProgressLoggingStream : Stream
{
    private const long LogIntervalBytes = 10L * 1024 * 1024;

    private readonly Stream _inner;
    private readonly ILogger _logger;
    private readonly long? _declaredLength;
    private readonly bool _leaveOpen;
    private long _nextLogAt = LogIntervalBytes;

    public ProgressLoggingStream(Stream inner, ILogger logger, long? declaredLength, bool leaveOpen = false)
    {
        _inner = inner;
        _logger = logger;
        _declaredLength = declaredLength;
        _leaveOpen = leaveOpen;
    }

    public long BytesRead { get; private set; }

    public override bool CanRead => _inner.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _declaredLength ?? _inner.Length;
    public override long Position
    {
        get => BytesRead;
        set => throw new NotSupportedException();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var read = await _inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        OnRead(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var read = await _inner.ReadAsync(buffer, cancellationToken);
        OnRead(read);
        return read;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = _inner.Read(buffer, offset, count);
        OnRead(read);
        return read;
    }

    public override void Flush() => _inner.Flush();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
            _inner.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_leaveOpen)
            await _inner.DisposeAsync();
        await base.DisposeAsync();
    }

    private void OnRead(int read)
    {
        if (read <= 0)
            return;

        BytesRead += read;
        while (BytesRead >= _nextLogAt)
        {
            _logger.LogInformation(
                "FileStorage diagnostics upload progress: {BytesRead:N0} / {Total:N0} bytes",
                BytesRead,
                _declaredLength);
            _nextLogAt += LogIntervalBytes;
        }
    }
}
