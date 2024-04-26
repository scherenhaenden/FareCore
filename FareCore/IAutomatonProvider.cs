namespace RandomDataGeneratorCore.FareRegex;

public interface IAutomatonProvider
{
    Automaton GetAutomaton(string name);
}