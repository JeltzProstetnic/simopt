"""SIM-130 D2, D3, D6: a small synchronous MCP client for the SimOpt server's stdio transport.

The server speaks newline-delimited JSON-RPC 2.0. A tool answers with one text content holding the tool's
own JSON; a refusal is that JSON with an "error" field (and, for a model, "validation").
"""
import json
import os
import queue
import shutil
import subprocess
import threading
from collections import deque
from pathlib import Path

from ._results import Model

PROTOCOL_VERSION = "2025-06-18"
_REPOSITORY = Path(__file__).resolve().parents[2]
_BUILD = "dotnet build src/SimOpt.McpServer -c Release"


class SimOptError(Exception):
    """The server refused a request. ``validation`` holds the model's report when there is one."""

    def __init__(self, message, validation=None, answer=None):
        super().__init__(message)
        self.message = message
        self.validation = validation
        self.answer = answer


class ServerError(Exception):
    """The server could not be found or started, or stopped answering."""


def server_command(environ=None, repository=None):
    """D3: the command that starts the server: SIMOPT_SERVER, else the repository's newest build (a
    Release left from an older build would answer without the newer tools)."""
    environ = os.environ if environ is None else environ
    repository = Path(repository) if repository else _REPOSITORY
    named = environ.get("SIMOPT_SERVER")
    if named:
        path = Path(named)
        if not path.is_file():
            raise ServerError(f"SIMOPT_SERVER names {path}, which does not exist. Build the server with "
                              f"'{_BUILD}' in the SimOpt repository, or point SIMOPT_SERVER at it.")
        return [_dotnet(environ), str(path)] if path.suffix == ".dll" else [str(path)]
    builds = [repository / "src" / "SimOpt.McpServer" / "bin" / configuration / "net9.0" / "SimOpt.McpServer.dll"
              for configuration in ("Release", "Debug")]
    built = [dll for dll in builds if dll.is_file()]
    if built:
        return [_dotnet(environ), str(max(built, key=lambda dll: dll.stat().st_mtime))]
    raise ServerError(f"No SimOpt MCP server found. Build it with '{_BUILD}' in {repository}, "
                      "or set SIMOPT_SERVER to its .dll or executable.")


def _dotnet(environ):
    """DOTNET_ROOT when set, else ~/.dotnet when it holds dotnet, else dotnet on the PATH."""
    name = "dotnet.exe" if os.name == "nt" else "dotnet"
    if environ.get("DOTNET_ROOT"):
        return str(Path(environ["DOTNET_ROOT"]) / name)
    home = Path.home() / ".dotnet" / name
    return str(home) if home.is_file() else shutil.which("dotnet") or name


class Server:
    """A running SimOpt MCP server. Use it as a context manager, so that the process ends with the block.

    ``timeout`` is the longest wait, in seconds, for one answer; a long search needs a long one.
    """

    def __init__(self, command=None, timeout=3600):
        self._timeout = timeout
        self._next_id = 0
        self._stderr = deque(maxlen=40)
        command = command or server_command()
        environ = dict(os.environ)
        if Path(command[0]).stem == "dotnet":
            environ.setdefault("DOTNET_ROOT", str(Path(command[0]).parent))
        try:
            self._process = subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                                             stderr=subprocess.PIPE, text=True, encoding="utf-8", env=environ)
        except OSError as failure:
            raise ServerError(f"Could not start the server ({' '.join(command)}): {failure}") from failure
        self._lines = queue.Queue()
        threading.Thread(target=self._read_answers, daemon=True).start()
        self._stderr_reader = threading.Thread(target=self._read_stderr, daemon=True)
        self._stderr_reader.start()
        self._request("initialize", {"protocolVersion": PROTOCOL_VERSION, "capabilities": {},
                                     "clientInfo": {"name": "simopt-python", "version": "0.1.0"}})
        self._send({"jsonrpc": "2.0", "method": "notifications/initialized"})

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.close()

    def call(self, tool, arguments=None):
        """Calls a tool and returns its answer as data; raises SimOptError when the tool refuses."""
        result = self._request("tools/call", {"name": tool, "arguments": arguments or {}})
        text = "".join(c.get("text", "") for c in result.get("content", []) if c.get("type") == "text")
        try:
            answer = json.loads(text)
        except ValueError:
            answer = text
        if result.get("isError"):
            raise SimOptError(text or f"{tool} failed", answer=answer)
        if isinstance(answer, dict) and "error" in answer:
            raise SimOptError(answer["error"], validation=answer.get("validation"), answer=answer)
        return answer

    # ── the tools (D4) ──

    def validate(self, topology):
        """validate_model: the report on a topology, every fault at once, without registering it."""
        return self.call("validate_model", {"topology": topology})

    def create_model(self, topology):
        """create_model: registers the topology and returns its Model; a faulty one raises SimOptError."""
        answer = self.call("create_model", {"topology": topology})
        return Model(self, answer["model_id"], 1, answer)

    def load_model(self, path):
        """load_model (SIM-67): opens a saved topology file as create_model would and returns its Model."""
        answer = self.call("load_model", {"path": str(path)})
        return Model(self, answer["model_id"], 1, answer)

    def reproduce_run(self, path):
        """reproduce_run (SIM-86): repeats a run record on this server and reports record_intact,
        model_intact, identical and any differences."""
        return self.call("reproduce_run", {"path": str(path)})

    def schema(self):
        """get_schema: node types, parameters, metric kinds, distributions and the JSON Schema."""
        return self.call("get_schema")

    def models(self):
        """list_models: the ids of the models registered on this server."""
        return self.call("list_models")

    def close(self):
        """Ends the server: closes its input, then stops it if it does not exit within five seconds."""
        if self._process.poll() is None:
            try:
                self._process.stdin.close()
                self._process.wait(timeout=5)
            except (OSError, subprocess.TimeoutExpired):
                self._process.kill()
                self._process.wait()

    # ── JSON-RPC ──

    def _request(self, method, params):
        self._next_id += 1
        request_id = self._next_id
        self._send({"jsonrpc": "2.0", "id": request_id, "method": method, "params": params})
        while True:
            message = self._receive()
            if message.get("id") != request_id or "method" in message:
                continue  # a notification or a request from the server; nothing here needs one
            if "error" in message:
                raise ServerError(f"{method}: {message['error'].get('message', message['error'])}")
            return message.get("result", {})

    def _send(self, message):
        try:
            self._process.stdin.write(json.dumps(message) + "\n")
            self._process.stdin.flush()
        except OSError as failure:
            raise self._dead() from failure

    def _receive(self):
        try:
            line = self._lines.get(timeout=self._timeout)
        except queue.Empty:
            raise ServerError(f"The server gave no answer within {self._timeout} s.") from None
        if line is None:
            raise self._dead()
        return json.loads(line)

    def _dead(self):
        try:
            self._process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            pass
        self._stderr_reader.join(timeout=5)
        said = "".join(self._stderr).strip()
        return ServerError(f"The server stopped (exit code {self._process.returncode})" +
                           (f": {said}" if said else "."))

    def _read_answers(self):
        for line in self._process.stdout:
            self._lines.put(line)
        self._lines.put(None)  # the server closed its output

    def _read_stderr(self):
        for line in self._process.stderr:
            self._stderr.append(line)
