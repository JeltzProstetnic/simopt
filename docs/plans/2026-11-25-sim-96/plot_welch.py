"""SIM-96: Welch's plot for the M/M/1 at rho = 0.8 from the CSV that AnalyticNightlyTests writes.

Make the CSV:  SIMOPT_NIGHTLY=1 SIMOPT_WELCH_CSV=docs/plans/2026-11-25-sim-96/welch-mm1-rho08.csv \
               dotnet test tests/SimOpt.Tests/SimOpt.Tests.csproj --filter "FullyQualifiedName~WelchsMovingAverage"
Then:          python3 docs/plans/2026-11-25-sim-96/plot_welch.py
"""
import csv
import pathlib

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt  # noqa: E402

HERE = pathlib.Path(__file__).parent
W = 5.0                 # steady-state sojourn 1 / (mu - lambda)
WARMUP_CUSTOMERS = 800  # 1,000 time units at lambda = 0.8
TAU_CUSTOMERS = 72      # relaxation time 89.7 time units, in customers

rows = list(csv.DictReader(open(HERE / "welch-mm1-rho08.csv")))
customer = [int(r["customer"]) for r in rows]
mean = [float(r["mean_sojourn"]) for r in rows]
moving = [float(r["welch_moving_average"]) for r in rows]

fig, ax = plt.subplots(figsize=(9, 4.8), dpi=150)
ax.plot(customer, mean, color="#b8c4d6", linewidth=0.6, label="mean over 30 replications, per customer")
ax.plot(customer, moving, color="#1f4e8c", linewidth=1.8, label="Welch moving average (window 201)")
ax.axhline(W, color="#2e7d32", linestyle="--", linewidth=1.2, label="steady state W = 1/(μ − λ) = 5")
ax.axvline(WARMUP_CUSTOMERS, color="#c62828", linewidth=1.2, label="warm-up end, 1,000 time units ≈ 800 customers")
ax.axvspan(0, 4 * TAU_CUSTOMERS, color="#fbe9e7", label="4 relaxation times ≈ 290 customers")
ax.set_xlim(0, customer[-1])
ax.set_ylim(0, max(max(mean), 9))
ax.set_xlabel("customer (completion order)")
ax.set_ylabel("sojourn time")
ax.set_title("M/M/1, λ = 0.8, μ = 1: the start-up transient ends well before the warm-up does")
ax.legend(loc="lower right", fontsize=8, frameon=False)
ax.spines[["top", "right"]].set_visible(False)
fig.tight_layout()
fig.savefig(HERE / "welch-mm1-rho08.png")
print("wrote", HERE / "welch-mm1-rho08.png")
