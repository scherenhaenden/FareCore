namespace RandomDataGeneratorCore.FareRegex;

/// <summary>
/// Pair of states.
/// </summary>
public class StatePair : IEquatable<StatePair>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatePair"/> class.
    /// </summary>
    /// <param name="s">The s.</param>
    /// <param name="s1">The s1.</param>
    /// <param name="s2">The s2.</param>
    public StatePair(State s, State s1, State s2)
    {
        S = s;
        FirstState = s1;
        SecondState = s2;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatePair"/> class.
    /// </summary>
    /// <param name="s1">The first state.</param>
    /// <param name="s2">The second state.</param>
    public StatePair(State s1, State s2)
        : this(null, s1, s2)
    {
    }

    public State S { get; set; }

    /// <summary>
    /// Gets or sets the first component of this pair.
    /// </summary>
    /// <value>
    /// The first state.
    /// </value>
    public State FirstState { get; set; }

    /// <summary>
    /// Gets or sets the second component of this pair.
    /// </summary>
    /// <value>
    /// The second state.
    /// </value>
    public State SecondState { get; set; }

    /// <summary>
    /// Implements the operator ==.
    /// </summary>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <returns>
    /// The result of the operator.
    /// </returns>
    public static bool operator ==(StatePair left, StatePair right)
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
    public static bool operator !=(StatePair left, StatePair right)
    {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public bool Equals(StatePair other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Equals(other.FirstState, FirstState)
               && Equals(other.SecondState, SecondState);
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

        if (obj.GetType() != typeof(StatePair))
        {
            return false;
        }

        return Equals((StatePair)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var result = 0;
            result = (result * 397) ^ (FirstState != null ? FirstState.GetHashCode() : 0);
            result = (result * 397) ^ (SecondState != null ? SecondState.GetHashCode() : 0);
            return result;
        }
    }
}