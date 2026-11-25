"""SIM-126 schema: the steps of a process node, as the schema reads them. A process runs every item it receives
through its steps in order; one nothing feeds repeats them beside the flow. There are no loops or conditions.
Each function returns the step's dict; id is optional (without one the server names it <type>#<n>).

    from simopt import Topology, steps, uniform
    wash = [steps.seize("bays", patience=uniform(5, 20), outlet="gave_up"),
            steps.take("soap", 1),
            steps.delay(uniform(4, 6), id="washing")]
"""


def _step(type, id=None, **fields):
    step = {"id": id} if id is not None else {}
    step["type"] = type
    step.update({key: value for key, value in fields.items() if value is not None})
    return step


def delay(duration, id=None):
    """Holds the item for its own duration, a distribution drawn when it enters."""
    return _step("delay", id, duration=duration)


def seize(resource, patience=None, outlet=None, id=None):
    """Waits for a unit of the resource and holds it until a release or until the item leaves; with patience
    (a distribution) and an outlet, an item that waited that long leaves by the outlet, holding nothing."""
    return _step("seize", id, resource=resource, patience=patience, outlet=outlet)


def release(resource, id=None):
    """Frees one unit of a resource the item holds, before it leaves the process."""
    return _step("release", id, resource=resource)


def take(stock, amount, patience=None, outlet=None, id=None):
    """Takes the amount (a number or a distribution) out of the stock, waiting until the whole amount is there."""
    return _step("take", id, stock=stock, amount=amount, patience=patience, outlet=outlet)


def put(stock, amount=None, up_to=None, id=None):
    """Puts the amount into the stock, or fills it up to a level (up_to), waiting while it would overfill."""
    return _step("put", id, stock=stock, amount=amount, up_to=up_to)


def wait_until(stock, at_most=None, at_least=None, id=None):
    """Waits until the stock's level is at most or at least a figure; passes at once when it already is."""
    return _step("wait_until", id, stock=stock, at_most=at_most, at_least=at_least)
