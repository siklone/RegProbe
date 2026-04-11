#!/usr/bin/env python3
import argparse
import http.server
import os
import posixpath
import urllib.parse


class GuestBridgeHandler(http.server.SimpleHTTPRequestHandler):
    server_version = "RegProbeGuestBridge/1.0"

    def translate_path(self, path: str) -> str:
        path = urllib.parse.urlparse(path).path
        path = urllib.parse.unquote(path)
        path = posixpath.normpath(path)
        words = [word for word in path.split("/") if word]
        translated = self.server.serve_root
        for word in words:
            if word in {".", ".."}:
                continue
            translated = os.path.join(translated, word)
        return translated

    def do_GET(self):
        if self.path == "/healthz":
            self.send_response(200)
            self.end_headers()
            self.wfile.write(b"ok")
            return
        return super().do_GET()

    def do_PUT(self):
        relative = urllib.parse.urlparse(self.path).path.lstrip("/")
        relative = urllib.parse.unquote(relative)
        safe_parts = [part for part in relative.split("/") if part and part not in {".", ".."}]
        if not safe_parts:
            safe_parts = ["upload.bin"]

        target = os.path.join(self.server.upload_root, *safe_parts)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        length = int(self.headers.get("Content-Length", "0"))
        with open(target, "wb") as handle:
            remaining = length
            while remaining > 0:
                chunk = self.rfile.read(min(65536, remaining))
                if not chunk:
                    break
                handle.write(chunk)
                remaining -= len(chunk)

        self.send_response(200)
        self.end_headers()
        self.wfile.write(target.encode("utf-8"))


def main() -> None:
    parser = argparse.ArgumentParser(description="Serve guest downloads and accept guest artifact uploads.")
    parser.add_argument("--host", default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--serve-root", default=".")
    parser.add_argument("--upload-root", default="/tmp/regprobe-upload")
    args = parser.parse_args()

    class BridgeServer(http.server.ThreadingHTTPServer):
        serve_root = os.path.abspath(args.serve_root)
        upload_root = os.path.abspath(args.upload_root)

    os.makedirs(BridgeServer.upload_root, exist_ok=True)
    with BridgeServer((args.host, args.port), GuestBridgeHandler) as httpd:
        print(
            f"serving {BridgeServer.serve_root} with uploads in {BridgeServer.upload_root} on {args.host}:{args.port}",
            flush=True,
        )
        httpd.serve_forever()


if __name__ == "__main__":
    main()
