"""SIM-122: the same M/G/1-with-breakdowns case on SimPy, for the replication spread.

Arrivals exp(mean 60 min), service exp(mean 45), time to failure exp(mean 480 operating minutes),
repair exp(mean 30), preempt-resume: the operating-time budget to the next failure is spent by
running, a failure pauses the job for a repair and the job resumes. One stream per entity.
"""

import math
import random
import statistics
import sys

import simpy

DAY = 1440.0
GAP, SERVE, MTBF, MTTR = 60.0, 45.0, 480.0, 30.0


def replication(seed: int, days: float, warm_days: float) -> float:
    env = simpy.Environment()
    queue = simpy.Store(env)
    arrivals = random.Random(f"{seed}|src")
    machine = random.Random(f"{seed}|srv")
    times: list[float] = []
    warm = warm_days * DAY

    def source():
        while True:
            yield env.timeout(arrivals.expovariate(1 / GAP))
            queue.put(env.now)

    def server():
        budget = machine.expovariate(1 / MTBF)
        while True:
            t0 = yield queue.get()
            remaining = machine.expovariate(1 / SERVE)
            while remaining > budget:
                yield env.timeout(budget)
                remaining -= budget
                yield env.timeout(machine.expovariate(1 / MTTR))
                budget = machine.expovariate(1 / MTBF)
            yield env.timeout(remaining)
            budget -= remaining
            if t0 >= warm:
                times.append(env.now - t0)

    env.process(source())
    env.process(server())
    env.run(until=days * DAY)
    return statistics.mean(times)


def main() -> None:
    reps = int(sys.argv[1]) if len(sys.argv) > 1 else 20
    means = [replication(r, 360.0, 30.0) for r in range(reps)]
    f = 1 / MTBF
    m1 = SERVE * (1 + f * MTTR)
    m2 = (1 + f * MTTR) ** 2 * 2 * SERVE**2 + 2 * f * MTTR**2 * SERVE
    pk = m2 / GAP / (2 * (1 - m1 / GAP)) + m1
    avg, sd = statistics.mean(means), statistics.stdev(means)
    t = {20: 2.093, 50: 2.010, 100: 1.984}.get(reps, 1.96)
    hw = t * sd / math.sqrt(reps)
    print("rep,mean_time_in_system")
    for r, m in enumerate(means):
        print(f"{r},{m:.2f}")
    print(f"# replications={reps} mean={avg:.2f} sd={sd:.2f} ci95_halfwidth={hw:.2f} "
          f"rel_halfwidth={hw / avg:.1%} pollaczek_khinchine={pk:.2f} bias={avg / pk - 1:.1%}")


if __name__ == "__main__":
    main()
