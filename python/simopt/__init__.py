"""SimOpt from Python: build a model as topology JSON, validate it, run replicated experiments and
searches through the SimOpt MCP server, and get the results as plain data ready for pandas (SIM-130)."""
from ._client import Server, ServerError, SimOptError

__all__ = ["Server", "ServerError", "SimOptError"]
__version__ = "0.1.0"
