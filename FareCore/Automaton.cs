﻿namespace FareCore
{
    public class Automaton
    {
        /// <summary>
        /// Minimize using Huffman's O(n<sup>2</sup>) algorithm.
        ///   This is the standard text-book algorithm.
        /// </summary>
        public const int MinimizeHuffman = 0;

        /// <summary>
        /// Minimize using Brzozowski's O(2<sup>n</sup>) algorithm. 
        ///   This algorithm uses the reverse-determinize-reverse-determinize trick, which has a bad
        ///   worst-case behavior but often works very well in practice even better than Hopcroft's!).
        /// </summary>
        public const int MinimizeBrzozowski = 1;

        /// <summary>
        /// Minimize using Hopcroft's O(n log n) algorithm.
        ///   This is regarded as one of the most generally efficient algorithms that exist.
        /// </summary>
        public const int MinimizeHopcroft = 2;

        /// <summary>
        /// Selects whether operations may modify the input automata (default: <code>false</code>).
        /// </summary>
        private static bool allowMutation;

        /// <summary>
        /// Minimize always flag.
        /// </summary>
        private static bool minimizeAlways;

        /// <summary>
        /// The hash code.
        /// </summary>
        private int hashCode;

        /// <summary>
        /// The initial.
        /// </summary>
        private State initial;

        /// <summary>
        /// Initializes a new instance of the <see cref="Automaton"/> class that accepts the empty 
        ///   language. Using this constructor, automata can be constructed manually from 
        ///   <see cref="State"/> and <see cref="Transition"/> objects.
        /// </summary>
        public Automaton()
        {
            Initial = new State();
            IsDeterministic = true;
            Singleton = null;
        }

        /// <summary>
        /// Gets the minimization algorithm (default: 
        /// <code>
        /// MINIMIZE_HOPCROFT
        /// </code>
        /// ).
        /// </summary>
        public static int Minimization
        {
            get { return MinimizeHopcroft; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether operations may modify the input automata (default:
        ///   <code>
        /// false
        /// </code>
        /// ).
        /// </summary>
        /// <value>
        /// <c>true</c> if [allow mutation]; otherwise, <c>false</c>.
        /// </value>
        public static bool AllowMutation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this automaton is definitely deterministic (i.e.,
        ///   there are no choices for any run, but a run may crash).
        /// </summary>
        /// <value>
        /// <c>true</c> then this automaton is definitely deterministic (i.e., there are no 
        ///   choices for any run, but a run may crash)., <c>false</c>.
        /// </value>
        public bool IsDeterministic { get; set; }

        /// <summary>
        /// Gets or sets the initial state of this automaton.
        /// </summary>
        /// <value>
        /// The initial state of this automaton.
        /// </value>
        public State Initial
        {
            get
            {
                ExpandSingleton();
                return initial;
            }

            set
            {
                Singleton = null;
                initial = value;
            }
        }

        /// <summary>
        /// Gets or sets the singleton string for this automaton. An automaton that accepts exactly one
        ///  string <i>may</i> be represented in singleton mode. In that case, this method may be 
        /// used to obtain the string.
        /// </summary>
        /// <value>The singleton string, null if this automaton is not in singleton mode.</value>
        public string Singleton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is singleton.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is singleton; otherwise, <c>false</c>.
        /// </value>
        public bool IsSingleton
        {
            get { return Singleton != null; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is debug.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is debug; otherwise, <c>false</c>.
        /// </value>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IsEmpty.
        /// </summary>
        public bool IsEmpty { get; set; }

        /// <summary>
        /// Gets the number of states in this automaton.
        /// </summary>
        /// Returns the number of states in this automaton.
        public int NumberOfStates
        {
            get
            {
                if (IsSingleton)
                {
                    return Singleton.Length + 1;
                }

                return GetStates().Count;
            }
        }

        /// <summary>
        /// Gets the number of transitions in this automaton. This number is counted
        ///   as the total number of edges, where one edge may be a character interval.
        /// </summary>
        public int NumberOfTransitions
        {
            get
            {
                if (IsSingleton)
                {
                    return Singleton.Length;
                }

                return GetStates().Sum(s => s.Transitions.Count);
            }
        }

        public static Transition[][] GetSortedTransitions(HashSet<State> states)
        {
            SetStateNumbers(states);
            var transitions = new Transition[states.Count][];
            foreach (State s in states)
            {
                transitions[s.Number] = s.GetSortedTransitions(false).ToArray();
            }

            return transitions;
        }

        public static Automaton MakeChar(char c)
        {
            return BasicAutomata.MakeChar(c);
        }

        public static Automaton MakeCharSet(string set)
        {
            return BasicAutomata.MakeCharSet(set);
        }

        public static Automaton MakeString(string s)
        {
            return BasicAutomata.MakeString(s);
        }

        public static Automaton Minimize(Automaton a)
        {
            a.Minimize();
            return a;
        }

        /// <summary>
        /// Sets or resets allow mutate flag. If this flag is set, then all automata operations
        /// may modify automata given as input; otherwise, operations will always leave input
        /// automata languages unmodified. By default, the flag is not set.
        /// </summary>
        /// <param name="flag">if set to <c>true</c> then all automata operations may modify 
        /// automata given as input; otherwise, operations will always leave input automata 
        /// languages unmodified..</param>
        /// <returns>The previous value of the flag.</returns>
        public static bool SetAllowMutate(bool flag)
        {
            bool b = allowMutation;
            allowMutation = flag;
            return b;
        }

        /// <summary>
        /// Sets or resets minimize always flag. If this flag is set, then {@link #minimize()} 
        /// will automatically be invoked after all operations that otherwise may produce 
        /// non-minimal automata. By default, the flag is not set.
        /// </summary>
        /// <param name="flag">The flag if true, the flag is set.</param>
        public static void SetMinimizeAlways(bool flag)
        {
            minimizeAlways = flag;
        }

        /// <summary>
        /// Assigns consecutive numbers to the given states.
        /// </summary>
        /// <param name="states">The states.</param>
        public static void SetStateNumbers(IEnumerable<State> states)
        {
            int number = 0;
            foreach (State s in states)
            {
                s.Number = number++;
            }
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            if (hashCode == 0)
            {
                Minimize();
            }

            return hashCode;
        }

        public void AddEpsilons(ICollection<StatePair> pairs)
        {
            BasicOperations.AddEpsilons(this, pairs);
        }

        /// <summary>
        /// The check minimize always.
        /// </summary>
        public void CheckMinimizeAlways()
        {
            if (minimizeAlways)
            {
                Minimize();
            }
        }

        /// <summary>
        /// The clear hash code.
        /// </summary>
        public void ClearHashCode()
        {
            hashCode = 0;
        }

        /// <summary>
        /// Creates a shallow copy of the current Automaton.
        /// </summary>
        /// <returns>
        /// A shallow copy of the current Automaton.
        /// </returns>
        public Automaton Clone()
        {
            var a = (Automaton)MemberwiseClone();
            if (!IsSingleton)
            {
                HashSet<State> states = GetStates();
                var d = states.ToDictionary(s => s, s => new State());

                foreach (State s in states)
                {
                    State p;
                    if (!d.TryGetValue(s, out p))
                    {
                        continue;
                    }

                    p.Accept = s.Accept;
                    if (s == Initial)
                    {
                        a.Initial = p;
                    }

                    foreach (Transition t in s.Transitions)
                    {
                        State to;
                        d.TryGetValue(t.To, out to);
                        p.Transitions.Add(new Transition(t.Min, t.Max, to));
                    }
                }
            }

            return a;
        }

        /// <summary>
        /// A clone of this automaton, expands if singleton.
        /// </summary>
        /// <returns>
        /// Returns a clone of this automaton, expands if singleton.
        /// </returns>
        public Automaton CloneExpanded()
        {
            Automaton a = Clone();
            a.ExpandSingleton();
            return a;
        }

        /// <summary>
        /// A clone of this automaton unless 
        /// <code>
        /// allowMutation
        /// </code>
        /// is set, expands if singleton.
        /// </summary>
        /// <returns>
        /// Returns a clone of this automaton unless 
        /// <code>
        /// allowMutation
        /// </code>
        /// is set, expands if singleton.
        /// </returns>
        public Automaton CloneExpandedIfRequired()
        {
            if (AllowMutation)
            {
                ExpandSingleton();
                return this;
            }

            return CloneExpanded();
        }

        /// <summary>
        /// Returns a clone of this automaton, or this automaton itself if <code>allow_mutation</code>
        /// flag is set.
        /// </summary>
        /// <returns>A clone of this automaton, or this automaton itself if <code>allow_mutation</code>
        /// flag is set.</returns>
        public Automaton CloneIfRequired()
        {
            if (allowMutation)
            {
                return this;
            }

            return Clone();
        }

        public Automaton Complement()
        {
            return BasicOperations.Complement(this);
        }

        public Automaton Concatenate(Automaton a)
        {
            return BasicOperations.Concatenate(this, a);
        }

        public void Determinize()
        {
            BasicOperations.Determinize(this);
        }

        /// <summary>
        /// Expands singleton representation to normal representation.
        /// Does nothing if not in singleton representation.
        /// </summary>
        public void ExpandSingleton()
        {
            if (IsSingleton)
            {
                var p = new State();
                initial = p;
                foreach (char t in Singleton)
                {
                    var q = new State();
                    p.Transitions.Add(new Transition(t, q));
                    p = q;
                }

                p.Accept = true;
                IsDeterministic = true;
                Singleton = null;
            }
        }

        /// <summary>
        /// The set of reachable accept states.
        /// </summary>
        /// <returns>Returns the set of reachable accept states.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is not executing immediately nor returns the same value each time it is invoked.")]
        public HashSet<State> GetAcceptStates()
        {
            ExpandSingleton();

            var accepts = new HashSet<State>();
            var visited = new HashSet<State>();

            var worklist = new LinkedList<State>();
            worklist.AddLast(Initial);

            visited.Add(Initial);

            while (worklist.Count > 0)
            {
                State s = worklist.RemoveAndReturnFirst();
                if (s.Accept)
                {
                    accepts.Add(s);
                }

                foreach (Transition t in s.Transitions)
                {
                    // TODO: Java code does not check for null states.
                    if (t.To == null)
                    {
                        continue;
                    }

                    if (!visited.Contains(t.To))
                    {
                        visited.Add(t.To);
                        worklist.AddLast(t.To);
                    }
                }
            }

            return accepts;
        }

        /// <summary>
        /// Returns the set of live states. A state is "live" if an accept state is reachable from it.
        /// </summary>
        /// <returns></returns>
        public HashSet<State> GetLiveStates()
        {
            ExpandSingleton();
            return GetLiveStates(GetStates());
        }

        /// <summary>
        /// The sorted array of all interval start points.
        /// </summary>
        /// <returns>Returns sorted array of all interval start points.</returns>
        public char[] GetStartPoints()
        {
            var pointSet = new HashSet<char>();
            foreach (State s in GetStates())
            {
                pointSet.Add(char.MinValue);
                foreach (Transition t in s.Transitions)
                {
                    pointSet.Add(t.Min);
                    if (t.Max < char.MaxValue)
                    {
                        pointSet.Add((char)(t.Max + 1));
                    }
                }
            }

            var points = new char[pointSet.Count];
            int n = 0;
            foreach (char m in pointSet)
            {
                points[n++] = m;
            }

            Array.Sort(points);
            return points;
        }

        /// <summary>
        /// Gets the set of states that are reachable from the initial state.
        /// </summary>
        /// <returns>
        /// The set of states that are reachable from the initial state.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "This is not executing immediately nor returns the same value each time it is invoked.")]
        public HashSet<State> GetStates()
        {
            ExpandSingleton();
            HashSet<State> visited;
            if (IsDebug)
            {
                visited = new HashSet<State>(); // LinkedHashSet
            }
            else
            {
                visited = new HashSet<State>();
            }

            var worklist = new LinkedList<State>();
            worklist.AddLast(Initial);
            visited.Add(Initial);
            while (worklist.Count > 0)
            {
                State s = worklist.RemoveAndReturnFirst();
                if (s == null)
                {
                    continue;
                }

                HashSet<Transition> tr = IsDebug
                    ? new HashSet<Transition>(s.GetSortedTransitions(false))
                    : new HashSet<Transition>(s.Transitions);

                foreach (Transition t in tr)
                {
                    if (!visited.Contains(t.To))
                    {
                        visited.Add(t.To);
                        worklist.AddLast(t.To);
                    }
                }
            }

            return visited;
        }

        public Automaton Intersection(Automaton a)
        {
            return BasicOperations.Intersection(this, a);
        }

        public bool IsEmptyString()
        {
            return BasicOperations.IsEmptyString(this);
        }

        /// <summary>
        /// The minimize.
        /// </summary>
        public void Minimize()
        {
            MinimizationOperations.Minimize(this);
        }

        public Automaton Optional()
        {
            return BasicOperations.Optional(this);
        }

        /// <summary>
        /// Recomputes the hash code.
        ///   The automaton must be minimal when this operation is performed.
        /// </summary>
        public void RecomputeHashCode()
        {
            hashCode = (NumberOfStates * 3) + (NumberOfTransitions * 2);
            if (hashCode == 0)
            {
                hashCode = 1;
            }
        }

        /// <summary>
        /// Reduces this automaton.
        /// An automaton is "reduced" by combining overlapping and adjacent edge intervals with same 
        /// destination.
        /// </summary>
        public void Reduce()
        {
            if (IsSingleton)
            {
                return;
            }

            HashSet<State> states = GetStates();
            SetStateNumbers(states);
            foreach (State s in states)
            {
                IList<Transition> st = s.GetSortedTransitions(true);
                s.ResetTransitions();
                State p = null;
                int min = -1, max = -1;
                foreach (Transition t in st)
                {
                    if (p == t.To)
                    {
                        if (t.Min <= max + 1)
                        {
                            if (t.Max > max)
                            {
                                max = t.Max;
                            }
                        }
                        else
                        {
                            if (p != null)
                            {
                                s.Transitions.Add(new Transition((char)min, (char)max, p));
                            }

                            min = t.Min;
                            max = t.Max;
                        }
                    }
                    else
                    {
                        if (p != null)
                        {
                            s.Transitions.Add(new Transition((char)min, (char)max, p));
                        }

                        p = t.To;
                        min = t.Min;
                        max = t.Max;
                    }
                }

                if (p != null)
                {
                    s.Transitions.Add(new Transition((char)min, (char)max, p));
                }
            }

            ClearHashCode();
        }

        /// <summary>
        /// Removes transitions to dead states and calls Reduce() and ClearHashCode().
        /// (A state is "dead" if no accept state is reachable from it).
        /// </summary>
        public void RemoveDeadTransitions()
        {
            ClearHashCode();
            if (IsSingleton)
            {
                return;
            }

            // TODO: Java code does not check for null states.
            var states = new HashSet<State>(GetStates().Where(state => state != null));
            var live = GetLiveStates(states);
            foreach (State s in states)
            {
                var st = s.Transitions;
                s.ResetTransitions();
                foreach (Transition t in st)
                {
                    // TODO: Java code does not check for null states.
                    if (t.To == null)
                    {
                        continue;
                    }

                    if (live.Contains(t.To))
                    {
                        s.Transitions.Add(t);
                    }
                }
            }

            Reduce();
        }

        public Automaton Repeat(int min, int max)
        {
            return BasicOperations.Repeat(this, min, max);
        }

        public Automaton Repeat()
        {
            return BasicOperations.Repeat(this);
        }

        public Automaton Repeat(int min)
        {
            return BasicOperations.Repeat(this, min);
        }

        public bool Run(string s)
        {
            return BasicOperations.Run(this, s);
        }

        /// <summary>
        /// Adds transitions to explicit crash state to ensure that transition function is total.
        /// </summary>
        public void Totalize()
        {
            var s = new State();
            s.Transitions.Add(new Transition(char.MinValue, char.MaxValue, s));

            foreach (State p in GetStates())
            {
                int maxi = char.MinValue;
                foreach (Transition t in p.GetSortedTransitions(false))
                {
                    if (t.Min > maxi)
                    {
                        p.Transitions.Add(new Transition((char)maxi, (char)(t.Min - 1), s));
                    }

                    if (t.Max + 1 > maxi)
                    {
                        maxi = t.Max + 1;
                    }
                }

                if (maxi <= char.MaxValue)
                {
                    p.Transitions.Add(new Transition((char)maxi, char.MaxValue, s));
                }
            }
        }

        private HashSet<State> GetLiveStates(HashSet<State> states)
        {
            var dictionary = states.ToDictionary(s => s, s => new HashSet<State>());

            foreach (State s in states)
            {
                foreach (Transition t in s.Transitions)
                {
                    // TODO: Java code does not check for null states.
                    if (t.To == null)
                    {
                        continue;
                    }

                    dictionary[t.To].Add(s);
                }
            }

            var comparer = new StateEqualityComparer();

            var live = new HashSet<State>(GetAcceptStates(), comparer);
            var worklist = new LinkedList<State>(live);
            while (worklist.Count > 0)
            {
                State s = worklist.RemoveAndReturnFirst();
                foreach (State p in dictionary[s])
                {
                    if (!live.Contains(p))
                    {
                        live.Add(p);
                        worklist.AddLast(p);
                    }
                }
            }

            return live;
        }
    }
}