using System.Text;

namespace RandomDataGeneratorCore.FareRegex;




/// <summary>
/// <tt>Automaton</tt> state.
/// </summary>
public class State : IEquatable<State>, IComparable<State>, IComparable
{
    private readonly int id;
    private static int nextId;

    /// <summary>
    /// Initializes a new instance of the <see cref="State"/> class. Initially, the new state is a 
    ///   reject state.
    /// </summary>
    public State()
    {
        ResetTransitions();
        id = Interlocked.Increment(ref nextId);
    }

    /// <summary>
    /// Gets the id.
    /// </summary>
    public int Id => id;

    /// <summary>
    /// Gets or sets a value indicating whether this State is Accept.
    /// </summary>
    public bool Accept { get; set; }

    /// <summary>
    /// Gets or sets this State Number.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Gets or sets this State Transitions.
    /// </summary>
    public IList<Transition> Transitions { get; set; }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(State left, State right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Implements the operator !=.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator !=(State left, State right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != typeof(State))
        {
            return false;
        }

        return Equals((State)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int result = id;
            result = (result * 397) ^ Accept.GetHashCode();
            result = (result * 397) ^ Number;
            return result;
        }
    }


    /// <inheritdoc />
    public int CompareTo(object other)
    {
        if (other == null)
        {
            return 1;
        }

        if (other.GetType() != typeof(State))
        {
            throw new ArgumentException("Object is not a State");
        }

        return CompareTo((State)other);
    }

    /// <inheritdoc />
    public bool Equals(State other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.id == id 
               && other.Accept.Equals(Accept)
               && other.Number == Number;
    }

    /// <inheritdoc />
    public int CompareTo(State other)
    {
        return other.Id - Id;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("state ").Append(Number);
        sb.Append(Accept ? " [accept]" : " [reject]");
        sb.Append(":\n");
        foreach (Transition t in Transitions)
        {
            sb.Append("  ").Append(t.ToString()).Append("\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Adds an outgoing transition.
    /// </summary>
    /// <param name="t">
    /// The transition.
    /// </param>
    public void AddTransition(Transition t)
    {
        Transitions.Add(t);
    }

    /// <summary>
    /// Performs lookup in transitions, assuming determinism.
    /// </summary>
    /// <param name="c">
    /// The character to look up.
    /// </param>
    /// <returns>
    /// The destination state, null if no matching outgoing transition.
    /// </returns>
    public State Step(char c)
    {
        return (from t in Transitions where t.Min <= c && c <= t.Max select t.To).FirstOrDefault();
    }

    /// <summary>
    /// Performs lookup in transitions, allowing nondeterminism.
    /// </summary>
    /// <param name="c">
    /// The character to look up.
    /// </param>
    /// <param name="dest">
    /// The collection where destination states are stored.
    /// </param>
    public void Step(char c, List<State> dest)
    {
        dest.AddRange(from t in Transitions where t.Min <= c && c <= t.Max select t.To);
    }

    /// <summary>
    /// Gets the transitions sorted by (min, reverse max, to) or (to, min, reverse max).
    /// </summary>
    /// <param name="toFirst">
    /// if set to <c>true</c> [to first].
    /// </param>
    /// <returns>
    /// The transitions sorted by (min, reverse max, to) or (to, min, reverse max).
    /// </returns>
    public IList<Transition> GetSortedTransitions(bool toFirst)
    {
        Transition[] e = Transitions.ToArray();
        Array.Sort(e, new TransitionComparer(toFirst));
        return e.ToList();
    }

    internal void AddEpsilon(State to)
    {
        if (to.Accept)
        {
            Accept = true;
        }

        foreach (Transition t in to.Transitions)
        {
            Transitions.Add(t);
        }
    }

    internal void ResetTransitions()
    {
        Transitions = new List<Transition>();
    }
}