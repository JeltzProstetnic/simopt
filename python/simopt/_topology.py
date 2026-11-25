"""SIM-130 D5: a topology is a plain dict in the SIM-65 schema; the builder only saves typing.

Every method returns the topology itself, so calls chain, and the result serialises as it is. Nothing is
checked here: the server validates, reporting every fault at once (validate_model).
"""


def _given(**fields):
    return {key: value for key, value in fields.items() if value is not None}


def exponential(mean=None, *, rate=None):
    return _given(type="exponential", mean=mean, rate=rate)


def triangular(min, mode, max):
    return {"type": "triangular", "min": min, "mode": mode, "max": max}


def uniform(min, max):
    return {"type": "uniform", "min": min, "max": max}


def lognormal(mean=None, stddev=None, *, mu=None, sigma=None, shift=None):
    """Either mean and stddev, or mu and sigma of the underlying normal."""
    return _given(type="lognormal", mean=mean, stddev=stddev, mu=mu, sigma=sigma, shift=shift)


def gamma(k=None, theta=None, *, mean=None, shift=None):
    """Shape k and scale theta."""
    return _given(type="gamma", k=k, theta=theta, mean=mean, shift=shift)


def constant(value):
    return {"type": "constant", "value": value}


def empirical(min, max, probabilities):
    """Probabilities of equally spaced values from min to max, summing to 1."""
    return {"type": "empirical", "min": min, "max": max, "probabilities": list(probabilities)}


class Topology(dict):
    """A model in the SIM-65 topology schema, built by chained calls; it is the dict create_model takes."""

    def __init__(self, name, seed=None):
        super().__init__(_given(schema_version="1", name=name, seed=seed))
        self["nodes"] = []
        self["connections"] = []

    def node(self, id, type, **params):
        """Any node type, with its parameters as keywords; None leaves a parameter at its default."""
        node = {"id": id, "type": type}
        given = _given(**params)
        if given:
            node["params"] = given
        self["nodes"].append(node)
        return self

    def source(self, id, arrival=None):
        return self.node(id, "source", arrival=arrival)

    def queue(self, id, capacity=None, discipline=None):
        return self.node(id, "queue", capacity=capacity, discipline=discipline)

    def station(self, id, servers=None, service=None):
        return self.node(id, "station", servers=servers, service=service)

    def delay(self, id, duration):
        return self.node(id, "delay", duration=duration)

    def router(self, id, policy):
        return self.node(id, "router", policy=policy)

    def sink(self, id):
        return self.node(id, "sink")

    def resource(self, id, capacity=None):
        """SIM-126: units processes seize (bays, nurses); beside the item flow, connected to nothing."""
        return self.node(id, "resource", capacity=capacity)

    def stock(self, id, initial=None, capacity=None):
        """SIM-126: a level processes take from and put into; beside the item flow, connected to nothing."""
        return self.node(id, "stock", initial=initial, capacity=capacity)

    def process(self, id, steps):
        """SIM-126: runs every item it receives through its steps (see simopt.steps); fed by nothing, it repeats them."""
        self["nodes"].append({"id": id, "type": "process", "steps": list(steps)})
        return self

    def connect(self, *path, weight=None, outlet=None):
        """Connects each node of the path to the next; a weight, or the process outlet a connection leaves
        by, belongs to a single connection."""
        if len(path) < 2:
            raise ValueError("connect needs at least two node ids.")
        if weight is not None and len(path) != 2:
            raise ValueError("A weight belongs to one connection: connect(from, to, weight=...).")
        if outlet is not None and len(path) != 2:
            raise ValueError("An outlet belongs to one connection: connect(process, to, outlet=...).")
        for source, target in zip(path, path[1:]):
            self["connections"].append(_given(**{"from": source, "to": target, "weight": weight, "outlet": outlet}))
        return self

    def metric(self, id, kind, node=None):
        self.setdefault("metrics", []).append(_given(id=id, kind=kind, node=node))
        return self
