using FluentAssertions;
using SimOpt.Mathematics.Stochastics.Distributions;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Entities;
using SimOpt.Simulation.Interfaces;
using SimOpt.Simulation.Templates;
using Xunit;

namespace SimOpt.Tests.Simulation.Templates;

/// <summary>
/// Tests for Server&lt;TMaterial, TProduct&gt; and Server&lt;TMaterial, TProduct, TData&gt;
/// via the SimpleServer concrete subclass and the typed generics.
///
/// Strategy: use a real Model + ConstantDoubleDistribution (same pattern as
/// SourceSinkIntegrationTests) — the Server constructor requires an initialised
/// IModel, and the state machine initialisation wires internal event infrastructure
/// that is not feasible to mock out cleanly.
/// </summary>
public class ServerTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Model CreateModel() => new Model("ServerTestModel", 42, 0.0);

    private static ConstantDoubleDistribution ConstDist(double value) =>
        new ConstantDoubleDistribution(value, initialize: false);

    // ---------------------------------------------------------------------------
    // 1. Initial state — property defaults after construction
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithDistribution_IsNotWorking()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Working.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_IsNotDamaged()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Damaged.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_IsNotRecovering()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Recovering.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_IsNotStopped()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Stopped.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_IsIdle()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Idle.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithDistribution_IsNotBusy()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.Busy.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_IsMachiningTimeStochastic()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.IsMachiningTimeStochastic.Should().BeTrue();
        server.IsMachiningTimeFunctional.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDistribution_CurrentMaterialIsEmpty()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.CurrentMaterial.Should().NotBeNull();
        server.CurrentMaterial.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithDistribution_EntityFinishedEventIsNotNull()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.EntityFinishedEvent.Should().NotBeNull();
    }

    // ---------------------------------------------------------------------------
    // 2. AutoStart flag behavior
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithoutAutoStartDelay_AutoStartIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoStart.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithAutoStartDelayZero_AutoStartIsTrue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0), autoStartDelay: 0.0);

        server.AutoStart.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithAutoStartDelayPositive_AutoStartIsTrue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0), autoStartDelay: 2.0);

        server.AutoStart.Should().BeTrue();
        server.AutoStartDelay.Should().Be(2.0);
    }

    [Fact]
    public void Constructor_WithNaNAutoStartDelay_AutoStartIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0), autoStartDelay: double.NaN);

        server.AutoStart.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // 3. Default behavior flags
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_AutoContinue_DefaultIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoContinue.Should().BeFalse();
    }

    [Fact]
    public void Constructor_AutoRecover_DefaultIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoRecover.Should().BeFalse();
    }

    [Fact]
    public void Constructor_AutoRestart_DefaultIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoRestart.Should().BeFalse();
    }

    [Fact]
    public void Constructor_AllowPushDuringMaintainance_DefaultIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AllowPushDuringMaintainance.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ContinueProductAfterFailure_DefaultIsFalse()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.ContinueProductAfterFailure.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // 4. Property assignment — behavior flags are settable before run
    // ---------------------------------------------------------------------------

    [Fact]
    public void AutoContinue_SetToTrue_ReflectsNewValue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoContinue = true;

        server.AutoContinue.Should().BeTrue();
    }

    [Fact]
    public void AutoRecover_SetToTrue_ReflectsNewValue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoRecover = true;

        server.AutoRecover.Should().BeTrue();
    }

    [Fact]
    public void AutoRestart_SetToTrue_ReflectsNewValue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.AutoRestart = true;

        server.AutoRestart.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // 5. MTTF / MTTR — no failure distribution means MaxValue
    // ---------------------------------------------------------------------------

    [Fact]
    public void MTTF_WithoutFailureDistribution_IsMaxValue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.MTTF.Should().Be(TimeSpan.MaxValue);
    }

    [Fact]
    public void MTTR_WithoutRecoverDistribution_IsMaxValue()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        server.MTTR.Should().Be(TimeSpan.MaxValue);
    }

    // ---------------------------------------------------------------------------
    // 6. Delegate (functional) constructor variant
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithDelegate_IsMachiningTimeFunctional()
    {
        var model = CreateModel();
        Func<List<SimpleEntity>, double> delegateFn = _ => 3.0;
        var server = new SimpleServer(model, delegateFn);

        server.IsMachiningTimeFunctional.Should().BeTrue();
        server.IsMachiningTimeStochastic.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDelegate_MachiningTimeFunctionIsSet()
    {
        var model = CreateModel();
        Func<List<SimpleEntity>, double> delegateFn = _ => 3.0;
        var server = new SimpleServer(model, delegateFn);

        server.MachiningTimeFunction.Should().NotBeNull();
        server.MachiningTimeFunction.Should().BeSameAs(delegateFn);
    }

    [Fact]
    public void Constructor_WithDelegate_InitialStateIsIdleNotWorking()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, _ => 4.0);

        server.Idle.Should().BeTrue();
        server.Working.Should().BeFalse();
    }

    // ---------------------------------------------------------------------------
    // 7. Named id/name constructor parameters
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithIdAndName_EntityNameContainsName()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(1.0), id: "srv-1", name: "Welder");

        server.EntityName.Should().Contain("Welder");
    }

    // ---------------------------------------------------------------------------
    // 8. Seed-based constructor variant
    // ---------------------------------------------------------------------------

    [Fact]
    public void Constructor_WithSeedId_IsMachiningTimeStochastic()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, seedID: 7, machiningTime: ConstDist(2.0));

        server.IsMachiningTimeStochastic.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithSeedIdAndDelegate_IsMachiningTimeFunctional()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, seedID: 7, machiningTimeDelegate: _ => 2.0);

        server.IsMachiningTimeFunctional.Should().BeTrue();
    }

    // ---------------------------------------------------------------------------
    // 9. Reset — state flags clear after model reset
    // ---------------------------------------------------------------------------

    [Fact]
    public void Reset_AfterModelReset_ServerRemainsIdle()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        model.Reset(42);

        server.Idle.Should().BeTrue();
        server.Working.Should().BeFalse();
        server.Damaged.Should().BeFalse();
        server.Recovering.Should().BeFalse();
    }

    [Fact]
    public void Reset_AfterModelReset_CurrentMaterialIsEmpty()
    {
        var model = CreateModel();
        var server = new SimpleServer(model, ConstDist(5.0));

        model.Reset(42);

        server.CurrentMaterial.Should().BeEmpty();
    }
}
