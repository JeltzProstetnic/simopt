using FluentAssertions;
using Moq;
using SimOpt.Simulation.Engine;
using SimOpt.Simulation.Enum;
using Xunit;

namespace SimOpt.Tests.Simulation.Engine;

public class ModelTests
{
    [Fact]
    public void Constructor_DefaultModel_HasCorrectDefaults()
    {
        var model = new Model();

        model.Name.Should().StartWith("Model");
        model.CurrentState.Should().Be(ExecutionState.Stopped);
        model.IsReset.Should().BeTrue();
        model.IsRunning.Should().BeFalse();
        model.IsPaused.Should().BeFalse();
        model.IsStopped.Should().BeTrue();
        model.CurrentTime.Should().Be(0.0);
    }

    [Fact]
    public void Constructor_NamedModel_SetsNameAndSeed()
    {
        var model = new Model("TestModel", 123, 0.0);

        model.Name.Should().Be("TestModel");
        model.Seed.Should().Be(123);
    }

    [Fact]
    public void AddEntity_EntityIsRetrievable()
    {
        var model = new Model();
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.EntityName).Returns("TestEntity");
        entity.Setup(e => e.Identifier).Returns("TestEntity");
        entity.SetupProperty(e => e.Model);

        model.AddEntity(entity.Object);

        model.HasEntity("TestEntity").Should().BeTrue();
    }

    [Fact]
    public void GetEntity_ReturnsCorrectEntity()
    {
        var model = new Model();
        var entity = new Mock<IEntity>();
        entity.Setup(e => e.EntityName).Returns("MyEntity");
        entity.Setup(e => e.Identifier).Returns("MyEntity");
        entity.SetupProperty(e => e.Model);
        model.AddEntity(entity.Object);

        var retrieved = model.GetEntity<IEntity>("MyEntity");

        retrieved.Should().BeSameAs(entity.Object);
    }

    [Fact]
    public void HasEntity_NonExistent_ReturnsFalse()
    {
        var model = new Model();

        model.HasEntity("Ghost").Should().BeFalse();
    }

    [Fact]
    public void Schedule_ActionExecutedAtCorrectTime()
    {
        var model = new Model("Test", 42, 0.0);
        bool executed = false;

        model.Schedule(5.0, () => executed = true);
        model.Run(10.0);

        executed.Should().BeTrue();
    }

    [Fact]
    public void Run_WithStopTime_StopsAtCorrectTime()
    {
        var model = new Model("Test", 42, 0.0);
        // Schedule events that span beyond stop time so we hit TimeElapsed
        model.Schedule(5.0, () => model.Schedule(10.0, () => { }));

        model.Run(10.0);

        model.CurrentTime.Should().Be(10.0);
        model.CurrentState.Should().Be(ExecutionState.TimeElapsed);
    }

    [Fact]
    public void Run_NoEvents_ThrowsApplicationException()
    {
        var model = new Model("Test", 42, 0.0);

        var act = () => model.Run(10.0);

        act.Should().Throw<ApplicationException>()
            .WithMessage("*no events are scheduled*");
    }

    [Fact]
    public void Step_AdvancesByStepSize()
    {
        var model = new Model("Test", 42, 0.0);
        model.StepSize = 1.0;
        // Schedule events so there's something to process
        model.Schedule(0.5, () => { });
        model.Schedule(1.5, () => { });

        model.Step();

        model.CurrentTime.Should().Be(1.0);
    }

    [Fact]
    public void Stop_DuringRun_StopsSimulation()
    {
        var model = new Model("Test", 42, 0.0);
        int count = 0;

        // Schedule a chain of events; stop after 3
        void ScheduleNext()
        {
            count++;
            if (count >= 3) model.Stop();
            else model.Schedule(1.0, ScheduleNext);
        }

        model.Schedule(1.0, ScheduleNext);
        model.Run();

        count.Should().Be(3);
        model.CurrentState.Should().Be(ExecutionState.Stopped);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        var model = new Model("Test", 42, 0.0);
        model.Schedule(5.0, () => { });
        model.Run(10.0);

        model.Reset();

        model.CurrentTime.Should().Be(0.0);
        model.IsReset.Should().BeTrue();
        model.EventCounter.Should().Be(0);
    }

    [Fact]
    public void Reset_WithSameSeed_ProducesReproducibleResults()
    {
        var model = new Model("Test", 42, 0.0);
        var results1 = new List<double>();
        var results2 = new List<double>();

        void RecordTime() => results1.Add(model.CurrentTime);
        model.Schedule(1.0, RecordTime);
        model.Schedule(2.0, RecordTime);
        model.Run(5.0);

        model.Reset(42);

        void RecordTime2() => results2.Add(model.CurrentTime);
        model.Schedule(1.0, RecordTime2);
        model.Schedule(2.0, RecordTime2);
        model.Run(5.0);

        results1.Should().Equal(results2);
    }

    [Fact]
    public void EventCounter_TracksProcessedEvents()
    {
        var model = new Model("Test", 42, 0.0);
        model.Schedule(1.0, () => { });
        model.Schedule(2.0, () => { });
        model.Schedule(3.0, () => { });

        model.Run(10.0);

        model.EventCounter.Should().Be(3);
    }

    [Fact]
    public void SimulationFinished_EventFires()
    {
        var model = new Model("Test", 42, 0.0);
        bool finished = false;
        model.SimulationFinished += (sender, e) => finished = true;

        model.Schedule(1.0, () => { });
        model.Run(5.0);

        finished.Should().BeTrue();
    }
}
