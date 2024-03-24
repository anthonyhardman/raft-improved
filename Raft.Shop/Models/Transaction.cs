namespace Raft.Shop;

public interface ITransactionStep
{
    Task<bool> Execute();
    Task ExecuteRollback();
}

public class TransactionStep<T> : ITransactionStep
{
    public Func<T, Task<bool>> Action { get; }
    public Func<T, Task> Rollback { get; }
    public T State { get; }

    public TransactionStep(T state, Func<T, Task<bool>> action, Func<T, Task> rollback)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Rollback = rollback;
        State = state;
    }

    public async Task<bool> Execute()
    {
        return await Action(State);
    }

    public async Task ExecuteRollback()
    {
        if (Rollback != null)
        {
            await Rollback(State);
        }
    }
}

public class Transaction
{
    private readonly List<ITransactionStep> _steps = new ();

    public Transaction AddStep<T>(T state, Func<T, Task<bool>> action, Func<T, Task> rollback = null)
    {
        TransactionStep<T> step = new(state, action, rollback);
        _steps.Add(step);
        return this;
    }

    public async Task<bool> Execute()
    {
        var completedSteps = new Stack<ITransactionStep>();

        foreach (var step in _steps)
        {
            var success = false;
            try
            {
                success = await step.Execute();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error executing transaction step {e.Message}");
                success = false;
            }
            if (!success)
            {
                while (completedSteps.Count > 0)
                {
                    var completedStep = completedSteps.Pop();
                    await completedStep.ExecuteRollback();
                }
                return false;
            }
            completedSteps.Push(step);
        }

        return true;
    }
}

