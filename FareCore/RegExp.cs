using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace RandomDataGeneratorCore.FareRegex;

public static class BasicOperations
    {
        /// <summary>
        /// Adds epsilon transitions to the given automaton. This method adds extra character interval
        /// transitions that are equivalent to the given set of epsilon transitions.
        /// </summary>
        /// <param name="automatonInput">The automaton.</param>
        /// <param name="pairs">A collection of <see cref="StatePair"/> objects representing pairs of
        /// source/destination states where epsilon transitions should be added.</param>
        public static void AddEpsilons(Automaton automatonInput, ICollection<StatePair> pairs)
        {
            automatonInput.ExpandSingleton();
            var forward = new Dictionary<State, HashSet<State>>();
            var back = new Dictionary<State, HashSet<State>>();
            foreach (StatePair p in pairs)
            {
                HashSet<State> to = forward[p.FirstState];
                if (to == null)
                {
                    to = new HashSet<State>();
                    forward.Add(p.FirstState, to);
                }

                to.Add(p.SecondState);
                HashSet<State> from = back[p.SecondState];
                if (from == null)
                {
                    from = new HashSet<State>();
                    back.Add(p.SecondState, from);
                }

                from.Add(p.FirstState);
            }

            var worklist = new LinkedList<StatePair>(pairs);
            var workset = new HashSet<StatePair>(pairs);
            while (worklist.Count != 0)
            {
                StatePair p = worklist.RemoveAndReturnFirst();
                workset.Remove(p);
                HashSet<State> to = forward[p.SecondState];
                HashSet<State> from = back[p.FirstState];
                if (to != null)
                {
                    foreach (State s in to)
                    {
                        var pp = new StatePair(p.FirstState, s);
                        if (!pairs.Contains(pp))
                        {
                            pairs.Add(pp);
                            forward[p.FirstState].Add(s);
                            back[s].Add(p.FirstState);
                            worklist.AddLast(pp);
                            workset.Add(pp);
                            if (from != null)
                            {
                                foreach (State q in from)
                                {
                                    var qq = new StatePair(q, p.FirstState);
                                    if (!workset.Contains(qq))
                                    {
                                        worklist.AddLast(qq);
                                        workset.Add(qq);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Add transitions.
            foreach (StatePair p in pairs)
            {
                p.FirstState.AddEpsilon(p.SecondState);
            }

            automatonInput.IsDeterministic = false;
            automatonInput.ClearHashCode();
            automatonInput.CheckMinimizeAlways();
        }

        /// <summary>
        /// Returns an automaton that accepts the union of the languages of the given automata.
        /// </summary>
        /// <param name="automatons">The l.</param>
        /// <returns>
        /// An automaton that accepts the union of the languages of the given automata.
        /// </returns>
        /// <remarks>
        /// Complexity: linear in number of states.
        /// </remarks>
        public static Automaton Union(IList<Automaton> automatons)
        {
            var ids = new HashSet<int>();
            foreach (Automaton a in automatons)
            {
                ids.Add(RuntimeHelpers.GetHashCode(a));
            }

            bool hasAliases = ids.Count != automatons.Count;
            var s = new State();
            foreach (Automaton b in automatons)
            {
                if (b.IsEmpty)
                {
                    continue;
                }

                Automaton bb = b;
                bb = hasAliases ? bb.CloneExpanded() : bb.CloneExpandedIfRequired();

                s.AddEpsilon(bb.Initial);
            }

            var automaton = new Automaton();
            automaton.Initial = s;
            automaton.IsDeterministic = false;
            automaton.ClearHashCode();
            automaton.CheckMinimizeAlways();
            return automaton;
        }

        /// <summary>
        /// Returns a (deterministic) automaton that accepts the complement of the language of the 
        /// given automaton.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <returns>A (deterministic) automaton that accepts the complement of the language of the 
        /// given automaton.</returns>
        /// <remarks>
        /// Complexity: linear in number of states (if already deterministic).
        /// </remarks>
        public static Automaton Complement(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            a.Determinize();
            a.Totalize();
            foreach (State p in a.GetStates())
            {
                p.Accept = !p.Accept;
            }

            a.RemoveDeadTransitions();
            return a;
        }

        public static Automaton Concatenate(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton && a2.IsSingleton)
            {
                return BasicAutomata.MakeString(a1.Singleton + a2.Singleton);
            }

            if (IsEmpty(a1) || IsEmpty(a2))
            {
                return BasicAutomata.MakeEmpty();
            }

            bool deterministic = a1.IsSingleton && a2.IsDeterministic;
            if (a1 == a2)
            {
                a1 = a1.CloneExpanded();
                a2 = a2.CloneExpanded();
            }
            else
            {
                a1 = a1.CloneExpandedIfRequired();
                a2 = a2.CloneExpandedIfRequired();
            }

            foreach (State s in a1.GetAcceptStates())
            {
                s.Accept = false;
                s.AddEpsilon(a2.Initial);
            }

            a1.IsDeterministic = deterministic;
            a1.ClearHashCode();
            a1.CheckMinimizeAlways();
            return a1;
        }

        public static Automaton Concatenate(IList<Automaton> l)
        {
            if (l.Count == 0)
            {
                return BasicAutomata.MakeEmptyString();
            }

            bool allSingleton = l.All(a => a.IsSingleton);

            if (allSingleton)
            {
                var b = new StringBuilder();
                foreach (Automaton a in l)
                {
                    b.Append(a.Singleton);
                }

                return BasicAutomata.MakeString(b.ToString());
            }
            else
            {
                if (l.Any(a => a.IsEmpty))
                {
                    return BasicAutomata.MakeEmpty();
                }

                var ids = new HashSet<int>();
                foreach (Automaton a in l)
                {
                    ids.Add(RuntimeHelpers.GetHashCode(a));
                }

                bool hasAliases = ids.Count != l.Count;
                Automaton b = l[0];
                b = hasAliases ? b.CloneExpanded() : b.CloneExpandedIfRequired();

                var ac = b.GetAcceptStates();
                bool first = true;
                foreach (Automaton a in l)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        if (a.IsEmptyString())
                        {
                            continue;
                        }

                        Automaton aa = a;
                        aa = hasAliases ? aa.CloneExpanded() : aa.CloneExpandedIfRequired();

                        HashSet<State> ns = aa.GetAcceptStates();
                        foreach (State s in ac)
                        {
                            s.Accept = false;
                            s.AddEpsilon(aa.Initial);
                            if (s.Accept)
                            {
                                ns.Add(s);
                            }
                        }

                        ac = ns;
                    }
                }

                b.IsDeterministic = false;
                b.ClearHashCode();
                b.CheckMinimizeAlways();
                return b;
            }
        }

        /// <summary>
        /// Determinizes the specified automaton.
        /// </summary>
        /// <remarks>
        /// Complexity: exponential in number of states.
        /// </remarks>
        /// <param name="a">The automaton.</param>
        public static void Determinize(Automaton a)
        {
            if (a.IsDeterministic || a.IsSingleton)
            {
                return;
            }

            var initialset = new HashSet<State>();
            initialset.Add(a.Initial);
            Determinize(a, initialset.ToList());
        }

        /// <summary>
        /// Determinizes the given automaton using the given set of initial states.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <param name="initialset">The initial states.</param>
        public static void Determinize(Automaton a, List<State> initialset)
        {
            char[] points = a.GetStartPoints();

            var comparer = new ListEqualityComparer<State>();

            // Subset construction.
            var sets = new Dictionary<List<State>, List<State>>(comparer);
            var worklist = new LinkedList<List<State>>();
            var newstate = new Dictionary<List<State>, State>(comparer);

            sets.Add(initialset, initialset);
            worklist.AddLast(initialset);
            a.Initial = new State();
            newstate.Add(initialset, a.Initial);

            while (worklist.Count > 0)
            {
                List<State> s = worklist.RemoveAndReturnFirst();
                State r;
                newstate.TryGetValue(s, out r);
                foreach (State q in s)
                {
                    if (q.Accept)
                    {
                        r.Accept = true;
                        break;
                    }
                }

                for (int n = 0; n < points.Length; n++)
                {
                    var set = new HashSet<State>();
                    foreach (State c in s)
                        foreach (Transition t in c.Transitions)
                            if (t.Min <= points[n] && points[n] <= t.Max)
                                set.Add(t.To);

                    var p = set.ToList();

                    if (!sets.ContainsKey(p))
                    {
                        sets.Add(p, p);
                        worklist.AddLast(p);
                        newstate.Add(p, new State());
                    }

                    State q;
                    newstate.TryGetValue(p, out q);
                    char min = points[n];
                    char max;
                    if (n + 1 < points.Length)
                    {
                        max = (char)(points[n + 1] - 1);
                    }
                    else
                    {
                        max = char.MaxValue;
                    }

                    r.Transitions.Add(new Transition(min, max, q));
                }
            }

            a.IsDeterministic = true;
            a.RemoveDeadTransitions();
        }

        /// <summary>
        /// Determines whether the given automaton accepts no strings.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <returns>
        ///   <c>true</c> if the given automaton accepts no strings; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsEmpty(Automaton a)
        {
            if (a.IsSingleton)
            {
                return false;
            }

            return !a.Initial.Accept && a.Initial.Transitions.Count == 0;
        }

        /// <summary>
        /// Determines whether the given automaton accepts the empty string and nothing else.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <returns>
        ///   <c>true</c> if the given automaton accepts the empty string and nothing else; otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool IsEmptyString(Automaton a)
        {
            if (a.IsSingleton)
            {
                return a.Singleton.Length == 0;
            }

            return a.Initial.Accept && a.Initial.Transitions.Count == 0;
        }

        /// <summary>
        /// Returns an automaton that accepts the intersection of the languages of the given automata.
        /// Never modifies the input automata languages.
        /// </summary>
        /// <param name="a1">The a1.</param>
        /// <param name="a2">The a2.</param>
        /// <returns></returns>
        public static Automaton Intersection(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton)
            {
                if (a2.Run(a1.Singleton))
                {
                    return a1.CloneIfRequired();
                }

                return BasicAutomata.MakeEmpty();
            }

            if (a2.IsSingleton)
            {
                if (a1.Run(a2.Singleton))
                {
                    return a2.CloneIfRequired();
                }

                return BasicAutomata.MakeEmpty();
            }

            if (a1 == a2)
            {
                return a1.CloneIfRequired();
            }

            Transition[][] transitions1 = Automaton.GetSortedTransitions(a1.GetStates());
            Transition[][] transitions2 = Automaton.GetSortedTransitions(a2.GetStates());
            var c = new Automaton();
            var worklist = new LinkedList<StatePair>();
            var newstates = new Dictionary<StatePair, StatePair>();
            var p = new StatePair(c.Initial, a1.Initial, a2.Initial);
            worklist.AddLast(p);
            newstates.Add(p, p);
            while (worklist.Count > 0)
            {
                p = worklist.RemoveAndReturnFirst();
                p.S.Accept = p.FirstState.Accept && p.SecondState.Accept;
                Transition[] t1 = transitions1[p.FirstState.Number];
                Transition[] t2 = transitions2[p.SecondState.Number];
                for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
                {
                    while (b2 < t2.Length && t2[b2].Max < t1[n1].Min)
                    {
                        b2++;
                    }

                    for (int n2 = b2; n2 < t2.Length && t1[n1].Max >= t2[n2].Min; n2++)
                    {
                        if (t2[n2].Max >= t1[n1].Min)
                        {
                            var q = new StatePair(t1[n1].To, t2[n2].To);
                            StatePair r;
                            newstates.TryGetValue(q, out r);
                            if (r == null)
                            {
                                q.S = new State();
                                worklist.AddLast(q);
                                newstates.Add(q, q);
                                r = q;
                            }

                            char min = t1[n1].Min > t2[n2].Min ? t1[n1].Min : t2[n2].Min;
                            char max = t1[n1].Max < t2[n2].Max ? t1[n1].Max : t2[n2].Max;
                            p.S.Transitions.Add(new Transition(min, max, r.S));
                        }
                    }
                }
            }

            c.IsDeterministic = a1.IsDeterministic && a2.IsDeterministic;
            c.RemoveDeadTransitions();
            c.CheckMinimizeAlways();
            return c;
        }

        /// <summary>
        /// Returns an automaton that accepts the union of the empty string and the language of the 
        /// given automaton.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <remarks>
        /// Complexity: linear in number of states.
        /// </remarks>
        /// <returns>An automaton that accepts the union of the empty string and the language of the 
        /// given automaton.</returns>
        public static Automaton Optional(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            var s = new State();
            s.AddEpsilon(a.Initial);
            s.Accept = true;
            a.Initial = s;
            a.IsDeterministic = false;
            a.ClearHashCode();
            a.CheckMinimizeAlways();
            return a;
        }

        /// <summary>
        /// Accepts the Kleene star (zero or more concatenated repetitions) of the language of the
        /// given automaton. Never modifies the input automaton language.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <returns>
        /// An automaton that accepts the Kleene star (zero or more concatenated repetitions)
        /// of the language of the given automaton. Never modifies the input automaton language.
        /// </returns>
        /// <remarks>
        /// Complexity: linear in number of states.
        /// </remarks>
        public static Automaton Repeat(Automaton a)
        {
            a = a.CloneExpanded();
            var s = new State();
            s.Accept = true;
            s.AddEpsilon(a.Initial);
            foreach (State p in a.GetAcceptStates())
            {
                p.AddEpsilon(s);
            }

            a.Initial = s;
            a.IsDeterministic = false;
            a.ClearHashCode();
            a.CheckMinimizeAlways();
            return a;
        }

        /// <summary>
        /// Accepts <code>min</code> or more concatenated repetitions of the language of the given 
        /// automaton.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <param name="min">The minimum concatenated repetitions of the language of the given 
        /// automaton.</param>
        /// <returns>Returns an automaton that accepts <code>min</code> or more concatenated 
        /// repetitions of the language of the given automaton.
        /// </returns>
        /// <remarks>
        /// Complexity: linear in number of states and in <code>min</code>.
        /// </remarks>
        public static Automaton Repeat(Automaton a, int min)
        {
            if (min == 0)
            {
                return Repeat(a);
            }

            var @as = new List<Automaton>();
            while (min-- > 0)
            {
                @as.Add(a);
            }

            @as.Add(Repeat(a));
            return Concatenate(@as);
        }

        /// <summary>
        /// Accepts between <code>min</code> and <code>max</code> (including both) concatenated
        /// repetitions of the language of the given automaton.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <param name="min">The minimum concatenated repetitions of the language of the given
        /// automaton.</param>
        /// <param name="max">The maximum concatenated repetitions of the language of the given
        /// automaton.</param>
        /// <returns>
        /// Returns an automaton that accepts between <code>min</code> and <code>max</code>
        /// (including both) concatenated repetitions of the language of the given automaton.
        /// </returns>
        /// <remarks>
        /// Complexity: linear in number of states and in <code>min</code> and <code>max</code>.
        /// </remarks>
        public static Automaton Repeat(Automaton a, int min, int max)
        {
            if (min > max)
            {
                return BasicAutomata.MakeEmpty();
            }

            max -= min;
            a.ExpandSingleton();
            Automaton b;
            if (min == 0)
            {
                b = BasicAutomata.MakeEmptyString();
            }
            else if (min == 1)
            {
                b = a.Clone();
            }
            else
            {
                var @as = new List<Automaton>();
                while (min-- > 0)
                {
                    @as.Add(a);
                }

                b = Concatenate(@as);
            }

            if (max > 0)
            {
                Automaton d = a.Clone();
                while (--max > 0)
                {
                    Automaton c = a.Clone();
                    foreach (State p in c.GetAcceptStates())
                    {
                        p.AddEpsilon(d.Initial);
                    }

                    d = c;
                }

                foreach (State p in b.GetAcceptStates())
                {
                    p.AddEpsilon(d.Initial);
                }

                b.IsDeterministic = false;
                b.ClearHashCode();
                b.CheckMinimizeAlways();
            }

            return b;
        }

        /// <summary>
        /// Returns true if the given string is accepted by the automaton.
        /// </summary>
        /// <param name="a">The automaton.</param>
        /// <param name="s">The string.</param>
        /// <returns></returns>
        /// <remarks>
        /// Complexity: linear in the length of the string.
        /// For full performance, use the RunAutomaton class.
        /// </remarks>
        public static bool Run(Automaton a, string s)
        {
            if (a.IsSingleton)
            {
                return s.Equals(a.Singleton);
            }

            if (a.IsDeterministic)
            {
                State p = a.Initial;
                foreach (char t in s)
                {
                    State q = p.Step(t);
                    if (q == null)
                    {
                        return false;
                    }

                    p = q;
                }

                return p.Accept;
            }

            HashSet<State> states = a.GetStates();
            Automaton.SetStateNumbers(states);
            var pp = new LinkedList<State>();
            var ppOther = new LinkedList<State>();
            var bb = new BitArray(states.Count);
            var bbOther = new BitArray(states.Count);
            pp.AddLast(a.Initial);
            var dest = new List<State>();
            bool accept = a.Initial.Accept;

            foreach (char c in s)
            {
                accept = false;
                ppOther.Clear();
                bbOther.SetAll(false);
                foreach (State p in pp)
                {
                    dest.Clear();
                    p.Step(c, dest);
                    foreach (State q in dest)
                    {
                        if (q.Accept)
                        {
                            accept = true;
                        }

                        if (!bbOther.Get(q.Number))
                        {
                            bbOther.Set(q.Number, true);
                            ppOther.AddLast(q);
                        }
                    }
                }

                LinkedList<State> tp = pp;
                pp = ppOther;
                ppOther = tp;
                BitArray tb = bb;
                bb = bbOther;
                bbOther = tb;
            }

            return accept;
        }
    }


/// <summary>
/// Regular Expression extension to Automaton.
/// </summary>
public class RegExp
{
    private readonly string b;
    private readonly RegExpSyntaxOptions flags;

    private static bool allowMutation;

    private char c;
    private int digits;
    private RegExp exp1;
    private RegExp exp2;
    private char from;
    private Kind kind;
    private int max;
    private int min;
    private int pos;
    private string s;
    private char to;

    /// <summary>
    ///   Prevents a default instance of the <see cref = "RegExp" /> class from being created.
    /// </summary>
    private RegExp()
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref = "RegExp" /> class from a string.
    /// </summary>
    /// <param name = "s">A string with the regular expression.</param>
    public RegExp(string s)
        : this(s, RegExpSyntaxOptions.All)
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref = "RegExp" /> class from a string.
    /// </summary>
    /// <param name = "s">A string with the regular expression.</param>
    /// <param name = "syntaxFlags">Boolean 'or' of optional syntax constructs to be enabled.</param>
    public RegExp(string s, RegExpSyntaxOptions syntaxFlags)
    {
        b = s;
        flags = syntaxFlags;
        RegExp e;
        if (s.Length == 0)
        {
            e = MakeString(string.Empty);
        }
        else
        {
            e = ParseUnionExp();
            if (pos < b.Length)
            {
                throw new ArgumentException("end-of-string expected at position " + pos);
            }
        }

        kind = e.kind;
        exp1 = e.exp1;
        exp2 = e.exp2;
        this.s = e.s;
        c = e.c;
        min = e.min;
        max = e.max;
        digits = e.digits;
        from = e.from;
        to = e.to;
        b = null;
    }

    /// <summary>
    ///   Constructs new <code>Automaton</code> from this <code>RegExp</code>. 
    ///   Same as <code>toAutomaton(null)</code> (empty automaton map).
    /// </summary>
    /// <returns></returns>
    public Automaton ToAutomaton()
    {
        return ToAutomatonAllowMutate(null, null, true);
    }

    /// <summary>
    /// Constructs new <code>Automaton</code> from this <code>RegExp</code>.
    /// Same as <code>toAutomaton(null,minimize)</code> (empty automaton map).
    /// </summary>
    /// <param name="minimize">if set to <c>true</c> [minimize].</param>
    /// <returns></returns>
    public Automaton ToAutomaton(bool minimize)
    {
        return ToAutomatonAllowMutate(null, null, minimize);
    }

    /// <summary>
    ///   Constructs new <code>Automaton</code> from this <code>RegExp</code>. 
    ///   The constructed automaton is minimal and deterministic and has no 
    ///   transitions to dead states.
    /// </summary>
    /// <param name = "automatonProvider">The provider of automata for named identifiers.</param>
    /// <returns></returns>
    public Automaton ToAutomaton(IAutomatonProvider automatonProvider)
    {
        return ToAutomatonAllowMutate(null, automatonProvider, true);
    }

    /// <summary>
    ///   Constructs new <code>Automaton</code> from this <code>RegExp</code>. 
    ///   The constructed automaton has no transitions to dead states.
    /// </summary>
    /// <param name = "automatonProvider">The provider of automata for named identifiers.</param>
    /// <param name = "minimize">if set to <c>true</c> the automaton is minimized and determinized.</param>
    /// <returns></returns>
    public Automaton ToAutomaton(IAutomatonProvider automatonProvider, bool minimize)
    {
        return ToAutomatonAllowMutate(null, automatonProvider, minimize);
    }

    /// <summary>
    ///   Constructs new <code>Automaton</code> from this <code>RegExp</code>. 
    ///   The constructed automaton is minimal and deterministic and has no 
    ///   transitions to dead states.
    /// </summary>
    /// <param name = "automata">The a map from automaton identifiers to automata.</param>
    /// <returns></returns>
    public Automaton ToAutomaton(IDictionary<string, Automaton> automata)
    {
        return ToAutomatonAllowMutate(automata, null, true);
    }

    /// <summary>
    ///   Constructs new <code>Automaton</code> from this <code>RegExp</code>. 
    ///   The constructed automaton has no transitions to dead states.
    /// </summary>
    /// <param name = "automata">The map from automaton identifiers to automata.</param>
    /// <param name = "minimize">if set to <c>true</c> the automaton is minimized and determinized.</param>
    /// <returns></returns>
    public Automaton ToAutomaton(IDictionary<string, Automaton> automata, bool minimize)
    {
        return ToAutomatonAllowMutate(automata, null, minimize);
    }

    /// <summary>
    ///   Sets or resets allow mutate flag.
    ///   If this flag is set, then automata construction uses mutable automata,
    ///   which is slightly faster but not thread safe.
    /// </summary>
    /// <param name = "flag">if set to <c>true</c> the flag is set.</param>
    /// <returns>The previous value of the flag.</returns>
    public bool SetAllowMutate(bool flag)
    {
        bool @bool = allowMutation;
        allowMutation = flag;
        return @bool;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToStringBuilder(new StringBuilder()).ToString();
    }

    /// <summary>
    /// Returns the set of automaton identifiers that occur in this regular expression.
    /// </summary>
    /// <returns>The set of automaton identifiers that occur in this regular expression.</returns>
    public HashSet<string> GetIdentifiers()
    {
        var set = new HashSet<string>();
        GetIdentifiers(set);
        return set;
    }

    private static RegExp MakeUnion(RegExp exp1, RegExp exp2)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpUnion;
        r.exp1 = exp1;
        r.exp2 = exp2;
        return r;
    }

    private static RegExp MakeIntersection(RegExp exp1, RegExp exp2)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpIntersection;
        r.exp1 = exp1;
        r.exp2 = exp2;
        return r;
    }

    private static RegExp MakeConcatenation(RegExp exp1, RegExp exp2)
    {
        if ((exp1.kind == Kind.RegexpChar || exp1.kind == Kind.RegexpString)
            && (exp2.kind == Kind.RegexpChar || exp2.kind == Kind.RegexpString))
        {
            return MakeString(exp1, exp2);
        }

        var r = new RegExp();
        r.kind = Kind.RegexpConcatenation;
        if (exp1.kind == Kind.RegexpConcatenation
            && (exp1.exp2.kind == Kind.RegexpChar || exp1.exp2.kind == Kind.RegexpString)
            && (exp2.kind == Kind.RegexpChar || exp2.kind == Kind.RegexpString))
        {
            r.exp1 = exp1.exp1;
            r.exp2 = MakeString(exp1.exp2, exp2);
        }
        else if ((exp1.kind == Kind.RegexpChar || exp1.kind == Kind.RegexpString)
                 && exp2.kind == Kind.RegexpConcatenation
                 && (exp2.exp1.kind == Kind.RegexpChar || exp2.exp1.kind == Kind.RegexpString))
        {
            r.exp1 = MakeString(exp1, exp2.exp1);
            r.exp2 = exp2.exp2;
        }
        else
        {
            r.exp1 = exp1;
            r.exp2 = exp2;
        }

        return r;
    }

    private static RegExp MakeRepeat(RegExp exp)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpRepeat;
        r.exp1 = exp;
        return r;
    }

    private static RegExp MakeRepeat(RegExp exp, int min)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpRepeatMin;
        r.exp1 = exp;
        r.min = min;
        return r;
    }

    private static RegExp MakeRepeat(RegExp exp, int min, int max)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpRepeatMinMax;
        r.exp1 = exp;
        r.min = min;
        r.max = max;
        return r;
    }

    private static RegExp MakeOptional(RegExp exp)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpOptional;
        r.exp1 = exp;
        return r;
    }

    private static RegExp MakeChar(char @char)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpChar;
        r.c = @char;
        return r;
    }

    private static RegExp MakeInterval(int min, int max, int digits)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpInterval;
        r.min = min;
        r.max = max;
        r.digits = digits;
        return r;
    }

    private static RegExp MakeAutomaton(string s)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpAutomaton;
        r.s = s;
        return r;
    }

    private static RegExp MakeAnyString()
    {
        var r = new RegExp();
        r.kind = Kind.RegexpAnyString;
        return r;
    }

    private static RegExp MakeEmpty()
    {
        var r = new RegExp();
        r.kind = Kind.RegexpEmpty;
        return r;
    }

    private static RegExp MakeAnyChar()
    {
        var r = new RegExp();
        r.kind = Kind.RegexpAnyChar;
        return r;
    }

    private static RegExp MakeAnyPrintableASCIIChar()
    {
        return MakeCharRange(' ', '~');
    }

    private static RegExp MakeCharRange(char from, char to)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpCharRange;
        r.from = from;
        r.to = to;
        return r;
    }

    private static RegExp MakeComplement(RegExp exp)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpComplement;
        r.exp1 = exp;
        return r;
    }

    private static RegExp MakeString(string @string)
    {
        var r = new RegExp();
        r.kind = Kind.RegexpString;
        r.s = @string;
        return r;
    }

    private static RegExp MakeString(RegExp exp1, RegExp exp2)
    {
        var sb = new StringBuilder();
        if (exp1.kind == Kind.RegexpString)
        {
            sb.Append(exp1.s);
        }
        else
        {
            sb.Append(exp1.c);
        }

        if (exp2.kind == Kind.RegexpString)
        {
            sb.Append(exp2.s);
        }
        else
        {
            sb.Append(exp2.c);
        }

        return MakeString(sb.ToString());
    }

    private Automaton ToAutomatonAllowMutate(
        IDictionary<string, Automaton> automata,
        IAutomatonProvider automatonProvider,
        bool minimize)
    {
        bool @bool = false;
        if (allowMutation)
        {
            @bool = SetAllowMutate(true); // This is not thead safe.
        }

        Automaton a = ToAutomaton(automata, automatonProvider, minimize);
        if (allowMutation)
        {
            SetAllowMutate(@bool);
        }

        return a;
    }

    private Automaton ToAutomaton(
        IDictionary<string, Automaton> automata,
        IAutomatonProvider automatonProvider,
        bool minimize)
    {
        IList<Automaton> list;
        Automaton a = null;
        switch (kind)
        {
            case Kind.RegexpUnion:
                list = new List<Automaton>();
                FindLeaves(exp1, Kind.RegexpUnion, list, automata, automatonProvider, minimize);
                FindLeaves(exp2, Kind.RegexpUnion, list, automata, automatonProvider, minimize);
                a = BasicOperations.Union(list);
                a.Minimize();
                break;
            case Kind.RegexpConcatenation:
                list = new List<Automaton>();
                FindLeaves(exp1, Kind.RegexpConcatenation, list, automata, automatonProvider, minimize);
                FindLeaves(exp2, Kind.RegexpConcatenation, list, automata, automatonProvider, minimize);
                a = BasicOperations.Concatenate(list);
                a.Minimize();
                break;
            case Kind.RegexpIntersection:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize)
                    .Intersection(exp2.ToAutomaton(automata, automatonProvider, minimize));
                a.Minimize();
                break;
            case Kind.RegexpOptional:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize).Optional();
                a.Minimize();
                break;
            case Kind.RegexpRepeat:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize).Repeat();
                a.Minimize();
                break;
            case Kind.RegexpRepeatMin:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize).Repeat(min);
                a.Minimize();
                break;
            case Kind.RegexpRepeatMinMax:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize).Repeat(min, max);
                a.Minimize();
                break;
            case Kind.RegexpComplement:
                a = exp1.ToAutomaton(automata, automatonProvider, minimize).Complement();
                a.Minimize();
                break;
            case Kind.RegexpChar:
                a = BasicAutomata.MakeChar(c);
                break;
            case Kind.RegexpCharRange:
                a = BasicAutomata.MakeCharRange(from, to);
                break;
            case Kind.RegexpAnyChar:
                a = BasicAutomata.MakeAnyChar();
                break;
            case Kind.RegexpEmpty:
                a = BasicAutomata.MakeEmpty();
                break;
            case Kind.RegexpString:
                a = BasicAutomata.MakeString(s);
                break;
            case Kind.RegexpAnyString:
                a = BasicAutomata.MakeAnyString();
                break;
            case Kind.RegexpAutomaton:
                Automaton aa = null;
                if (automata != null)
                {
                    automata.TryGetValue(s, out aa);
                }

                if (aa == null && automatonProvider != null)
                {
                    try
                    {
                        aa = automatonProvider.GetAutomaton(s);
                    }
                    catch (IOException e)
                    {
                        throw new ArgumentException(string.Empty, e);
                    }
                }

                if (aa == null)
                {
                    throw new ArgumentException("'" + s + "' not found");
                }

                a = aa.Clone(); // Always clone here (ignore allowMutate).
                break;
            case Kind.RegexpInterval:
                a = BasicAutomata.MakeInterval(min, max, digits);
                break;
        }

        return a;
    }

    private void FindLeaves(
        RegExp exp,
        Kind regExpKind,
        IList<Automaton> list,
        IDictionary<String, Automaton> automata,
        IAutomatonProvider automatonProvider,
        bool minimize)
    {
        if (exp.kind == regExpKind)
        {
            FindLeaves(exp.exp1, regExpKind, list, automata, automatonProvider, minimize);
            FindLeaves(exp.exp2, regExpKind, list, automata, automatonProvider, minimize);
        }
        else
        {
            list.Add(exp.ToAutomaton(automata, automatonProvider, minimize));
        }
    }

    private StringBuilder ToStringBuilder(StringBuilder sb)
    {
        switch (kind)
        {
            case Kind.RegexpUnion:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append("|");
                exp2.ToStringBuilder(sb);
                sb.Append(")");
                break;
            case Kind.RegexpConcatenation:
                exp1.ToStringBuilder(sb);
                exp2.ToStringBuilder(sb);
                break;
            case Kind.RegexpIntersection:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append("&");
                exp2.ToStringBuilder(sb);
                sb.Append(")");
                break;
            case Kind.RegexpOptional:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append(")?");
                break;
            case Kind.RegexpRepeat:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append(")*");
                break;
            case Kind.RegexpRepeatMin:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append("){").Append(min).Append(",}");
                break;
            case Kind.RegexpRepeatMinMax:
                sb.Append("(");
                exp1.ToStringBuilder(sb);
                sb.Append("){").Append(min).Append(",").Append(max).Append("}");
                break;
            case Kind.RegexpComplement:
                sb.Append("~(");
                exp1.ToStringBuilder(sb);
                sb.Append(")");
                break;
            case Kind.RegexpChar:
                sb.Append("\\").Append(c);
                break;
            case Kind.RegexpCharRange:
                sb.Append("[\\").Append(from).Append("-\\").Append(to).Append("]");
                break;
            case Kind.RegexpAnyChar:
                sb.Append(".");
                break;
            case Kind.RegexpEmpty:
                sb.Append("#");
                break;
            case Kind.RegexpString:
                sb.Append("\"").Append(s).Append("\"");
                break;
            case Kind.RegexpAnyString:
                sb.Append("@");
                break;
            case Kind.RegexpAutomaton:
                sb.Append("<").Append(s).Append(">");
                break;
            case Kind.RegexpInterval:
                string s1 = Convert.ToDecimal(min).ToString();
                string s2 = Convert.ToDecimal(max).ToString();
                sb.Append("<");
                if (digits > 0)
                {
                    for (int i = s1.Length; i < digits; i++)
                    {
                        sb.Append('0');
                    }
                }

                sb.Append(s1).Append("-");
                if (digits > 0)
                {
                    for (int i = s2.Length; i < digits; i++)
                    {
                        sb.Append('0');
                    }
                }

                sb.Append(s2).Append(">");
                break;
        }

        return sb;
    }

    private void GetIdentifiers(HashSet<string> set)
    {
        switch (kind)
        {
            case Kind.RegexpUnion:
            case Kind.RegexpConcatenation:
            case Kind.RegexpIntersection:
                exp1.GetIdentifiers(set);
                exp2.GetIdentifiers(set);
                break;
            case Kind.RegexpOptional:
            case Kind.RegexpRepeat:
            case Kind.RegexpRepeatMin:
            case Kind.RegexpRepeatMinMax:
            case Kind.RegexpComplement:
                exp1.GetIdentifiers(set);
                break;
            case Kind.RegexpAutomaton:
                set.Add(s);
                break;
        }
    }

    private RegExp ParseUnionExp()
    {
        RegExp e = ParseInterExp();
        if (Match('|'))
        {
            e = MakeUnion(e, ParseUnionExp());
        }

        return e;
    }

    private bool Match(char @char)
    {
        if (pos >= b.Length)
        {
            return false;
        }

        if (b[pos] == @char)
        {
            pos++;
            return true;
        }

        return false;
    }

    private RegExp ParseInterExp()
    {
        RegExp e = ParseConcatExp();
        if (Check(RegExpSyntaxOptions.Intersection) && Match('&'))
        {
            e = MakeIntersection(e, ParseInterExp());
        }

        return e;
    }

    private bool Check(RegExpSyntaxOptions flag)
    {
        return (flags & flag) != 0;
    }

    private RegExp ParseConcatExp()
    {
        RegExp e = ParseRepeatExp();
        if (More() && !Peek(")|") && (!Check(RegExpSyntaxOptions.Intersection) || !Peek("&")))
        {
            e = MakeConcatenation(e, ParseConcatExp());
        }

        return e;
    }

    private bool More()
    {
        return pos < b.Length;
    }

    private bool Peek(string @string)
    {
        return More() && @string.IndexOf(b[pos]) != -1;
    }

    private RegExp ParseRepeatExp()
    {
        RegExp e = ParseComplExp();
        while (Peek("?*+{"))
        {
            if (Match('?'))
            {
                e = MakeOptional(e);
            }
            else if (Match('*'))
            {
                e = MakeRepeat(e);
            }
            else if (Match('+'))
            {
                e = MakeRepeat(e, 1);
            }
            else if (Match('{'))
            {
                int start = pos;
                while (Peek("0123456789"))
                {
                    Next();
                }

                if (start == pos)
                {
                    throw new ArgumentException("integer expected at position " + pos);
                }

                int n = int.Parse(b.Substring(start, pos - start));
                int m = -1;
                if (Match(','))
                {
                    start = pos;
                    while (Peek("0123456789"))
                    {
                        Next();
                    }

                    if (start != pos)
                    {
                        m = int.Parse(b.Substring(start, pos - start));
                    }
                }
                else
                {
                    m = n;
                }

                if (!Match('}'))
                {
                    throw new ArgumentException("expected '}' at position " + pos);
                }

                e = m == -1 ? MakeRepeat(e, n) : MakeRepeat(e, n, m);
            }
        }

        return e;
    }

    private char Next()
    {
        if (!More())
        {
            throw new InvalidOperationException("unexpected end-of-string");
        }

        return b[pos++];
    }

    private RegExp ParseComplExp()
    {
        if (Check(RegExpSyntaxOptions.Complement) && Match('~'))
        {
            return MakeComplement(ParseComplExp());
        }

        return ParseCharClassExp();
    }

    private RegExp ParseCharClassExp()
    {
        if (Match('['))
        {
            bool negate = false;
            if (Match('^'))
            {
                negate = true;
            }

            RegExp e = ParseCharClasses();
            if (negate)
            {
                e = ExcludeChars(e, MakeAnyPrintableASCIIChar());
            }

            if (!Match(']'))
            {
                throw new ArgumentException("expected ']' at position " + pos);
            }

            return e;
        }

        return ParseSimpleExp();
    }

    private RegExp ParseSimpleExp()
    {
        if (Match('.'))
        {
            return MakeAnyPrintableASCIIChar();
        }

        if (Check(RegExpSyntaxOptions.Empty) && Match('#'))
        {
            return MakeEmpty();
        }

        if (Check(RegExpSyntaxOptions.Anystring) && Match('@'))
        {
            return MakeAnyString();
        }

        if (Match('"'))
        {
            int start = pos;
            while (More() && !Peek("\""))
            {
                Next();
            }

            if (!Match('"'))
            {
                throw new ArgumentException("expected '\"' at position " + pos);
            }

            return MakeString(b.Substring(start, ((pos - 1) - start)));
        }

        if (Match('('))
        {
            if (Match('?'))
            {
                SkipNonCapturingSubpatternExp();
            }

            if (Match(')'))
            {
                return MakeString(string.Empty);
            }

            RegExp e = ParseUnionExp();
            if (!Match(')'))
            {
                throw new ArgumentException("expected ')' at position " + pos);
            }

            return e;
        }

        if ((Check(RegExpSyntaxOptions.Automaton) || Check(RegExpSyntaxOptions.Interval)) && Match('<'))
        {
            int start = pos;
            while (More() && !Peek(">"))
            {
                Next();
            }

            if (!Match('>'))
            {
                throw new ArgumentException("expected '>' at position " + pos);
            }

            string str = b.Substring(start, ((pos - 1) - start));
            int i = str.IndexOf('-');
            if (i == -1)
            {
                if (!Check(RegExpSyntaxOptions.Automaton))
                {
                    throw new ArgumentException("interval syntax error at position " + (pos - 1));
                }

                return MakeAutomaton(str);
            }

            if (!Check(RegExpSyntaxOptions.Interval))
            {
                throw new ArgumentException("illegal identifier at position " + (pos - 1));
            }

            try
            {
                if (i == 0 || i == str.Length - 1 || i != str.LastIndexOf('-'))
                {
                    throw new FormatException();
                }

                string smin = str.Substring(0, i - 0);
                string smax = str.Substring(i + 1, (str.Length - (i + 1)));
                int imin = int.Parse(smin);
                int imax = int.Parse(smax);
                int numdigits = smin.Length == smax.Length ? smin.Length : 0;
                if (imin > imax)
                {
                    int t = imin;
                    imin = imax;
                    imax = t;
                }

                return MakeInterval(imin, imax, numdigits);
            }
            catch (FormatException)
            {
                throw new ArgumentException("interval syntax error at position " + (pos - 1));
            }
        }

        if (Match('\\'))
        {
            // Escaped '\' character.
            if (Match('\\'))
            {
                return MakeChar('\\');
            }

            bool inclusion;

            // Digits.
            if ((inclusion = Match('d')) || Match('D'))
            {
                RegExp digitChars = MakeCharRange('0', '9');
                return inclusion ? digitChars : ExcludeChars(digitChars, MakeAnyPrintableASCIIChar());
            }

            // Whitespace chars only.
            if ((inclusion = Match('s')) || Match('S'))
            {
                // Do not add line breaks, as usually RegExp is single line.
                RegExp whitespaceChars = MakeUnion(MakeChar(' '), MakeChar('\t'));
                return inclusion ? whitespaceChars : ExcludeChars(whitespaceChars, MakeAnyPrintableASCIIChar());
            }

            // Word character. Range is [A-Za-z0-9_]
            if ((inclusion = Match('w')) || Match('W'))
            {
                var ranges = new[] { MakeCharRange('A', 'Z'), MakeCharRange('a', 'z'), MakeCharRange('0', '9') };
                RegExp wordChars = ranges.Aggregate(MakeChar('_'), MakeUnion);
                    
                return inclusion ? wordChars : ExcludeChars(wordChars, MakeAnyPrintableASCIIChar());
            }
        }
            
        return MakeChar(ParseCharExp());
    }

    private void SkipNonCapturingSubpatternExp()
    {
        RegExpMatchingOptions.All().Any(Match);
        Match(':');
    }

    private char ParseCharExp()
    {
        Match('\\');
        return Next();
    }

    private RegExp ParseCharClasses()
    {
        RegExp e = ParseCharClass();
        while (More() && !Peek("]"))
        {
            e = MakeUnion(e, ParseCharClass());
        }

        return e;
    }

    private RegExp ParseCharClass()
    {
        char @char = ParseCharExp();
        if (Match('-'))
        {
            if (Peek("]"))
            {
                return MakeUnion(MakeChar(@char), MakeChar('-'));
            }

            return MakeCharRange(@char, ParseCharExp());
        }

        return MakeChar(@char);
    }

    private static RegExp ExcludeChars(RegExp exclusion, RegExp allChars)
    {
        return MakeIntersection(allChars, MakeComplement(exclusion));
    }

    private enum Kind
    {
        RegexpUnion,
        RegexpConcatenation,
        RegexpIntersection,
        RegexpOptional,
        RegexpRepeat,
        RegexpRepeatMin,
        RegexpRepeatMinMax,
        RegexpComplement,
        RegexpChar,
        RegexpCharRange,
        RegexpAnyChar,
        RegexpEmpty,
        RegexpString,
        RegexpAnyString,
        RegexpAutomaton,
        RegexpInterval
    }
}