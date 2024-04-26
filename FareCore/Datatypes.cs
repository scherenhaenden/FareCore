namespace FareCore
{
    public static class Datatypes
    {
        private static readonly Automaton ws = Automaton.Minimize(Automaton.MakeCharSet(" \t\n\r").Repeat());

        public static Automaton WhitespaceAutomaton
        {
            get { return ws; }
        }
    }
}