using System.Text;

namespace FareCore
{
    ///<summary>
    ///  <tt>Automaton</tt> transition. 
    ///  <p>
    ///    A transition, which belongs to a source state, consists of a Unicode character interval
    ///    and a destination state.
    ///  </p>
    ///</summary>
    public class Transition : IEquatable<Transition>
    {
        private readonly char max;
        private readonly char min;
        private readonly State to;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> class.
        /// (Constructs a new singleton interval transition).
        /// </summary>
        /// <param name="c">The transition character.</param>
        /// <param name="to">The destination state.</param>
        public Transition(char c, State to)
    {
        min = max = c;
        this.to = to;
    }

        /// <summary>
        /// Initializes a new instance of the <see cref="Transition"/> class.
        /// (Both end points are included in the interval).
        /// </summary>
        /// <param name="min">The transition interval minimum.</param>
        /// <param name="max">The transition interval maximum.</param>
        /// <param name="to">The destination state.</param>
        public Transition(char min, char max, State to)
    {
        if (max < min)
        {
            char t = max;
            max = min;
            min = t;
        }

        this.min = min;
        this.max = max;
        this.to = to;
    }

        /// <summary>
        /// Gets the minimum of this transition interval.
        /// </summary>
        public char Min
        {
            get { return min; }
        }

        /// <summary>
        /// Gets the maximum of this transition interval.
        /// </summary>
        public char Max
        {
            get { return max; }
        }

        /// <summary>
        /// Gets the destination of this transition.
        /// </summary>
        public State To
        {
            get { return to; }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Transition left, Transition right)
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
        public static bool operator !=(Transition left, Transition right)
    {
        return !Equals(left, right);
    }

        /// <inheritdoc />
        public override string ToString()
    {
        var sb = new StringBuilder();
        AppendCharString(min, sb);
        if (min != max)
        {
            sb.Append("-");
            AppendCharString(max, sb);
        }

        sb.Append(" -> ").Append(to.Number);
        return sb.ToString();
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

        if (obj.GetType() != typeof(Transition))
        {
            return false;
        }

        return Equals((Transition)obj);
    }

        /// <inheritdoc />
        public override int GetHashCode()
    {
        unchecked
        {
            int result = min.GetHashCode();
            result = (result*397) ^ max.GetHashCode();
            result = (result*397) ^ (to != null ? to.GetHashCode() : 0);
            return result;
        }
    }

        /// <inheritdoc />
        public bool Equals(Transition other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return other.min == min
               && other.max == max
               && Equals(other.to, to);
    }

        private static void AppendCharString(char c, StringBuilder sb)
    {
        if (c >= 0x21 && c <= 0x7e && c != '\\' && c != '"')
        {
            sb.Append(c);
        }
        else
        {
            sb.Append("\\u");
            string s = ((int)c).ToString("x");
            if (c < 0x10)
            {
                sb.Append("000").Append(s);
            }
            else if (c < 0x100)
            {
                sb.Append("00").Append(s);
            }
            else if (c < 0x1000)
            {
                sb.Append("0").Append(s);
            }
            else
            {
                sb.Append(s);
            }
        }
    }

        private void AppendDot(StringBuilder sb)
    {
        sb.Append(" -> ").Append(to.Number).Append(" [label=\"");
        AppendCharString(min, sb);
        if (min != max)
        {
            sb.Append("-");
            AppendCharString(max, sb);
        }

        sb.Append("\"]\n");
    }
    }
}