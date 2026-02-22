using FluentAssertions;
using SimOpt.Basics.Datastructures.StateMachine;
using Xunit;

namespace SimOpt.Tests.Basics;

public class StateMachineTests
{
    private readonly StateMachine<State> _sm;

    public StateMachineTests()
    {
        _sm = new StateMachine<State>();
    }

    [Fact]
    public void AddState_ById_Succeeds()
    {
        _sm.AddState(0, "Idle").Should().BeTrue();
        _sm.State[0].Name.Should().Be("Idle");
    }

    [Fact]
    public void AddState_ByName_AutoAssignsId()
    {
        int id = _sm.AddState("Running");
        _sm.State[id].Name.Should().Be("Running");
    }

    [Fact]
    public void AddState_DuplicateName_Throws()
    {
        _sm.AddState("Idle");
        var act = () => _sm.AddState("Idle");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddState_DuplicateId_ReturnsFalse()
    {
        _sm.AddState(0, "Idle").Should().BeTrue();
        _sm.AddState(0, "Other").Should().BeFalse();
    }

    [Fact]
    public void SwitchState_WithoutTransition_ReturnsFalse()
    {
        var a = new State("A", 0);
        var b = new State("B", 1);
        _sm.AddState(a);
        _sm.AddState(b);

        // Allow A->B only (not B->A)
        _sm.AddTransition(a, b);

        // Force initial state via reflection (no public setter)
        var field = typeof(StateMachine<State>).GetField("currentState",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(_sm, b);

        // B->A is not allowed
        _sm.SwitchState(a).Should().BeFalse();
    }

    [Fact]
    public void AddTransition_AllowsSwitch()
    {
        var idle = new State("Idle", 0);
        var running = new State("Running", 1);

        _sm.AddState(idle);
        _sm.AddState(running);
        _sm.AddTransition(idle, running).Should().BeTrue();

        idle.TransitionAllowed(running).Should().BeTrue();
    }

    [Fact]
    public void AddTransition_DuplicateReturnsFalse()
    {
        var idle = new State("Idle", 0);
        var running = new State("Running", 1);
        _sm.AddState(idle);
        _sm.AddState(running);

        _sm.AddTransition(idle, running).Should().BeTrue();
        _sm.AddTransition(idle, running).Should().BeFalse();
    }

    [Fact]
    public void RemoveTransition_ForbidsSwitch()
    {
        var a = new State("A", 0);
        var b = new State("B", 1);
        _sm.AddState(a);
        _sm.AddState(b);

        _sm.AddTransition(a, b);
        a.TransitionAllowed(b).Should().BeTrue();

        _sm.RemoveTransition(a, b).Should().BeTrue();
        a.TransitionAllowed(b).Should().BeFalse();
    }

    [Fact]
    public void AllowAllTransitions_AllowsEveryPair()
    {
        var a = new State("A", 0);
        var b = new State("B", 1);
        var c = new State("C", 2);
        _sm.AddState(a);
        _sm.AddState(b);
        _sm.AddState(c);

        _sm.AllowAllTransitions();

        a.TransitionAllowed(b).Should().BeTrue();
        a.TransitionAllowed(c).Should().BeTrue();
        b.TransitionAllowed(a).Should().BeTrue();
        b.TransitionAllowed(c).Should().BeTrue();
        c.TransitionAllowed(a).Should().BeTrue();
        c.TransitionAllowed(b).Should().BeTrue();
        // Self-transitions should NOT be allowed
        a.TransitionAllowed(a).Should().BeFalse();
    }

    [Fact]
    public void ForbidAllTransitions_RemovesAll()
    {
        var a = new State("A", 0);
        var b = new State("B", 1);
        _sm.AddState(a);
        _sm.AddState(b);

        _sm.AllowAllTransitions();
        _sm.ForbidAllTransitions();

        a.TransitionAllowed(b).Should().BeFalse();
        b.TransitionAllowed(a).Should().BeFalse();
    }

    [Fact]
    public void StateLookup_ByNameAndId()
    {
        _sm.AddState(5, "Custom");

        _sm.State[5].Name.Should().Be("Custom");
        _sm.State["Custom"].ID.Should().Be(5);
    }
}
