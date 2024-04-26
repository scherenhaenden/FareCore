namespace FareCore
{
    public class TransitionComparer : IComparer<Transition>
    {
        private readonly bool toFirst;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransitionComparer"/> class.
        /// </summary>
        /// <param name="toFirst">if set to <c>true</c> [to first].</param>
        public TransitionComparer(bool toFirst)
    {
        this.toFirst = toFirst;
    }

        /// <summary>
        /// Compares by (min, reverse max, to) or (to, min, reverse max).
        /// </summary>
        /// <param name="t1">The first Transition.</param>
        /// <param name="t2">The second Transition.</param>
        /// <returns></returns>
        public int Compare(Transition t1, Transition t2)
    {
        if (toFirst)
        {
            if (t1.To != t2.To)
            {
                if (t1.To == null)
                {
                    return -1;
                }

                if (t2.To == null)
                {
                    return 1;
                }

                if (t1.To.Number < t2.To.Number)
                {
                    return -1;
                }

                if (t1.To.Number > t2.To.Number)
                {
                    return 1;
                }
            }
        }

        if (t1.Min < t2.Min)
        {
            return -1;
        }

        if (t1.Min > t2.Min)
        {
            return 1;
        }

        if (t1.Max > t2.Max)
        {
            return -1;
        }

        if (t1.Max < t2.Max)
        {
            return 1;
        }

        if (!toFirst)
        {
            if (t1.To != t2.To)
            {
                if (t1.To == null)
                {
                    return -1;
                }

                if (t2.To == null)
                {
                    return 1;
                }

                if (t1.To.Number < t2.To.Number)
                {
                    return -1;
                }

                if (t1.To.Number > t2.To.Number)
                {
                    return 1;
                }
            }
        }

        return 0;
    }
    }
}