namespace FareCore
{
    public interface IAutomatonProvider
    {
        Automaton GetAutomaton(string name);
    }
}