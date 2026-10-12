"""Local-only static preview server, with WebAssembly MIME type and no stale assets."""
import functools
import http.server
import pathlib
import sys
import os
import json
import threading
import urllib.request
import urllib.error

directory = pathlib.Path(sys.argv[1]).resolve()
port = int(sys.argv[2]) if len(sys.argv) > 2 else 4318
key = os.environ.get("TYPESAFE_API_KEY", "")
if not key and len(sys.argv) > 3 and sys.argv[3]:
    for line in pathlib.Path(sys.argv[3]).read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("TYPESAFE_API_KEY="):
            key = line.split("=", 1)[1].strip().strip("\"'")
requests_used = 0
input_tokens = 0
cost_usd = 0.0
lock = threading.Lock()
if not (directory / "index.html").is_file():
    raise SystemExit(f"Publish the browser project first: missing {directory / 'index.html'}")

class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {**http.server.SimpleHTTPRequestHandler.extensions_map, ".wasm": "application/wasm"}

    def json_reply(self, status, value):
        body = json.dumps(value).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def same_origin(self):
        return self.headers.get("Host") in (f"127.0.0.1:{port}", f"localhost:{port}") and self.headers.get("Origin", f"http://127.0.0.1:{port}") in (f"http://127.0.0.1:{port}", f"http://localhost:{port}")

    def do_GET(self):
        if self.path == "/jev/status":
            self.json_reply(200, {"available": bool(key), "requests": requests_used, "inputTokens": input_tokens,
                                  "dollars": cost_usd, "maxRequests": 20, "maxDollars": .02})
        else:
            super().do_GET()

    def do_POST(self):
        global requests_used, input_tokens, cost_usd
        if self.path != "/jev" or not self.same_origin():
            self.json_reply(403, {"error": "Only the local bath may use this bridge."}); return
        length = int(self.headers.get("Content-Length", "0"))
        if not key or not 1 <= length <= 20_000:
            self.json_reply(400, {"error": "Bridge unavailable or request exceeds the trial size limit."}); return
        try:
            request_body = self.rfile.read(length)
            parsed = json.loads(request_body)
            if parsed.get("model") != "jev-1.13.0" or set(parsed.get("questions", {})) != {"decision"} or parsed["questions"]["decision"].get("type") != "choice":
                raise ValueError("Expected the bounded choice protocol")
        except (ValueError, KeyError):
            self.json_reply(400, {"error": "Invalid trial request."}); return
        reserve = (length + 2000) * .000000042
        with lock:
            if requests_used >= 20 or cost_usd + reserve > .02:
                self.json_reply(429, {"error": "The 20-request / two-cent trial budget is exhausted."}); return
            requests_used += 1; cost_usd += reserve
        try:
            remote = urllib.request.Request("https://api.typesafe.ai/v1/systemone", data=request_body,
                headers={"Content-Type": "application/json", "Authorization": "Bearer " + key}, method="POST")
            with urllib.request.urlopen(remote, timeout=25) as result:
                reply = json.loads(result.read())
            tokens = int(reply["usage"]["input_tokens"])
            with lock:
                input_tokens += tokens; cost_usd += tokens * .000000042 - reserve
            self.json_reply(200, reply)
        except (urllib.error.URLError, TimeoutError, ValueError, KeyError):
            self.json_reply(502, {"error": "Jev request failed; reserved trial cost remains accounted."})

    def log_message(self, format, *args):
        # Access logs belong on stdout: Windows PowerShell treats native stderr as errors.
        print(f"{self.address_string()} - - [{self.log_date_time_string()}] {format % args}", flush=True)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")
        super().end_headers()

with http.server.ThreadingHTTPServer(("127.0.0.1", port), functools.partial(Handler, directory=str(directory))) as server:
    print(f"Gojo bath: http://127.0.0.1:{port}", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nPreview stopped.", flush=True)
