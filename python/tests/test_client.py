"""SIM-130 step 1 (D2, D3, D6): the client speaks MCP over the server's stdio, raises refusals and a dead
server as exceptions, and finds the server from the environment or this repository's build output."""
import os
import sys
from pathlib import Path

import pytest

from simopt import Server, ServerError, SimOptError
from simopt._client import server_command

FAKE = str(Path(__file__).with_name("fake_server.py"))


def fake(mode, monkeypatch):
    monkeypatch.setenv("FAKE_MODE", mode)
    return Server(command=[sys.executable, FAKE], timeout=10)


def test_a_tool_call_returns_the_tools_answer_as_data(monkeypatch):
    with fake("echo", monkeypatch) as server:
        assert server.call("create_model", {"topology": {"name": "x"}}) == \
            {"tool": "create_model", "arguments": {"topology": {"name": "x"}}}


def test_the_handshake_comes_before_the_first_call(monkeypatch):
    with fake("echo", monkeypatch) as server:
        assert server.call("methods", {})["methods"] == ["initialize", "notifications/initialized", "tools/call"]


def test_a_notification_from_the_server_is_skipped(monkeypatch):
    with fake("chatter", monkeypatch) as server:
        assert server.call("get_schema", {})["tool"] == "get_schema"
        assert server.call("list_models", {})["tool"] == "list_models"


def test_a_refusal_raises_with_the_whole_report(monkeypatch):
    with fake("refuse", monkeypatch) as server:
        with pytest.raises(SimOptError) as refused:
            server.call("create_model", {"topology": {}})
    assert refused.value.message == "The model has no source."
    assert refused.value.validation["errors"][0]["code"] == "NO_SOURCE"
    assert "no source" in str(refused.value)


def test_a_server_that_dies_raises_with_its_last_words(monkeypatch):
    with fake("die", monkeypatch) as server:
        with pytest.raises(ServerError) as dead:
            server.call("get_schema", {})
    assert "boom" in str(dead.value)


def test_closing_ends_the_server(monkeypatch):
    with fake("echo", monkeypatch) as server:
        process = server._process
    assert process.poll() is not None


# ── D3: finding the server ──

def test_a_dll_named_in_the_environment_runs_with_dotnet(tmp_path):
    dll = tmp_path / "SimOpt.McpServer.dll"
    dll.write_text("")
    command = server_command({"SIMOPT_SERVER": str(dll), "DOTNET_ROOT": str(tmp_path)})
    assert command == [str(tmp_path / "dotnet"), str(dll)]


def test_an_executable_named_in_the_environment_runs_as_it_is(tmp_path):
    exe = tmp_path / "simopt-server"
    exe.write_text("")
    assert server_command({"SIMOPT_SERVER": str(exe)}) == [str(exe)]


def test_without_the_environment_the_repositorys_build_is_used(tmp_path):
    command = server_command({"DOTNET_ROOT": str(tmp_path)})
    assert command[0] == str(tmp_path / "dotnet")
    assert command[1].endswith(os.path.join("net9.0", "SimOpt.McpServer.dll"))
    assert Path(command[1]).is_file()


def test_a_missing_server_says_how_to_build_it(tmp_path):
    with pytest.raises(ServerError) as missing:
        server_command({"SIMOPT_SERVER": str(tmp_path / "nowhere.dll")})
    assert "dotnet build" in str(missing.value)
