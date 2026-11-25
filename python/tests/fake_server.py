"""A stand-in for the SimOpt MCP server, speaking the same stdio protocol, for the client's unit tests.

FAKE_MODE chooses its behaviour:
  echo     answers every tool with {"tool": name, "arguments": arguments}, and the tool "methods" with
           the methods received so far
  refuse   answers every tool as the server answers a model it cannot build
  die      writes to stderr and exits after the handshake
  chatter  sends a notification before each answer, as a server may
"""
import json
import os
import sys

mode = os.environ.get("FAKE_MODE", "echo")
received = []


def send(message):
    sys.stdout.write(json.dumps(message) + "\n")
    sys.stdout.flush()


def answer(request_id, payload):
    send({"jsonrpc": "2.0", "id": request_id,
          "result": {"content": [{"type": "text", "text": json.dumps(payload)}], "isError": False}})


for line in sys.stdin:
    message = json.loads(line)
    received.append(message.get("method"))
    method = message.get("method")
    if method == "initialize":
        send({"jsonrpc": "2.0", "id": message["id"], "result": {
            "protocolVersion": message["params"]["protocolVersion"], "capabilities": {"tools": {}},
            "serverInfo": {"name": "fake", "version": "0"}}})
        continue
    if method == "notifications/initialized":
        if mode == "die":
            sys.stderr.write("boom: the fake server stopped\n")
            sys.stderr.flush()
            sys.exit(3)
        continue
    if method == "tools/call":
        name = message["params"]["name"]
        arguments = message["params"].get("arguments", {})
        if mode == "chatter":
            send({"jsonrpc": "2.0", "method": "notifications/message", "params": {"level": "info", "data": "hello"}})
        if mode == "refuse":
            answer(message["id"], {"error": "The model has no source.",
                                   "validation": {"valid": False, "errors": [{"code": "NO_SOURCE"}]}})
        elif name == "methods":
            answer(message["id"], {"methods": received})
        else:
            answer(message["id"], {"tool": name, "arguments": arguments})
