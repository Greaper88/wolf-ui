"""Disposable Unix-socket Wolf fixture. Never connects to Docker or a real Wolf server."""
import http.server
import json
import os
import socketserver
import sys
import time

root = sys.argv[1]
state_path = root + "/state.json"


def state():
    with open(state_path) as f:
        return json.load(f)


class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def reply(self, body, status=200):
        body = json.dumps(body).encode()
        self.send_response(status)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        current = state()
        if self.path.endswith("/events"):
            self.send_response(200)
            self.send_header("Content-Type", "text/event-stream")
            self.end_headers()
            try:
                while True:
                    self.wfile.write(b":keepalive\n\n")
                    self.wfile.flush()
                    time.sleep(0.25)
            except (BrokenPipeError, ConnectionResetError):
                pass
        elif self.path.endswith("/profiles"):
            self.reply(dict(success=True, profiles=[dict(
                id=owner, name=owner, apps=[dict(id=title.lower(), title=title,
                    runner=dict(type="docker", name="Wolf" + title, image="test/steam"))
                    for title in ("Steam", "Firefox", "Desktop", "RetroArch", "Kodi")]
            ) for owner in ("alice", "bob")]))
        elif self.path.endswith("/sessions"):
            self.reply(dict(success=True, sessions=[]))
        elif "/docker/images/inspect" in self.path:
            self.reply(dict(Id="test-image"))
        elif self.path.endswith("/lobbies"):
            if current.get("delay_lobbies"):
                open(root + "/snapshot-started", "w").close()
                time.sleep(current["delay_lobbies"])
            self.reply(dict(success=True, lobbies=current["lobbies"]),
                       503 if current.get("fail_lobbies") else 200)
        else:
            self.reply(dict(error=self.path), 404)

    def do_POST(self):
        body = json.loads(self.rfile.read(int(self.headers.get("Content-Length", 0))))
        current = state()
        if self.path.endswith("/lobbies/create"):
            # Existing persistent app: deliberately no create event.
            self.reply(dict(success=True, lobby_id="deduplicated-app"))
        elif self.path.endswith("/lobbies/join"):
            self.reply(dict(success=True))
        elif self.path.endswith("/lobbies/stop"):
            with open(root + "/stopped.json", "w") as f:
                json.dump(body, f)
            self.reply(dict(success=not current.get("fail_stop"), error="test stop failure"))
        else:
            self.reply(dict(error=self.path), 404)


class Server(socketserver.ThreadingMixIn, socketserver.UnixStreamServer):
    daemon_threads = True


socket_path = root + "/wolf.sock"
if os.path.exists(socket_path):
    os.unlink(socket_path)
with Server(socket_path, Handler) as server:
    server.serve_forever()
