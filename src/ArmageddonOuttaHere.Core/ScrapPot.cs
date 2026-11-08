namespace ArmageddonOuttaHere.Core;

/// <summary>
/// Splits scrap from enemies evenly between every player in a level. Leftovers that don't divide evenly are kept
/// and added to the next drop, so no scrap is lost.
/// </summary>
public sealed class ScrapPot
{
    private int _leftover;

    public void Share(int amount, IReadOnlyList<Character> players)
    {
        if (players.Count == 0)
        {
            return;
        }

        var total = amount + _leftover;
        var each = total / players.Count;
        _leftover = total % players.Count;
        if (each == 0)
        {
            return;
        }

        foreach (var player in players)
        {
            player.AddScrap(each);
        }
    }
}
