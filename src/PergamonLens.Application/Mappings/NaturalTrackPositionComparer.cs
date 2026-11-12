using PergamonLens.Domain.Entities;

namespace PergamonLens.Application.Mappings;

/// <summary>
/// Comparador natural para ordenar alfanuméricamente posiciones de pistas
/// (ej. "1, 2, ..., 9, 10, 11, 19" y "A1, A2, ..., A10, B1, B2").
/// </summary>
public sealed class NaturalTrackPositionComparer : IComparer<string>, IComparer<Track>
{
    public static readonly NaturalTrackPositionComparer Instance = new();

    public int Compare(Track? x, Track? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return Compare(x.Position, y.Position);
    }

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                // Extraer segmento numérico contiguo de x
                int startX = i;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                ReadOnlySpan<char> numSpanX = x.AsSpan(startX, i - startX);

                // Extraer segmento numérico contiguo de y
                int startY = j;
                while (j < y.Length && char.IsDigit(y[j])) j++;
                ReadOnlySpan<char> numSpanY = y.AsSpan(startY, j - startY);

                // Comparación de valor numérico
                if (ulong.TryParse(numSpanX, out var numX) && ulong.TryParse(numSpanY, out var numY))
                {
                    int numComp = numX.CompareTo(numY);
                    if (numComp != 0) return numComp;
                }
                else
                {
                    if (numSpanX.Length != numSpanY.Length)
                    {
                        return numSpanX.Length.CompareTo(numSpanY.Length);
                    }
                    int spanComp = numSpanX.CompareTo(numSpanY, StringComparison.Ordinal);
                    if (spanComp != 0) return spanComp;
                }
            }
            else
            {
                char cX = char.ToUpperInvariant(x[i]);
                char cY = char.ToUpperInvariant(y[j]);

                if (cX != cY)
                {
                    return cX.CompareTo(cY);
                }

                i++;
                j++;
            }
        }

        return x.Length.CompareTo(y.Length);
    }
}
